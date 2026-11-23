using System.ComponentModel;

namespace Hinet.Service.Constant
{
    public class PlatformStatusConstant
    {
        [DisplayName("Tạm lưu")]                  public static int TamLuu => 0;
        [DisplayName("Chờ duyệt")]                public static int ChoDuyet => 1;
        [DisplayName("Đề nghị chỉnh sửa")]        public static int DeNghiChinhSua => 2;
        [DisplayName("Bị từ chối")]               public static int BiTuChoi => 3;
        [DisplayName("Đã duyệt điện tử")]         public static int DaDuyetDienTu => 4;
        [DisplayName("Đã xác nhận")]              public static int DaXacNhan => 5;
        [DisplayName("Cần bổ sung thông tin")]    public static int CanBoSungThongTin => 6;
        [DisplayName("Đã chấm dứt đăng ký")]      public static int DaChamDutDangKy => 7;
        [DisplayName("Đã huỷ đăng ký")]           public static int DaHuyDangKy => 8;
        [DisplayName("Đề nghị chấm dứt đăng ký")] public static int DeNghiChamDutDangKy => 9;
        [DisplayName("Đã khoá")]                  public static int DaKhoa => 10;
        [DisplayName("Đã yêu cầu gia hạn")]       public static int DaYeuCauGiaHan => 11;
        [DisplayName("Chờ gia hạn")]              public static int ChoGiaHan => 12;
        [DisplayName("Đang xin ý kiến")]          public static int DangXinYKien => 25;
        [DisplayName("Đã review")]                public static int DaReview => 26;
        [DisplayName("Không hợp lệ")]             public static int KhongHopLe => 27;
        [DisplayName("Cần bản giấy")]             public static int CanBanGiay => 28;
    }
}
