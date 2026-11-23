using Hinet.FileServer.Helper.FileHelper;
using Hinet.FileServer.Services.TaiLieuDinhKemService;

namespace Hinet.FileServer.Middleware
{
    public class FileServerMiddleware
    {
        private readonly RequestDelegate _next;

        public FileServerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            if (context.Request.Method == "GET" && context.Request.Path.StartsWithSegments("/uploads"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (context.Request.Method == "GET" &&
                context.Request.Path.StartsWithSegments("/fileserver", out var remaining) && remaining.HasValue)
            {
                // Extract: /fileserver/{id}/{filename}
                var segments = remaining.Value.Trim('/').Split('/');
                if (segments.Length >= 1)
                {
                    if (Guid.TryParse(segments[0], out var id))
                    {
                        var taiLieuDinhKemService = context.RequestServices.GetRequiredService<ITaiLieuDinhKemService>();
                        var tailieu = await taiLieuDinhKemService.GetById(id);

                        if (tailieu != null && !string.IsNullOrEmpty(tailieu.DuongDanFile))
                        {
                            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                            string filePath;
                            try { filePath = SafePath.CombineSafe(uploadsRoot, tailieu.DuongDanFile); }
                            catch (PathTraversalException)
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsync("Invalid file path");
                                return;
                            }
                            if (File.Exists(filePath))
                            {
                                context.Request.Path = $"/uploads/{tailieu.DuongDanFile}";
                                await _next(context);
                                return;
                            }
                        }
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync("File not found");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}
