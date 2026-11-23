using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Hinet.Api;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

static class SaasHttpChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var config = new ConfigurationBuilder().SetBasePath(Path.GetFullPath("API/Hinet.Api")).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).Build();
        var connection = config["Connections:DefaultConnection"]!;
        await using var sql = new SqlConnection(connection); await sql.OpenAsync();
        using var command = sql.CreateCommand();
        command.CommandText = "SELECT Id FROM dbo.AspNetUsers WHERE NormalizedUserName=@name";
        command.Parameters.AddWithValue("@name", (config["RentalSaas:OwnerUserName"] ?? "admin").ToUpperInvariant());
        var owner = (Guid)(await command.ExecuteScalarAsync() ?? throw new Exception("Owner missing"));
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor { Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, owner.ToString()) }), Issuer = config["AuthSetting:Issuer"], Audience = config["AuthSetting:Audience"], Expires = DateTime.UtcNow.AddMinutes(10), SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["AuthSetting:Key"]!)), SecurityAlgorithms.HmacSha256Signature) };
        using var admin = Client(handler.WriteToken(handler.CreateToken(descriptor)));
        using var anonymous = Client("");
        var created = new List<(Guid Id, Guid UserId)>();
        try
        {
            check((await anonymous.GetAsync("rental-assistant/overview")).StatusCode == HttpStatusCode.Unauthorized, "HTTP: anonymous customer data denied");
            anonymous.DefaultRequestHeaders.Add("X-Rental-Key", config["RentalAssistant:ManagementKey"] ?? "");
            check((await anonymous.GetAsync("rental-assistant/overview")).StatusCode == HttpStatusCode.Unauthorized, "HTTP: old management key cannot bypass JWT");
            var accounts = new List<HttpClient>();
            var names = new List<string>();
            for (int i = 0; i < 2; i++)
            {
                var name = "saas-check-" + Guid.NewGuid().ToString("N")[..12];
                var password = "VerifyPrivate123-" + Guid.NewGuid().ToString("N");
                var provision = await admin.PostAsJsonAsync("rental-account/customers", new { name, userName = name, email = name + "@example.test", password, expiresAt = DateTimeOffset.UtcNow.AddHours(1) });
                if (!provision.IsSuccessStatusCode) throw new Exception("HTTP provision failed: " + (int)provision.StatusCode);
                var id = (await provision.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
                command.Parameters.Clear(); command.CommandText = "SELECT UserId FROM rental.Customers WHERE Id=@id"; command.Parameters.AddWithValue("@id", id);
                created.Add((id, (Guid)(await command.ExecuteScalarAsync())!));
                var login = await anonymous.PostAsJsonAsync("Account/Login", new { userName = name, password });
                var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>();
                accounts.Add(Client(loginJson.GetProperty("data").GetProperty("token").GetString()!));
                names.Add(name);
            }
            using var a = accounts[0]; using var b = accounts[1];
            check((await a.GetAsync("rental-account/customers")).StatusCode == HttpStatusCode.Forbidden, "HTTP: customer cannot list other customers");
            check((await a.GetAsync("AppUser/GetData")).StatusCode == HttpStatusCode.Forbidden, "HTTP: customer cannot reach legacy administration APIs");
            var tenantA = new RentalTenant { Id = created[0].Id };
            var tenantB = new RentalTenant { Id = created[1].Id };
            // First overview provisions each isolated schema with all automatic workers disabled.
            check((await a.GetAsync("rental-assistant/overview")).IsSuccessStatusCode && (await b.GetAsync("rental-assistant/overview")).IsSuccessStatusCode, "HTTP: valid customers can open their own dashboards");
            await using var db = new RentalCustomerDbFactory(connection, tenantA.Schema).CreateDbContext();
            var room = new RentalRoom { Name = "HTTP A private", Address = "A", MonthlyRent = 1 }; db.Rooms.Add(room);
            var post = new RentalPost { RoomId = room.Id, Content = "HTTP A private draft" }; db.Posts.Add(post); await db.SaveChangesAsync();
            check((await b.PutAsJsonAsync("rental-assistant/posts/" + post.Id, new { content = "malicious" })).StatusCode == HttpStatusCode.NotFound, "HTTP: customer cannot edit another customer's draft by ID");
            var overviewB = await b.GetFromJsonAsync<JsonElement>("rental-assistant/overview");
            check(overviewB.GetProperty("rooms").GetArrayLength() == 0 && overviewB.GetProperty("posts").GetArrayLength() == 0, "HTTP: customer overview excludes other customer's records");
            var me = await a.GetFromJsonAsync<JsonElement>("rental-account/me");
            check(!me.GetProperty("isOwner").GetBoolean() && !me.TryGetProperty("telegramToken", out _), "HTTP: customer metadata has no admin rights or bot secret");
            using var multipart = new MultipartFormDataContent();
            var bytes = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZlWQAAAAASUVORK5CYII="));
            bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png"); multipart.Add(bytes, "files", "pixel.png");
            var upload = await a.PostAsync("rental-assistant/images", multipart); upload.EnsureSuccessStatusCode();
            var image = (await upload.Content.ReadFromJsonAsync<string[]>())![0];
            using var mediaA = new HttpClient { BaseAddress = new Uri("http://localhost:5119/") };
            using var mediaB = new HttpClient { BaseAddress = mediaA.BaseAddress };
            // HttpClient keeps the HttpOnly media cookie obtained by /me, without JWT headers.
            mediaA.DefaultRequestHeaders.Authorization = a.DefaultRequestHeaders.Authorization;
            var cookieResponse = await mediaA.GetAsync("api/rental-account/me"); cookieResponse.EnsureSuccessStatusCode();
            mediaA.DefaultRequestHeaders.Authorization = null;
            check((await mediaA.GetAsync("rental-media/" + image)).IsSuccessStatusCode, "HTTP: private images load through time-limited HttpOnly cookie");
            mediaB.DefaultRequestHeaders.Authorization = b.DefaultRequestHeaders.Authorization;
            check((await mediaB.GetAsync("rental-media/" + image)).StatusCode == HttpStatusCode.NotFound, "HTTP: another customer cannot load private image by ID");
            var resetPassword = "ResetPrivate123-" + Guid.NewGuid().ToString("N");
            var reset = await admin.PutAsJsonAsync("rental-account/customers/" + created[1].Id + "/password", new { password = resetPassword }); reset.EnsureSuccessStatusCode();
            check((await b.GetAsync("rental-account/me")).StatusCode == HttpStatusCode.Unauthorized, "HTTP: password reset revokes an existing customer JWT");
            check((await b.GetAsync("Account/GetInfo")).StatusCode == HttpStatusCode.Unauthorized, "HTTP: revoked token cannot use legacy account API either");
            var relogin = await anonymous.PostAsJsonAsync("Account/Login", new { userName = names[1], password = resetPassword });
            var reloginJson = await relogin.Content.ReadFromJsonAsync<JsonElement>();
            using var fresh = Client(reloginJson.GetProperty("data").GetProperty("token").GetString()!);
            check((await fresh.GetAsync("rental-account/me")).IsSuccessStatusCode, "HTTP: new password immediately opens a fresh session");
            var expire = await admin.PutAsJsonAsync("rental-account/customers/" + created[0].Id, new { enabled = true, expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) }); expire.EnsureSuccessStatusCode();
            check((await a.GetAsync("rental-assistant/overview")).StatusCode == HttpStatusCode.Forbidden, "HTTP: expired lease blocks an already issued JWT");
            var disable = await admin.PutAsJsonAsync("rental-account/customers/" + created[1].Id, new { enabled = false, expiresAt = DateTimeOffset.UtcNow.AddHours(1) }); disable.EnsureSuccessStatusCode();
            check((await b.GetAsync("rental-account/me")).StatusCode == HttpStatusCode.Forbidden, "HTTP: locked customer cannot access settings with an old JWT");
        }
        finally
        {
            // Only rows and generated schemas created by this test are removed.
            foreach (var c in created)
            {
                var schema = "customer_" + c.Id.ToString("N");
                command.Parameters.Clear(); command.CommandText = "DELETE FROM rental.Customers WHERE Id=@id AND UserId=@user; DELETE FROM dbo.AspNetUsers WHERE Id=@user;";
                command.Parameters.AddWithValue("@id", c.Id); command.Parameters.AddWithValue("@user", c.UserId); await command.ExecuteNonQueryAsync();
                await using var db = new RentalCustomerDbFactory(connection, schema).CreateDbContext();
                foreach (var table in new[] { "Appointments", "Deliveries", "Posts", "Threads", "Groups", "Rooms", "State" })
                    await RentalSchemaUpgrade.ExecuteLiteralAsync(db, $"DROP TABLE IF EXISTS [{schema}].[{table}];");
                await RentalSchemaUpgrade.ExecuteLiteralAsync(db, $"IF SCHEMA_ID(N'{schema}') IS NOT NULL EXEC(N'DROP SCHEMA [{schema}]');");
                SaasChecks.DeleteTestDirectory(Path.GetFullPath("API/Hinet.Api"), new RentalTenant { Id = c.Id });
            }
        }
    }
    private static HttpClient Client(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5119/api/"), Timeout = TimeSpan.FromSeconds(60) };
        if (token.Length > 0) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
