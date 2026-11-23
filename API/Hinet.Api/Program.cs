using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Hinet.Model;
using Hinet.Api;
using Hinet.Extensions;
using Serilog;
using System.Security.Authentication;

internal class Program
{
    private static void InitializeDatabase(HinetContext db, IServiceProvider services)
    {
        if (db.Database.GetDbConnection().Database != "AI_TOOL")
            throw new InvalidOperationException("Database initialization only supports AI_TOOL.");
        db.Database.Migrate();
        RentalBootstrap.InitializeAsync(services).GetAwaiter().GetResult();
        Console.WriteLine("AI_TOOL: SQL Server database, account tables and rental schema ready.");
    }

    private static void Main(string[] args)
    {
        if (args.Contains("--rental-desktop"))
        {
            try { RentalDesktop.RunAsync(args).GetAwaiter().GetResult(); }
            catch (IOException)
            {
                Console.Error.WriteLine("Không mở được dữ liệu. Kiểm tra ứng dụng đã chạy ở cửa sổ khác hoặc quyền ghi thư mục App_Data.");
                Environment.ExitCode = 1;
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Không khởi động được AI TOOL. Kiểm tra .NET 8, cổng đang sử dụng và nhật ký phía trên.");
                Environment.ExitCode = 1;
            }
            return;
        }

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRentalAssistant(builder.Configuration);
        // Telegram bot tokens are part of the URL; do not log external request URLs.
        builder.Logging.AddFilter("System.Net.Http.HttpClient.RentalExternal", LogLevel.None);
        if (args.Contains("--init-rental-db"))
        {
            using var setup = builder.Build();
            RentalBootstrap.InitializeAsync(setup.Services).GetAwaiter().GetResult();
            return;
        }
        builder.WebHost.UseUrls("http://0.0.0.0:5111");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();
        builder.Host.UseSerilog();

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            });
        });

        builder.Configuration.UseAppSettings();

        builder.Services.AddHttpClient("FileServerClient", client =>
        {
            var baseUrl = builder.Configuration["FileServer:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Missing config: FileServer:BaseUrl");

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Invalid FileServer:BaseUrl = '{baseUrl}'");

            client.BaseAddress = uri;
        });

        builder.Services.AddMemoryCache();
        builder.Services.UseConfigurationServices();
        builder.Services.AddTransient<ExceptionHandlingMiddleware>();

        // Rate limiting: chống brute-force endpoint đăng nhập (5 req / 1 phút / IP)
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        var app = builder.Build();

        if (args.Contains("--init-db") || args.Contains("--script-db"))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HinetContext>();
            if (args.Contains("--script-db"))
            {
                var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RentalDb>>();
                using var rental = factory.CreateDbContext();
                var rentalScript = System.Text.RegularExpressions.Regex.Replace(rental.Database.GenerateCreateScript(), @"^\s*GO\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var setup = "USE [master];\nGO\nIF DB_ID(N'AI_TOOL') IS NULL CREATE DATABASE [AI_TOOL];\nGO\nUSE [AI_TOOL];\nGO\n" + script;
                setup += "\nGO\nSET XACT_ABORT ON;\nBEGIN TRANSACTION;\nIF OBJECT_ID(N'rental.Rooms', N'U') IS NULL\nBEGIN\n" + rentalScript + "\nEND;\n";
                setup += "IF NOT EXISTS (SELECT 1 FROM [rental].[State]) BEGIN SET IDENTITY_INSERT [rental].[State] ON; INSERT INTO [rental].[State] ([Id], [TelegramOffset], [Paused]) VALUES (1, 0, 1); SET IDENTITY_INSERT [rental].[State] OFF; END;\nCOMMIT;\nGO\nSELECT DB_NAME() AS DatabaseName, UserName FROM dbo.AspNetUsers WHERE NormalizedUserName = N'ADMIN';\n";
                File.WriteAllText("AI_TOOL.SqlServer.sql", setup, System.Text.Encoding.UTF8);
                Console.WriteLine("Generated AI_TOOL.SqlServer.sql (no database connection required).");
            }
            else
            {
                InitializeDatabase(db, scope.ServiceProvider);
            }
            return;
        }

        // Swagger chỉ bật ở môi trường dev (production không expose API docs)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin Base API");
            });
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();

        // Security headers chuẩn cho mọi response.
        app.Use(async (ctx, next) =>
        {
            var headers = ctx.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-XSS-Protection"] = "0"; // tắt legacy filter, dựa vào CSP
            // Strict-Transport-Security chỉ khi chạy HTTPS production:
            // headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            await next();
        });

        app.UseRouting();

        app.UseCors(c =>
        {
            c.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.MapControllers();

        app.Run();
    }
}
