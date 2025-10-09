using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Wishlist
{
    public class AddToWishlistRequest
    {
        [Required(ErrorMessage = "ID sản phẩm là bắt buộc")]
        public int IdSanPham { get; set; }
    }
}
