namespace PawVerseAPI.Models.DTOs.Wishlist
{
    public class WishlistItemDto
    {
        public int IdYeuThich { get; set; }
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public decimal GiaBan { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public int SoLuongTonKho { get; set; }
        public DateTime NgayThem { get; set; }
        
        // Computed properties
        public decimal GiaHienThi => GiaKhuyenMai ?? GiaBan;
        public bool CoKhuyenMai => GiaKhuyenMai.HasValue && GiaKhuyenMai < GiaBan;
        public bool ConHang => SoLuongTonKho > 0 && TrangThai == "Còn hàng";
    }
}
