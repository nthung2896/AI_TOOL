using Hinet.Model.Entities;
using Hinet.Service.AppUserService.Dto;
using Hinet.Service.AppUserService.Request;
using Hinet.Service.Common.Service;
using Hinet.Service.MoitSsoService.Dto;

namespace Hinet.Service.AppUserService
{
    public interface IAppUserService : IService<AppUser>
    {
        Task<LoginResponseDto> LoginUser(string email, string password);

        Task<string> ResetPassword(string email, string baseUri);

        Task<AppUserDto> ChangePassword(Guid? id, string oldPassword, string newPassword, string confirmPassword);

        Task<LoginResponseDto> RefreshToken(string refreshToken);

        Task<AppUserDto> CheckLogin(Guid? id);

        Task LogoutUser();

        Task<AppUser?> GetByUserName(string UserName);

        Task<AppUserDto> GetInfo(Guid? id);

        Task<AppUserDto> GetDto(Guid? id);

        Task<AppUserDto> RegisterUser(RegisterViewModel model);

        Task<AppUserDto> UpdateProfile(ProfileUserEditRequest dto);

        Task<AppUserDto> UpdateAvatarUser(Stream stream, string? fileName, string? fileType, Guid userId, Guid? itemId = null);

        // Upsert tài khoản + CompanyInfo từ thông tin SSO của Cổng DVC, trả về JWT.
        Task<LoginResponseDto> LoginBySsoAsync(MoitUserInfo userInfo, MoitBusinessInfo? businessInfo);
    }
}