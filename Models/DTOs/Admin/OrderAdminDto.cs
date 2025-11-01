namespace PawVerseAPI.Models.DTOs.Admin
{
    public class OrderAdminDto
    {
        public int IdDonHang { get; set; }
        public string IdNguoiDung { get; set; } = string.Empty;
        public string? TenNguoiDung { get; set; }
        public string? Email { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThaiDonHang { get; set; } = string.Empty;
        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public string TrangThaiThanhToan { get; set; } = string.Empty;
        public string? DiaChiGiaoHang { get; set; }
        public DateTime NgayDatHang { get; set; }
        public DateTime? NgayGiaoHang { get; set; }
        public List<AdminOrderItemDto> ChiTietDonHang { get; set; } = new List<AdminOrderItemDto>();
    }

    public class AdminOrderItemDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class AdminUpdateOrderStatusRequest
    {
        public string TrangThaiDonHang { get; set; } = string.Empty;
    }

    public class AdminUpdatePaymentStatusRequest
    {
        public string TrangThaiThanhToan { get; set; } = string.Empty;
    }
}
