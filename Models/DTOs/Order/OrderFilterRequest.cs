namespace PawVerseAPI.Models.DTOs.Order
{
    public class OrderFilterRequest
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Filters
        public string? TrangThai { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? SearchTerm { get; set; } // Search by customer name, phone, order id

        // Sorting
        public string SortBy { get; set; } = "NgayDatHang";
        public string SortOrder { get; set; } = "desc";
    }
}
