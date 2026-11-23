using Hinet.Service.Dto;

namespace Hinet.Service.CompanyInfoService.Request
{
    public class CompanyInfoSearch : SearchBase
    {
        public string? Keyword { get; set; }
        public string? TaxCode { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public string? TypeOrganization { get; set; }
        public Guid? TinhId { get; set; }
        public Guid? TinhIdNew { get; set; }
        public bool? IsNuocNgoai { get; set; }
    }
}
