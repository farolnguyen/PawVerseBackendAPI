namespace PawVerseAPI.Models.DTOs.Order
{
    public class OrderDto
    {
        public int IdDonHang { get; set; }
        public string IdNguoiDung { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChiGiaoHang { get; set; } = string.Empty;
        public DateTime NgayDatHang { get; set; }
        public DateTime? NgayGiaoHangDuKien { get; set; }
        public DateTime? NgayHuy { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public decimal PhiVanChuyen { get; set; }
        public decimal TongTien { get; set; }
        public string? GhiChu { get; set; }
        public int SoLuongSanPham { get; set; }
        
        // For detail view
        public List<OrderItemDto>? Items { get; set; }
    }
}
