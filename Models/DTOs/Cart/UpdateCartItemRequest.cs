using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Cart
{
    public class UpdateCartItemRequest
    {
        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }
    }
}
