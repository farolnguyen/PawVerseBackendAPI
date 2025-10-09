namespace PawVerseAPI.Models.DTOs.Brand
{
    public class BrandDto
    {
        public int IdThuongHieu { get; set; }
        public string TenThuongHieu { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string? Logo { get; set; }
        public string? TrangThai { get; set; }
        public int SoLuongSanPham { get; set; }
    }
}
