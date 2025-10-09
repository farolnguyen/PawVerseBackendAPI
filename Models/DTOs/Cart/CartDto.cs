namespace PawVerseAPI.Models.DTOs.Cart
{
    public class CartDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        
        // Computed properties
        public int TongSoLuong => Items.Sum(i => i.SoLuong);
        public decimal TongTien => Items.Sum(i => i.ThanhTien);
        public int SoMucHang => Items.Count;
    }
}
