namespace PawVerseAPI.Models.DTOs.Order
{
    public class OrderItemDto
    {
        public int IdChiTietDonHang { get; set; }
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}
