using Hinet.Service.MoitSsoService.Dto;

namespace Hinet.Service.MoitSsoService
{
    public interface IMoitSsoService
    {
        // Sinh URL redirect tới trang đăng nhập của Cổng DVC.
        // Sau khi user đăng nhập xong, Cổng sẽ redirect về ReceiverUrl?token=...
        string BuildLoginUrl();

        // Sinh URL redirect tới trang logout của Cổng DVC.
        string BuildLogoutUrl(string returnUrl);

        // Gọi API GetUserInfo bằng token nhận từ callback.
        Task<MoitUserInfo?> GetUserInfoAsync(string token);

        // Gọi API GetBusinessInfo bằng token nhận từ callback.
        Task<MoitBusinessInfo?> GetBusinessInfoAsync(string token);

        // Gọi API UpdateBusiness để cập nhật trạng thái hồ sơ DN lên Cổng.
        // status: 1-DaDuyet, 2-TuChoi, 3-BoSung, 4-Khoa
        Task<bool> UpdateBusinessAsync(string taxCode, int status, string? note);
    }
}
