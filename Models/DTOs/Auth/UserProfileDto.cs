using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Auth
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }
        
        public string? DiaChi { get; set; }
        public string? GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? Avatar { get; set; }
    }

    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }
        
        public string? DiaChi { get; set; }
        public string? GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
    }
}
