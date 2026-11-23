using Hinet.Extensions;
using Hinet.Service.AppUserService;
using Hinet.Service.MoitSsoService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hinet.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class MoitSsoController : ControllerBase
    {
        private readonly IMoitSsoService _moitSso;
        private readonly IAppUserService _userService;
        private readonly ILogger<MoitSsoController> _logger;

        public MoitSsoController(
            IMoitSsoService moitSso,
            IAppUserService userService,
            ILogger<MoitSsoController> logger)
        {
            _moitSso = moitSso;
            _userService = userService;
            _logger = logger;
        }

        // GET /api/MoitSso/InitiateLogin
        // Redirect 302 sang trang đăng nhập của Cổng DVC.
        [HttpGet("InitiateLogin")]
        public IActionResult InitiateLogin()
        {
            var url = _moitSso.BuildLoginUrl();
            return Redirect(url);
        }

        // GET /api/MoitSso/Callback?token=...
        // Cổng DVC redirect về đây sau khi user đăng nhập thành công.
        [HttpGet("Callback")]
        public async Task<IActionResult> Callback([FromQuery] string token)
        {
            var feUrl = AppSettings.MoitSso.FrontendCallbackUrl;
            if (string.IsNullOrWhiteSpace(token))
            {
                return Redirect($"{feUrl}?error=missing_token");
            }

            try
            {
                var userInfo = await _moitSso.GetUserInfoAsync(token);
                if (userInfo == null)
                {
                    return Redirect($"{feUrl}?error=get_user_info_failed");
                }

                // Thử lấy business info (sẽ null nếu là tài khoản chuyên viên — không có MST hợp lệ)
                var businessInfo = await _moitSso.GetBusinessInfoAsync(token);

                var loginResp = await _userService.LoginBySsoAsync(userInfo, businessInfo);

                // Truyền JWT về FE qua query string. FE sẽ đọc và lưu vào localStorage.
                return Redirect($"{feUrl}?token={Uri.EscapeDataString(loginResp.Token ?? string.Empty)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoitSso callback thất bại");
                return Redirect($"{feUrl}?error={Uri.EscapeDataString(ex.Message)}");
            }
        }

        // GET /api/MoitSso/Logout?returnUrl=...
        // Trả URL logout của Cổng DVC để FE redirect tới.
        [HttpGet("LogoutUrl")]
        public IActionResult LogoutUrl([FromQuery] string returnUrl)
        {
            return Ok(new { url = _moitSso.BuildLogoutUrl(returnUrl) });
        }
    }
}
