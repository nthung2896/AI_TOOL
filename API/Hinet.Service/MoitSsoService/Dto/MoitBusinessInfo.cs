namespace Hinet.Service.MoitSsoService.Dto
{
    // Object trả về từ API GetBusinessInfo của Cổng DVC
    public class MoitBusinessInfo
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? EnglishName { get; set; }
        public string? ShortName { get; set; }
        public string? TaxCode { get; set; }
        public int? TypeId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? Detail { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public string? Fax { get; set; }
        public string? RepresenterEmail { get; set; }
        public string? RepresenterMobile { get; set; }
        public string? RepresenterName { get; set; }
        public DateTime? SubmitedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RegisterFileLink { get; set; }
        public string? NumberId { get; set; }
        public List<MoitBusinessProperty>? Properties { get; set; }
    }

    public class MoitBusinessProperty
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}
