using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Brand
{
    public class CreateBrandRequest
    {
        [Required(ErrorMessage = "Tên thương hiệu là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên thương hiệu không được vượt quá 255 ký tự")]
        public string TenThuongHieu { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn logo không được vượt quá 255 ký tự")]
        public string? Logo { get; set; }

        public string TrangThai { get; set; } = "Hoạt động";
    }
}
