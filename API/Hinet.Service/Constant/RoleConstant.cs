using System.ComponentModel;

namespace Hinet.Service.Constant
{
    public class RoleConstant
    {
        [DisplayName("Quản trị viên")]
        public static string Admin => "Admin";

        [DisplayName("Doanh nghiệp")]
        public static string DoanhNghiep => "DoanhNghiep";

        [DisplayName("Chuyên viên sở")]
        public static string ChuyenVienSo => "ChuyenVienSo";

        [DisplayName("Trưởng phòng sở")]
        public static string TruongPhongSo => "TruongPhongSo";

        [DisplayName("Lãnh đạo sở")]
        public static string LanhDaoSo => "LanhDaoSo";

        [DisplayName("Chuyên viên cục")]
        public static string ChuyenVienCuc => "ChuyenVienCuc";

        [DisplayName("Trưởng phòng cục")]
        public static string TruongPhongCuc => "TruongPhongCuc";

        [DisplayName("Lãnh đạo cục")]
        public static string LanhDaoCuc => "LanhDaoCuc";
    }
}
