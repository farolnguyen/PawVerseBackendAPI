namespace PawVerseAPI.Models.DTOs.Product
{
    public class ProductDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string TenAlias { get; set; } = string.Empty;
        public int IdDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public int IdThuongHieu { get; set; }
        public string TenThuongHieu { get; set; } = string.Empty;
        public string TrongLuong { get; set; } = string.Empty;
        public string? MauSac { get; set; }
        public string? XuatXu { get; set; }
        public string? MoTa { get; set; }
        public int SoLuongTonKho { get; set; }
        public int SoLuongDaBan { get; set; }
        public decimal GiaBan { get; set; }
        public decimal GiaVon { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public string? HinhAnh { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public int SoLanXem { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        // Computed properties
        public decimal GiaHienThi => GiaKhuyenMai ?? GiaBan;
        public bool CoKhuyenMai => GiaKhuyenMai.HasValue && GiaKhuyenMai < GiaBan;
        public decimal? PhanTramGiam => CoKhuyenMai && GiaKhuyenMai.HasValue 
            ? Math.Round((GiaBan - GiaKhuyenMai.Value) / GiaBan * 100, 0) 
            : null;
    }
}
