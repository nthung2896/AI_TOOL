using Hinet.Api.Dto;
using Hinet.Model.Entities;
using Hinet.Service.AppUserService;
using Hinet.Service.AppUserService.Dto;
using Hinet.Service.AppUserService.Request;
using Hinet.Service.Core.Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hinet.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : HinetController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IAppUserService _userService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMapper _mapper;

        public AccountController(
            IAppUserService AuthService,
            ILogger<AccountController> logger,
            UserManager<AppUser> userManager
,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IMapper mapper)
        {
            _logger = logger;
            _userService = AuthService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _mapper = mapper;
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<DataResponse<LoginResponseDto>> Login([FromBody] LoginViewModel model)
        {
            try
            {
                var result = await _userService.LoginUser(model.UserName, model.Password);

                return new DataResponse<LoginResponseDto>
                {
                    Data = result,
                    Message = "Đăng nhập thành công",
                    Status = true
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("Logout")]
        public async Task<DataResponse> Logout()
        {
            await _userService.LogoutUser();
            return new DataResponse
            {
                Message = "Đăng xuất thành công",
                Status = true
            };
        }

        [HttpGet("GetInfo")]
        [Authorize]
        public async Task<DataResponse<AppUserDto>> GetInfo()
        {
            var result = await _userService.GetInfo(UserId);

            return new DataResponse<AppUserDto>
            {
                Data = result,
                Message = "Lấy thông tin tài khoản thành công",
                Status = true
            };
        }

        [HttpPut("UpdateProfile")]
        [Authorize]
        public async Task<DataResponse<AppUserDto>> UpdateInfo([FromBody] ProfileUserEditRequest edit)
        {
            try
            {
                var newInfo = await _userService.UpdateProfile(edit);
                return DataResponse<AppUserDto>.Success(newInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật profile với Id: {Id}", edit.Id);
                return new DataResponse<AppUserDto>()
                {
                    Data = null,
                    Status = false,
                    Message = ex.Message,
                };
            }
        }

        [HttpPut("UpdateProfile/UpdateAvatar")]
        //ignore-sync
        public async Task<DataResponse<AppUserDto>> UpdateAvatar([FromForm] IFormFile avatar)
        {
            try
            {
                if (avatar == null || avatar.Length == 0)
                    return DataResponse<AppUserDto>.False("File rỗng");

                var result = await _userService.UpdateAvatarUser(
                    avatar.OpenReadStream(),
                    avatar.FileName,
                    avatar.ContentType,
                    (Guid)UserId!);
                return DataResponse<AppUserDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật profile với Id: {Id}", UserId);
                return new DataResponse<AppUserDto>()
                {
                    Data = null,
                    Status = false,
                    Message = ex.Message,
                };
            }
        }


        [HttpPut("UpdateProfile/ChangePassword")]
        [Authorize]
        public async Task<DataResponse<AppUserDto>> ChangePassword([FromBody] ChangePasswordViewModel vm)
        {
            try
            {
                var newInfo = await _userService.ChangePassword(UserId, vm.OldPassword, vm.NewPassword, vm.ConfirmPassword);
                return DataResponse<AppUserDto>.Success(newInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật password với Id: {Id}", UserId);
                return new DataResponse<AppUserDto>()
                {
                    Data = null,
                    Status = false,
                    Message = ex.Message,
                };
            }
        }


        // Reset password tài khoản khác – CHỈ admin được phép.
        [HttpGet("ResetPassword")]
        [Authorize(Roles = "Admin")]
        public async Task<DataResponse<string>> ResetPasswordById(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return DataResponse<string>.False("User not found");
            }

            // Sinh password ngẫu nhiên 12 ký tự (chữ hoa, chữ thường, số, ký tự đặc biệt)
            var newPassword = GenerateRandomPassword(12);

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, newPassword);

            if (!result.Succeeded)
            {
                return DataResponse<string>.False(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }

            // Trả password 1 lần cho admin để chuyển cho user qua kênh an toàn.
            // Production nên gửi qua email/SMS thay vì trả về API.
            return DataResponse<string>.Success(newPassword);
        }

        private static string GenerateRandomPassword(int length)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghjkmnpqrstuvwxyz";
            const string digit = "23456789";
            const string special = "!@#$%&*";
            var all = upper + lower + digit + special;
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            // đảm bảo có ít nhất 1 ký tự mỗi nhóm
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = lower[bytes[1] % lower.Length];
            chars[2] = digit[bytes[2] % digit.Length];
            chars[3] = special[bytes[3] % special.Length];
            for (int i = 4; i < length; i++) chars[i] = all[bytes[i] % all.Length];
            // shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = bytes[i] % (i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}