using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Order
{
    public class UpdateOrderStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string TrangThai { get; set; } = string.Empty;

        public DateTime? NgayGiaoHangDuKien { get; set; }
    }
}
