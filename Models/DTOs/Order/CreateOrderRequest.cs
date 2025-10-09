using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Order
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên khách hàng không được vượt quá 255 ký tự")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        public string DiaChiGiaoHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string PhuongThucThanhToan { get; set; } = string.Empty;

        public int IdVanChuyen { get; set; } = 1; // Default shipping method

        public int? IdCoupon { get; set; }

        public string? GhiChu { get; set; }
    }
}
