namespace PawVerseAPI.Models.DTOs.Cart
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public decimal GiaBan { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int SoLuong { get; set; }
        public int SoLuongTonKho { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        
        // Computed properties
        public decimal GiaHienThi => GiaKhuyenMai ?? GiaBan;
        public decimal ThanhTien => GiaHienThi * SoLuong;
        public bool CoKhuyenMai => GiaKhuyenMai.HasValue && GiaKhuyenMai < GiaBan;
        public bool ConHang => SoLuongTonKho > 0;
    }
}
