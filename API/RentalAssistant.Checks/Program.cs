using System.Net;
using System.Text;
using System.Text.Json;
using Hinet.Api;
using Hinet.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

int checks = 0;
void Check(bool pass, string name) { if (!pass) throw new Exception("FAILED: " + name); checks++; Console.WriteLine("PASS: " + name); }
if (args.Contains("--messenger-setup-only")) { await MessengerSetupChecks.Run(Check); return; }
if (args.Contains("--photo-upload-only")) { await PhotoUploadChecks.Run(Check); return; }
SqlServerChecks.Run(Check);
EngagementChecks.Run(Check);
if (args.Contains("--sqlserver-only")) return;
Check(RentalFacebook.ValidUrl("https://www.facebook.com/groups/123456/", true), "group URL accepted");
Check(RentalFacebook.ValidUrl("https://www.facebook.com/messages/t/123456", false), "personal thread URL accepted");
foreach (var bad in new[] { "http://facebook.com/groups/123", "https://facebook.com.evil.test/groups/123", "https://facebook.com@evil.test/groups/123", "https://www.facebook.com/groups/123/posts/55", "https://www.facebook.com/groups/123?next=evil", "https://www.facebook.com:444/groups/123" })
    Check(!RentalFacebook.ValidUrl(bad, true), "reject untrusted URL " + bad);
Check(!RentalFacebook.ValidUrl("https://www.facebook.com/groups/123", false), "cannot treat group as inbox");

using var services = new ServiceCollection().Configure<RentalOptions>(o => o.ManagementKey = new string('k', 32)).BuildServiceProvider();
ActionExecutingContext Auth(string key)
{
    var http = new DefaultHttpContext { RequestServices = services };
    http.Request.Headers["X-Rental-Key"] = key;
    var ctx = new ActionExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    new RentalAccessAttribute().OnActionExecuting(ctx);
    return ctx;
}
Check(Auth("").Result is UnauthorizedResult, "missing management key rejected");
Check(Auth(new string('x', 32)).Result is UnauthorizedResult, "wrong management key rejected");
Check(Auth(new string('k', 32)).Result is null, "correct management key accepted");

using var db = new RentalDb(new DbContextOptionsBuilder<RentalDb>().UseSqlServer("Server=localhost;Database=AI_TOOL;Integrated Security=true").Options);
var script = db.Database.GenerateCreateScript();
Check(script.Contains("[rental].[Rooms]") && script.Contains("[rental].[Appointments]"), "SQL Server rental schema generated without connecting");
Check(!script.Contains("AspNetUsers"), "bootstrap excludes existing application tables");
Check(db.Model.FindEntityType(typeof(RentalDelivery))!.GetIndexes().Any(i => i.IsUnique && i.Properties.Count == 2), "unique post/group delivery");
Check(db.Model.FindEntityType(typeof(RentalAppointment))!.GetIndexes().Any(i => i.IsUnique), "unique appointment per source message");

var handler = new AiHandler();
var ai = new RentalAi(new Factory(handler), Options.Create(new RentalOptions { AiApiKey = "test", AiModel = "test", AiBaseUrl = "https://ai.test/v1/" }));
handler.Content = "Bài đăng phòng trọ";
Check(await ai.Draft(new RentalRoom { Name = "Phòng A" }, "Viết ngắn", default) == handler.Content, "draft through real AI adapter with fake transport");
Check(handler.RequestUri == "https://ai.test/v1/chat/completions", "AI endpoint composed correctly");
handler.Content = "```json\n{\"reply\":\"Bạn muốn xem phòng lúc nào?\",\"appointment\":null}\n```";
Check((await ai.Answer(new(), "Khách: Xin giá phòng", default)).Appointment is null, "parse fenced structured reply");
handler.Content = "{}";
bool rejected = false;
try { await ai.Answer(new(), "hello", default); } catch (InvalidOperationException) { rejected = true; }
Check(rejected, "reject incomplete AI response");
handler.Status = HttpStatusCode.Unauthorized;
rejected = false;
try { await ai.Draft(new(), "hello", default); } catch (InvalidOperationException ex) { rejected = !ex.Message.Contains("test-key"); }
Check(rejected, "provider failure has no raw response or credentials");
await ListingChecks.Run(Check);
await DesktopChecks.Run(Check);
if (args.Length >= 3 && args[0] == "--desktop-smoke") await DesktopChecks.Smoke(args[1], args[2], Check);
Console.WriteLine($"{checks} checks passed.");

class Factory(AiHandler handler) : IHttpClientFactory { public HttpClient CreateClient(string name) => new(handler, false); }
class AiHandler : HttpMessageHandler
{
    public string Content = "";
    public string RequestUri = "";
    public HttpStatusCode Status = HttpStatusCode.OK;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        RequestUri = request.RequestUri!.ToString();
        return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = Content } } } }), Encoding.UTF8, "application/json") });
    }
}

