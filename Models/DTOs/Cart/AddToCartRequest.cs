using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Cart
{
    public class AddToCartRequest
    {
        [Required(ErrorMessage = "ID sản phẩm là bắt buộc")]
        public int SanPhamId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; } = 1;
    }
}
