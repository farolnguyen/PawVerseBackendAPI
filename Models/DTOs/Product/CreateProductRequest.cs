using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Product
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên sản phẩm không được vượt quá 255 ký tự")]
        public string TenSanPham { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên alias là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên alias không được vượt quá 255 ký tự")]
        public string TenAlias { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        public int IdDanhMuc { get; set; }

        [Required(ErrorMessage = "Thương hiệu là bắt buộc")]
        public int IdThuongHieu { get; set; }

        [Required(ErrorMessage = "Trọng lượng là bắt buộc")]
        [StringLength(50, ErrorMessage = "Trọng lượng không được vượt quá 50 ký tự")]
        public string TrongLuong { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Màu sắc không được vượt quá 50 ký tự")]
        public string? MauSac { get; set; }

        [StringLength(100, ErrorMessage = "Xuất xứ không được vượt quá 100 ký tự")]
        public string? XuatXu { get; set; }

        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải >= 0")]
        public int SoLuongTonKho { get; set; }

        [Required(ErrorMessage = "Giá bán là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải > 0")]
        public decimal GiaBan { get; set; }

        [Required(ErrorMessage = "Giá vốn là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá vốn phải >= 0")]
        public decimal GiaVon { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi phải >= 0")]
        public decimal? GiaKhuyenMai { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 255 ký tự")]
        public string? HinhAnh { get; set; }

        [Required(ErrorMessage = "Ngày sản xuất là bắt buộc")]
        public DateTime NgaySanXuat { get; set; }

        [Required(ErrorMessage = "Hạn sử dụng là bắt buộc")]
        public DateTime HanSuDung { get; set; }

        [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
        public string TrangThai { get; set; } = "Còn hàng";
    }
}
