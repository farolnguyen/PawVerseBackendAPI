namespace PawVerseAPI.Models.DTOs.Admin
{
    public class ProductAdminDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal GiaBan { get; set; }
        public decimal GiaVon { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int SoLuongTonKho { get; set; }
        public int SoLuongDaBan { get; set; }
        public string? HinhAnh { get; set; }
        public int IdDanhMuc { get; set; }
        public string? TenDanhMuc { get; set; }
        public int IdThuongHieu { get; set; }
        public string? TenThuongHieu { get; set; }
        public string TrangThai { get; set; } = "Còn hàng";
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class AdminCreateProductRequest
    {
        public string TenSanPham { get; set; } = string.Empty;
        public string TenAlias { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal GiaBan { get; set; }
        public decimal GiaVon { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int SoLuongTonKho { get; set; }
        public string? HinhAnh { get; set; }
        public int IdDanhMuc { get; set; }
        public int IdThuongHieu { get; set; }
        public string TrongLuong { get; set; } = string.Empty;
        public string? MauSac { get; set; }
        public string? XuatXu { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }
        public string TrangThai { get; set; } = "Còn hàng";
    }

    public class AdminUpdateProductRequest
    {
        public string TenSanPham { get; set; } = string.Empty;
        public string TenAlias { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal GiaBan { get; set; }
        public decimal GiaVon { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int SoLuongTonKho { get; set; }
        public string? HinhAnh { get; set; }
        public int IdDanhMuc { get; set; }
        public int IdThuongHieu { get; set; }
        public string TrongLuong { get; set; } = string.Empty;
        public string? MauSac { get; set; }
        public string? XuatXu { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }
        public string TrangThai { get; set; }
    }

    public class UpdateStockRequest
    {
        public int SoLuongTonKho { get; set; }
    }
}

