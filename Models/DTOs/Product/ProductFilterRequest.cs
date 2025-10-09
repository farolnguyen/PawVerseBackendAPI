namespace PawVerseAPI.Models.DTOs.Product
{
    public class ProductFilterRequest
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Search
        public string? SearchTerm { get; set; }

        // Filters
        public int? IdDanhMuc { get; set; }
        public int? IdThuongHieu { get; set; }
        public string? TrangThai { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }

        // Sorting
        public string SortBy { get; set; } = "NgayTao"; // NgayTao, TenSanPham, GiaBan, SoLuongDaBan
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // Filters for special categories
        public bool? CoKhuyenMai { get; set; }
        public bool? SanPhamMoi { get; set; } // Created in last 30 days
        public bool? SanPhamBanChay { get; set; } // SoLuongDaBan > threshold
    }
}
