using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Hinet.Model.Entities
{
    [Table("RutTienKyQuy")]
    public class RutTienKyQuy : AuditableEntity
    {
        // === KHỐI MÀU XANH LÁ: THÔNG TIN TỔ CHỨC / CHỦ QUẢN ===
        
        [Required]
        [StringLength(500)]
        public string TenChuQuanToChuc { get; set; } // B: Tên chủ quản/Tổ chức

        [StringLength(250)]
        public string? SoNgayNoiCapGCN { get; set; } // C: Số, ngày cấp, nơi cấp GCN/QĐ

        [StringLength(50)]
        public string? MaSoThue { get; set; } // D: Mã số thuế

        [StringLength(500)]
        public string? DiaChiTruSoChinh { get; set; } // E: Địa chỉ trụ sở chính

        [StringLength(250)]
        [EmailAddress]
        public string? EmailTiepNhan { get; set; } // F: Email tiếp nhận (Chủ quản)

        [StringLength(50)]
        public string? SdtToChuc { get; set; } // G: SĐT (Tổ chức)

        // --- Đại diện pháp luật (ĐDPL) ---
        [StringLength(250)]
        public string? DdplHoVaTen { get; set; } // H: ĐDPL: Họ và tên

        [StringLength(150)]
        public string? DdplChucDanh { get; set; } // I: ĐDPL: Chức danh

        [StringLength(50)]
        public string? DdplSoCccdHoChieu { get; set; } // J: ĐDPL: Số CCCD/Hộ chiếu

        [StringLength(500)]
        public string? DdplDiaChi { get; set; } // K: ĐDPL: Địa chỉ

        [StringLength(50)]
        public string? DdplSdt { get; set; } // L: ĐDPL: SĐT

        // --- Đầu mối liên hệ (ĐMLH) ---
        [StringLength(250)]
        public string? DmlhHoVaTen { get; set; } // M: Đầu mối liên hệ: Họ và tên

        [StringLength(50)]
        public string? DmlhSdt { get; set; } // N: ĐMLH: SĐT

        // --- Thông tin khác ---
        [StringLength(500)]
        public string? WebDiaChi { get; set; } // O: Web: Địa chỉ

        [StringLength(250)]
        public string? AppTenUngDung { get; set; } // P: App: Tên ứng dụng


        // === KHỐI TRẮNG: THÔNG TIN GIAO DỊCH VÀ PHÁP LÝ ===

        [StringLength(250)]
        public string? NganHang { get; set; } // Q: Ngân hàng

        [StringLength(100)]
        public string? QuySo { get; set; } // R: Quỹ số

        public DateTime? Ngay { get; set; } // S: Ngày

        public string? LyDo { get; set; } // T: Lý do

        public string? VanBanTaiLieuKemTheo { get; set; } // U: văn bản tài liệu kèm theo

        [StringLength(100)]
        public string? Khoan { get; set; } // V: khoản

        [StringLength(100)]
        public string? Dieu { get; set; } // W: điều

        [StringLength(250)]
        public string? NghiDinh { get; set; } // X: nghị định

        [StringLength(100)]
        public string? SoVanBan { get; set; } // Y: Số văn bản

        [StringLength(500)]
        public string? DiaChiNgayThangNam { get; set; } // Z: địa chỉ, ngày, tháng, năm
    }
}