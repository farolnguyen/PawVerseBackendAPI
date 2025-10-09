namespace PawVerseAPI.Models.DTOs.Category
{
    public class CategoryDto
    {
        public int IdDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public string? TrangThai { get; set; }
        public int SoLuongSanPham { get; set; }
    }
}
