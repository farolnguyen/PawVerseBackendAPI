using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Helpers;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Order;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tạo đơn hàng từ giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<OrderDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("Unauthorized"));
                }

                // Get cart
                var cart = await _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                        .ThenInclude(item => item.SanPham)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.GioHangChiTiets.Any())
                {
                    return BadRequest(ApiResponse<OrderDto>.ErrorResponse("Giỏ hàng trống"));
                }

                // Validate all products are available
                foreach (var item in cart.GioHangChiTiets)
                {
                    if (item.SanPham == null)
                    {
                        return BadRequest(ApiResponse<OrderDto>.ErrorResponse($"Không tìm thấy sản phẩm"));
                    }

                    if (item.SanPham.TrangThai != "Còn hàng")
                    {
                        return BadRequest(ApiResponse<OrderDto>.ErrorResponse(
                            $"Sản phẩm '{item.SanPham.TenSanPham}' không còn hàng"));
                    }

                    if (item.SanPham.SoLuongTonKho < item.SoLuong)
                    {
                        return BadRequest(ApiResponse<OrderDto>.ErrorResponse(
                            $"Sản phẩm '{item.SanPham.TenSanPham}' không đủ số lượng. Chỉ còn {item.SanPham.SoLuongTonKho}"));
                    }
                }

                // Get shipping cost
                // If no shipping method specified (0), use default (free shipping)
                decimal phiVanChuyen = 0;
                int? idVanChuyen = null;
                
                if (request.IdVanChuyen > 0)
                {
                    var vanChuyen = await _context.VanChuyens.FindAsync(request.IdVanChuyen);
                    if (vanChuyen == null)
                    {
                        return BadRequest(ApiResponse<OrderDto>.ErrorResponse("Phương thức vận chuyển không hợp lệ"));
                    }
                    phiVanChuyen = vanChuyen.PhiVanChuyen;
                    idVanChuyen = request.IdVanChuyen;
                }

                // Calculate total
                decimal tongTienSanPham = cart.GioHangChiTiets.Sum(item =>
                {
                    var gia = item.SanPham!.GiaKhuyenMai ?? item.SanPham.GiaBan;
                    return gia * item.SoLuong;
                });

                decimal giamGia = 0;

                // Apply coupon if provided
                if (request.IdCoupon.HasValue)
                {
                    var coupon = await _context.Coupons.FindAsync(request.IdCoupon.Value);
                    if (coupon != null && coupon.TrangThai == "Hoạt động")
                    {
                        if (coupon.LoaiGiamGia == "Percent")
                        {
                            giamGia = tongTienSanPham * coupon.MucGiamGia / 100;
                        }
                        else
                        {
                            giamGia = coupon.MucGiamGia;
                        }
                    }
                }

                decimal tongTien = tongTienSanPham + phiVanChuyen - giamGia;

                // Create order
                var order = new DonHang
                {
                    IdNguoiDung = userId,
                    TenKhachHang = request.TenKhachHang,
                    SoDienThoai = request.SoDienThoai,
                    DiaChiGiaoHang = request.DiaChiGiaoHang,
                    NgayDatHang = DateTime.Now,
                    NgayGiaoHangDuKien = DateTime.Now.AddDays(3), // Default 3 days
                    TrangThai = "Chờ xác nhận",
                    PhuongThucThanhToan = request.PhuongThucThanhToan,
                    IdVanChuyen = idVanChuyen,
                    PhiVanChuyen = phiVanChuyen,
                    IdCoupon = request.IdCoupon,
                    TongTien = tongTien,
                    GhiChu = request.GhiChu
                };

                // Add order first
                _context.DonHangs.Add(order);
                
                // Create order details and update stock
                foreach (var item in cart.GioHangChiTiets)
                {
                    var donGia = item.SanPham!.GiaKhuyenMai ?? item.SanPham.GiaBan;
                    
                    var orderDetail = new ChiTietDonHang
                    {
                        IdDonHangNavigation = order,
                        SanPham = item.SanPham, // Use navigation property for product too
                        SoLuong = item.SoLuong,
                        DonGia = donGia
                    };

                    _context.ChiTietDonHangs.Add(orderDetail);

                    // Update product stock and sales count
                    item.SanPham.SoLuongTonKho -= item.SoLuong;
                    item.SanPham.SoLuongDaBan += item.SoLuong;
                }

                // Clear cart
                _context.GioHangChiTiets.RemoveRange(cart.GioHangChiTiets);

                // Save all changes in ONE transaction
                await _context.SaveChangesAsync();

                // Return order details
                var orderDto = new OrderDto
                {
                    IdDonHang = order.IdDonHang,
                    IdNguoiDung = order.IdNguoiDung,
                    TenKhachHang = order.TenKhachHang,
                    SoDienThoai = order.SoDienThoai,
                    DiaChiGiaoHang = order.DiaChiGiaoHang,
                    NgayDatHang = order.NgayDatHang,
                    NgayGiaoHangDuKien = order.NgayGiaoHangDuKien,
                    TrangThai = order.TrangThai,
                    PhuongThucThanhToan = order.PhuongThucThanhToan,
                    PhiVanChuyen = order.PhiVanChuyen,
                    TongTien = order.TongTien,
                    GhiChu = order.GhiChu,
                    SoLuongSanPham = await _context.ChiTietDonHangs
                        .Where(ct => ct.IdDonHang == order.IdDonHang)
                        .SumAsync(ct => ct.SoLuong)
                };

                return CreatedAtAction(nameof(GetOrder), new { id = order.IdDonHang },
                    ApiResponse<OrderDto>.SuccessResponse(orderDto, "Đặt hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi tạo đơn hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng của người dùng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetMyOrders([FromQuery] OrderFilterRequest filter)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<PagedResult<OrderDto>>.ErrorResponse("Unauthorized"));
                }

                var query = _context.DonHangs
                    .Where(o => o.IdNguoiDung == userId)
                    .AsQueryable();

                query = ApplyFilters(query, filter);

                var dtoQuery = query.Select(o => new OrderDto
                {
                    IdDonHang = o.IdDonHang,
                    IdNguoiDung = o.IdNguoiDung,
                    TenKhachHang = o.TenKhachHang,
                    SoDienThoai = o.SoDienThoai,
                    DiaChiGiaoHang = o.DiaChiGiaoHang,
                    NgayDatHang = o.NgayDatHang,
                    NgayGiaoHangDuKien = o.NgayGiaoHangDuKien,
                    NgayHuy = o.NgayHuy,
                    TrangThai = o.TrangThai,
                    PhuongThucThanhToan = o.PhuongThucThanhToan,
                    PhiVanChuyen = o.PhiVanChuyen,
                    TongTien = o.TongTien,
                    GhiChu = o.GhiChu,
                    SoLuongSanPham = o.ChiTietDonHangs.Sum(ct => ct.SoLuong)
                });

                var pagedResult = await PaginationHelper.CreateAsync(dtoQuery, filter.PageNumber, filter.PageSize);

                return Ok(ApiResponse<PagedResult<OrderDto>>.SuccessResponse(pagedResult));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<OrderDto>>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy danh sách đơn hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("Unauthorized"));
                }

                var isAdmin = User.IsInRole("Admin");

                var order = await _context.DonHangs
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(ct => ct.SanPham)
                    .FirstOrDefaultAsync(o => o.IdDonHang == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.ErrorResponse("Không tìm thấy đơn hàng"));
                }

                // Check ownership
                if (!isAdmin && order.IdNguoiDung != userId)
                {
                    return Forbid();
                }

                var orderDto = new OrderDto
                {
                    IdDonHang = order.IdDonHang,
                    IdNguoiDung = order.IdNguoiDung,
                    TenKhachHang = order.TenKhachHang,
                    SoDienThoai = order.SoDienThoai,
                    DiaChiGiaoHang = order.DiaChiGiaoHang,
                    NgayDatHang = order.NgayDatHang,
                    NgayGiaoHangDuKien = order.NgayGiaoHangDuKien,
                    NgayHuy = order.NgayHuy,
                    TrangThai = order.TrangThai,
                    PhuongThucThanhToan = order.PhuongThucThanhToan,
                    PhiVanChuyen = order.PhiVanChuyen,
                    TongTien = order.TongTien,
                    GhiChu = order.GhiChu,
                    SoLuongSanPham = order.ChiTietDonHangs.Sum(ct => ct.SoLuong),
                    Items = order.ChiTietDonHangs.Select(ct => new OrderItemDto
                    {
                        IdChiTietDonHang = ct.IdChiTietDonHang,
                        IdSanPham = ct.IdSanPham,
                        TenSanPham = ct.SanPham?.TenSanPham ?? "",
                        HinhAnh = ct.SanPham?.HinhAnh,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia
                    }).ToList()
                };

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy thông tin đơn hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Hủy đơn hàng (chỉ khi trạng thái Chờ xác nhận)
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CancelOrder(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("Unauthorized"));
                }

                var order = await _context.DonHangs
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(ct => ct.SanPham)
                    .FirstOrDefaultAsync(o => o.IdDonHang == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.ErrorResponse("Không tìm thấy đơn hàng"));
                }

                // Check ownership
                if (order.IdNguoiDung != userId)
                {
                    return Forbid();
                }

                // Only allow cancellation for pending orders
                if (order.TrangThai != "Chờ xác nhận")
                {
                    return BadRequest(ApiResponse<OrderDto>.ErrorResponse(
                        "Chỉ có thể hủy đơn hàng đang chờ xác nhận"));
                }

                // Restore stock
                foreach (var detail in order.ChiTietDonHangs)
                {
                    if (detail.SanPham != null)
                    {
                        detail.SanPham.SoLuongTonKho += detail.SoLuong;
                        detail.SanPham.SoLuongDaBan -= detail.SoLuong;
                    }
                }

                order.TrangThai = "Đã hủy";
                order.NgayHuy = DateTime.Now;

                await _context.SaveChangesAsync();

                var orderDto = new OrderDto
                {
                    IdDonHang = order.IdDonHang,
                    IdNguoiDung = order.IdNguoiDung,
                    TenKhachHang = order.TenKhachHang,
                    SoDienThoai = order.SoDienThoai,
                    DiaChiGiaoHang = order.DiaChiGiaoHang,
                    NgayDatHang = order.NgayDatHang,
                    NgayGiaoHangDuKien = order.NgayGiaoHangDuKien,
                    NgayHuy = order.NgayHuy,
                    TrangThai = order.TrangThai,
                    PhuongThucThanhToan = order.PhuongThucThanhToan,
                    PhiVanChuyen = order.PhiVanChuyen,
                    TongTien = order.TongTien,
                    GhiChu = order.GhiChu,
                    SoLuongSanPham = order.ChiTietDonHangs.Sum(ct => ct.SoLuong)
                };

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto, "Đã hủy đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi hủy đơn hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy tất cả đơn hàng (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetAllOrders([FromQuery] OrderFilterRequest filter)
        {
            try
            {
                var query = _context.DonHangs.AsQueryable();

                query = ApplyFilters(query, filter);

                var dtoQuery = query.Select(o => new OrderDto
                {
                    IdDonHang = o.IdDonHang,
                    IdNguoiDung = o.IdNguoiDung,
                    TenKhachHang = o.TenKhachHang,
                    SoDienThoai = o.SoDienThoai,
                    DiaChiGiaoHang = o.DiaChiGiaoHang,
                    NgayDatHang = o.NgayDatHang,
                    NgayGiaoHangDuKien = o.NgayGiaoHangDuKien,
                    NgayHuy = o.NgayHuy,
                    TrangThai = o.TrangThai,
                    PhuongThucThanhToan = o.PhuongThucThanhToan,
                    PhiVanChuyen = o.PhiVanChuyen,
                    TongTien = o.TongTien,
                    GhiChu = o.GhiChu,
                    SoLuongSanPham = o.ChiTietDonHangs.Sum(ct => ct.SoLuong)
                });

                var pagedResult = await PaginationHelper.CreateAsync(dtoQuery, filter.PageNumber, filter.PageSize);

                return Ok(ApiResponse<PagedResult<OrderDto>>.SuccessResponse(pagedResult));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<OrderDto>>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy danh sách đơn hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<OrderDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var order = await _context.DonHangs.FindAsync(id);
                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.ErrorResponse("Không tìm thấy đơn hàng"));
                }

                order.TrangThai = request.TrangThai;
                
                if (request.NgayGiaoHangDuKien.HasValue)
                {
                    order.NgayGiaoHangDuKien = request.NgayGiaoHangDuKien.Value;
                }

                if (request.TrangThai == "Đã hủy" && order.NgayHuy == null)
                {
                    order.NgayHuy = DateTime.Now;
                    
                    // Restore stock if cancelled by admin
                    var orderDetails = await _context.ChiTietDonHangs
                        .Include(ct => ct.SanPham)
                        .Where(ct => ct.IdDonHang == id)
                        .ToListAsync();

                    foreach (var detail in orderDetails)
                    {
                        if (detail.SanPham != null)
                        {
                            detail.SanPham.SoLuongTonKho += detail.SoLuong;
                            detail.SanPham.SoLuongDaBan -= detail.SoLuong;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var soLuongSanPham = await _context.ChiTietDonHangs
                    .Where(ct => ct.IdDonHang == id)
                    .SumAsync(ct => ct.SoLuong);

                var orderDto = new OrderDto
                {
                    IdDonHang = order.IdDonHang,
                    IdNguoiDung = order.IdNguoiDung,
                    TenKhachHang = order.TenKhachHang,
                    SoDienThoai = order.SoDienThoai,
                    DiaChiGiaoHang = order.DiaChiGiaoHang,
                    NgayDatHang = order.NgayDatHang,
                    NgayGiaoHangDuKien = order.NgayGiaoHangDuKien,
                    NgayHuy = order.NgayHuy,
                    TrangThai = order.TrangThai,
                    PhuongThucThanhToan = order.PhuongThucThanhToan,
                    PhiVanChuyen = order.PhiVanChuyen,
                    TongTien = order.TongTien,
                    GhiChu = order.GhiChu,
                    SoLuongSanPham = soLuongSanPham
                };

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto, "Cập nhật trạng thái thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi cập nhật trạng thái",
                    new List<string> { ex.Message }));
            }
        }

        #region Helper Methods

        private IQueryable<DonHang> ApplyFilters(IQueryable<DonHang> query, OrderFilterRequest filter)
        {
            // Filter by status
            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            {
                query = query.Where(o => o.TrangThai == filter.TrangThai);
            }

            // Filter by date range
            if (filter.TuNgay.HasValue)
            {
                query = query.Where(o => o.NgayDatHang >= filter.TuNgay.Value);
            }

            if (filter.DenNgay.HasValue)
            {
                query = query.Where(o => o.NgayDatHang <= filter.DenNgay.Value);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLower();
                query = query.Where(o =>
                    o.TenKhachHang.ToLower().Contains(searchLower) ||
                    o.SoDienThoai.Contains(filter.SearchTerm) ||
                    o.IdDonHang.ToString().Contains(filter.SearchTerm));
            }

            // Sorting
            query = filter.SortBy.ToLower() switch
            {
                "tongtien" => filter.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.TongTien)
                    : query.OrderByDescending(o => o.TongTien),
                "trangthai" => filter.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.TrangThai)
                    : query.OrderByDescending(o => o.TrangThai),
                _ => filter.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.NgayDatHang)
                    : query.OrderByDescending(o => o.NgayDatHang)
            };

            return query;
        }

        #endregion
    }
}
