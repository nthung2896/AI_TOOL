using System.ComponentModel.DataAnnotations.Schema;

namespace Hinet.Model.Entities
{
    [Table("PlatformManage")]
    public class PlatformManage : AuditableEntity
    {
        public int Status { get; set; }
        public int? AppManageTypeId { get; set; }
        public long? OrganizationId { get; set; }
        public string? TypeOrganization { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyTaxCode { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyFax { get; set; }
        public string? Name { get; set; }
        public string? Domain { get; set; }
        public string? ISPId { get; set; }
        public int? StaffNumber { get; set; }
        public string? RepresenterName { get; set; }
        public string? RepresenterMobile { get; set; }
        public string? RepresenterJob { get; set; }
        public string? RepresenterEmail { get; set; }
        public string? Detail { get; set; }
        public DateTime? SubmitDate { get; set; }
        public string? SubmitName { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? ReviewName { get; set; }
        public DateTime? RequestChangeDate { get; set; }
        public string? RequestChangeName { get; set; }
        public string? WebsiteNumber { get; set; }
        public string? Seal { get; set; }
        public string? DomainAdd { get; set; }
        public string? ImagePath { get; set; }
        public string? TinhId { get; set; }
        public string? XaId { get; set; }
        public string? QuocGiaId { get; set; }
        public string? TienIchKhac { get; set; }
        public string? LoaiHangHoaKhac { get; set; }
        public long? OldSysId { get; set; }
        public DateTime? DateLine { get; set; }
        public string? Address { get; set; }
        public string? PhuongThucLienHe { get; set; }
        public string? RepresenterNameOnline { get; set; }
        public string? RepresenterJobOnline { get; set; }
        public string? RepresenterMobileOnline { get; set; }
        public string? RepresenterEmailOnline { get; set; }
        public string? AddressOnline { get; set; }
        public string? PhuongThucLienHeOnline { get; set; }
        public string? RepresenterNameDaiDien { get; set; }
        public string? RepresenterJobDaiDien { get; set; }
        public string? RepresenterMobileDaiDien { get; set; }
        public string? RepresenterEmailDaiDien { get; set; }
        public string? AddressDaiDien { get; set; }
        public string? PhuongThucLienHeDaiDien { get; set; }
        public string? API { get; set; }
        public string? URLApp { get; set; }
        public string? UserApp { get; set; }
        public string? PassApp { get; set; }
        public bool? IsNuocNgoai { get; set; }
        public string? SoCongVan { get; set; }
        public string? CCCD { get; set; }
        public string? RepresenterCCCD { get; set; }
        public string? DataSignedUser { get; set; }
        public DateTime? SignDateUser { get; set; }
        public string? ISPIdKhac { get; set; }
        public string? TinhIdNew { get; set; }
    }
}
