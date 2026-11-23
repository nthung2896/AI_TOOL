using Hinet.Extensions;
using Hinet.Service.MoitSsoService.Dto;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hinet.Service.MoitSsoService
{
    public class MoitSsoService : IMoitSsoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MoitSsoService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public MoitSsoService(IHttpClientFactory httpClientFactory, ILogger<MoitSsoService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string BuildLoginUrl()
        {
            var cfg = AppSettings.MoitSso;
            var url = Combine(cfg.BaseUrl, "Login.aspx");
            return $"{url}?clientkey={Uri.EscapeDataString(cfg.ClientKey)}&url={Uri.EscapeDataString(cfg.ReceiverUrl)}";
        }

        public string BuildLogoutUrl(string returnUrl)
        {
            var cfg = AppSettings.MoitSso;
            var url = Combine(cfg.BaseUrl, "Logout.aspx");
            return $"{url}?url={Uri.EscapeDataString(returnUrl)}";
        }

        public async Task<MoitUserInfo?> GetUserInfoAsync(string token)
        {
            var cfg = AppSettings.MoitSso;
            var url = Combine(cfg.BaseUrl, "api/authen/userInfo/data.moit")
                + $"?clientkey={Uri.EscapeDataString(cfg.ClientKey)}&token={Uri.EscapeDataString(token)}";
            return await GetMoitDataAsync<MoitUserInfo>(url);
        }

        public async Task<MoitBusinessInfo?> GetBusinessInfoAsync(string token)
        {
            var cfg = AppSettings.MoitSso;
            var url = Combine(cfg.BaseUrl, "api/authen/businessInfo/data.moit")
                + $"?clientkey={Uri.EscapeDataString(cfg.ClientKey)}&token={Uri.EscapeDataString(token)}";
            return await GetMoitDataAsync<MoitBusinessInfo>(url);
        }

        public async Task<bool> UpdateBusinessAsync(string taxCode, int status, string? note)
        {
            var cfg = AppSettings.MoitSso;
            var url = Combine(cfg.BaseUrl, "api/update/business/data.moit")
                + $"?clientkey={Uri.EscapeDataString(cfg.ClientKey)}"
                + $"&tc={Uri.EscapeDataString(taxCode)}"
                + $"&st={status}"
                + $"&note={Uri.EscapeDataString(note ?? string.Empty)}";

            try
            {
                var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(30);
                var json = await http.GetStringAsync(url);
                var resp = JsonSerializer.Deserialize<MoitResponse<object>>(json, JsonOpts);
                return resp?.IsSuccess == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoitSso UpdateBusiness thất bại: tc={Tc}", taxCode);
                return false;
            }
        }

        // ===== Helpers =====

        private async Task<T?> GetMoitDataAsync<T>(string url)
        {
            try
            {
                var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(30);
                var json = await http.GetStringAsync(url);
                var resp = JsonSerializer.Deserialize<MoitResponse<T>>(json, JsonOpts);
                if (resp == null)
                {
                    _logger.LogWarning("MoitSso: phản hồi rỗng từ {Url}", url);
                    return default;
                }
                if (!resp.IsSuccess)
                {
                    _logger.LogWarning("MoitSso: lỗi code={Code} note={Note} url={Url}", resp.Message, resp.Note, url);
                    return default;
                }
                return resp.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoitSso: gọi {Url} thất bại", url);
                return default;
            }
        }

        private static string Combine(string baseUrl, string path)
        {
            if (string.IsNullOrEmpty(baseUrl)) return path;
            return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
        }
    }
}
