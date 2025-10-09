using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Category
{
    public class UpdateCategoryRequest
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên danh mục không được vượt quá 255 ký tự")]
        public string TenDanhMuc { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 255 ký tự")]
        public string? HinhAnh { get; set; }

        public string TrangThai { get; set; } = "Hoạt động";
    }
}
