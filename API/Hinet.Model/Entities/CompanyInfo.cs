using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hinet.Model.Entities
{
    [Table("CompanyInfo")]
    public class CompanyInfo : AuditableEntity
    {
        // ===== Trạng thái =====
        [DisplayName("Trạng thái")]
        public int Status { get; set; }

        // ===== Thông tin doanh nghiệp (đồng bộ từ cổng DVC) =====
        [DisplayName("Tên doanh nghiệp")]
        public string? Name { get; set; }

        [DisplayName("Tên tiếng Anh")]
        public string? EnglishName { get; set; }

        [DisplayName("Tên viết tắt")]
        public string? ShortName { get; set; }

        [DisplayName("Mã số thuế")]
        public string? TaxCode { get; set; }

        [DisplayName("Địa chỉ")]
        public string? Address { get; set; }

        [DisplayName("Mã tỉnh/thành (cổng DVC)")]
        public string? CityId { get; set; }

        [DisplayName("Tên tỉnh/thành (cổng DVC)")]
        public string? CityName { get; set; }

        [DisplayName("Điện thoại")]
        public string? Phone { get; set; }

        [DisplayName("Fax")]
        public string? Fax { get; set; }

        [DisplayName("Email")]
        public string? Email { get; set; }

        // ===== Người đại diện =====
        [DisplayName("Tên người đại diện")]
        public string? RepresenterName { get; set; }

        [DisplayName("Di động người đại diện")]
        public string? RepresenterMobile { get; set; }

        [DisplayName("Điện thoại người đại diện")]
        public string? RepresenterPhone { get; set; }

        [DisplayName("Email người đại diện")]
        public string? RepresenterEmail { get; set; }

        [DisplayName("CCCD/Hộ chiếu người đại diện")]
        public string? RepresenterCCCD { get; set; }

        // ===== Thông tin bổ sung =====
        [DisplayName("Chi tiết")]
        public string? Detail { get; set; }

        [DisplayName("Ngày duyệt trên cổng DVC")]
        public DateTime? ApproveDateOnline { get; set; }

        // ===== Tỉnh nội bộ =====
        [DisplayName("Id tỉnh (mã cũ)")]
        public Guid? TinhId { get; set; }

        [DisplayName("Id tỉnh (mã mới sau sáp nhập)")]
        public Guid? TinhIdNew { get; set; }

        [DisplayName("Id xã")]
        public Guid? XaId { get; set; }

        [DisplayName("Mã quốc gia")]
        public string? QuocGiaId { get; set; } = "VN";

        // ===== Phân loại =====
        [DisplayName("Loại tổ chức")]
        public string? TypeOrganization { get; set; }

        [DisplayName("Có yếu tố nước ngoài")]
        public bool IsNuocNgoai { get; set; }

        [DisplayName("Có vốn đầu tư nước ngoài")]
        public bool IsVonDauTuNuocNgoai { get; set; }

        // ===== File đăng ký kinh doanh =====
        [DisplayName("File ĐKKD")]
        public string? DKKD { get; set; }
    }
}
