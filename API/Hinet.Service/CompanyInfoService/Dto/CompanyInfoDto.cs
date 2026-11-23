using Hinet.Model.Entities;

namespace Hinet.Service.CompanyInfoService.Dto
{
    public class CompanyInfoDto : CompanyInfo
    {
        public string? StatusName { get; set; }
        public string? TypeOrganizationName { get; set; }
    }
}
