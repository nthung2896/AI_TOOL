namespace Hinet.Service.MoitSsoService.Dto
{
    // Object trả về từ API GetUserInfo của Cổng DVC
    public class MoitUserInfo
    {
        public long Id { get; set; }
        public string? LoginName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
    }
}
