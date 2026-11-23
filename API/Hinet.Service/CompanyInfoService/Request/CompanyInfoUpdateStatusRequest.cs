namespace Hinet.Service.CompanyInfoService.Request
{
    public class CompanyInfoUpdateStatusRequest
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
    }
}
