using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Admin;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all orders with pagination and filters
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderAdminDto>>>> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(ct => ct.SanPham)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(o =>
                        o.TenKhachHang.ToLower().Contains(search) ||
                        o.SoDienThoai.Contains(search));
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(o => o.TrangThai == status);
                }

                var totalCount = await query.CountAsync();

                var orders = await query
                    .OrderByDescending(o => o.NgayDatHang)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderAdminDto
                    {
                        IdDonHang = o.IdDonHang,
                        IdNguoiDung = o.IdNguoiDung,
                        TenNguoiDung = o.TenKhachHang,
                        Email = o.NguoiDung != null ? o.NguoiDung.Email : null,
                        TongTien = o.TongTien,
                        TrangThaiDonHang = o.TrangThai,
                        PhuongThucThanhToan = o.PhuongThucThanhToan,
                        TrangThaiThanhToan = o.TrangThai,
                        DiaChiGiaoHang = o.DiaChiGiaoHang,
                        NgayDatHang = o.NgayDatHang,
                        NgayGiaoHang = o.NgayGiaoHangDuKien,
                        ChiTietDonHang = o.ChiTietDonHangs.Select(ct => new OrderItemDto
                        {
                            IdSanPham = ct.IdSanPham,
                            TenSanPham = ct.SanPham != null ? ct.SanPham.TenSanPham : "",
                            SoLuong = ct.SoLuong,
                            DonGia = ct.DonGia,
                            ThanhTien = ct.SoLuong * ct.DonGia
                        }).ToList()
                    })
                    .ToListAsync();

                var result = new PagedResult<OrderAdminDto>
                {
                    Items = orders,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(ApiResponse<PagedResult<OrderAdminDto>>.SuccessResponse(result, "Lấy danh sách đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<OrderAdminDto>>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderAdminDto>>> GetOrderById(int id)
        {
            try
            {
                var order = await _context.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(ct => ct.SanPham)
                    .FirstOrDefaultAsync(o => o.IdDonHang == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderAdminDto>.ErrorResponse("Không tìm thấy đơn hàng"));
                }

                var orderDto = new OrderAdminDto
                {
                    IdDonHang = order.IdDonHang,
                    IdNguoiDung = order.IdNguoiDung,
                    TenNguoiDung = order.TenKhachHang,
                    Email = order.NguoiDung?.Email,
                    TongTien = order.TongTien,
                    TrangThaiDonHang = order.TrangThai,
                    PhuongThucThanhToan = order.PhuongThucThanhToan,
                    TrangThaiThanhToan = order.TrangThai,
                    DiaChiGiaoHang = order.DiaChiGiaoHang,
                    NgayDatHang = order.NgayDatHang,
                    NgayGiaoHang = order.NgayGiaoHangDuKien,
                    ChiTietDonHang = order.ChiTietDonHangs.Select(ct => new OrderItemDto
                    {
                        IdSanPham = ct.IdSanPham,
                        TenSanPham = ct.SanPham?.TenSanPham ?? "",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        ThanhTien = ct.SoLuong * ct.DonGia
                    }).ToList()
                };

                return Ok(ApiResponse<OrderAdminDto>.SuccessResponse(orderDto, "Lấy thông tin đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<OrderAdminDto>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _context.DonHangs.FindAsync(id);
                if (order == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy đơn hàng"));
                }

                order.TrangThai = request.TrangThaiDonHang;

                // Update delivery date if status is completed
                if (request.TrangThaiDonHang == "Đã giao")
                {
                    order.NgayGiaoHangDuKien = DateTime.Now;
                }
                else if (request.TrangThaiDonHang == "Đã hủy")
                {
                    order.NgayHuy = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Cập nhật trạng thái đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.DonHangs
                    .Include(o => o.ChiTietDonHangs)
                    .FirstOrDefaultAsync(o => o.IdDonHang == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy đơn hàng"));
                }

                _context.DonHangs.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Xóa đơn hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }
    }
}
