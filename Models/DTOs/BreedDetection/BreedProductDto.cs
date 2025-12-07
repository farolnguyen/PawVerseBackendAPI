namespace PawVerseAPI.Models.DTOs.BreedDetection
{
    /// <summary>
    /// Simplified product DTO for breed detection response
    /// </summary>
    public class BreedProductDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string TenDanhMuc { get; set; } = string.Empty;
        public string TenThuongHieu { get; set; } = string.Empty;
        public decimal GiaHienThi { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public string? HinhAnh { get; set; }
        public bool CoKhuyenMai { get; set; }
        public int? PhanTramGiam { get; set; }
    }
}
