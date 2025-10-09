using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Wishlist;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách yêu thích của người dùng
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WishlistItemDto>>>> GetWishlist()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<List<WishlistItemDto>>.ErrorResponse("Unauthorized"));
                }

                var wishlist = await _context.DanhSachYeuThiches
                    .Include(w => w.IdSanPhamNavigation)
                    .Where(w => w.IdNguoiDung == userId)
                    .OrderByDescending(w => w.NgayThem)
                    .Select(w => new WishlistItemDto
                    {
                        IdYeuThich = w.IdYeuThich,
                        IdSanPham = w.IdSanPham,
                        TenSanPham = w.IdSanPhamNavigation != null ? w.IdSanPhamNavigation.TenSanPham : "",
                        HinhAnh = w.IdSanPhamNavigation != null ? w.IdSanPhamNavigation.HinhAnh : null,
                        GiaBan = w.IdSanPhamNavigation != null ? w.IdSanPhamNavigation.GiaBan : 0,
                        GiaKhuyenMai = w.IdSanPhamNavigation != null ? w.IdSanPhamNavigation.GiaKhuyenMai : null,
                        TrangThai = w.IdSanPhamNavigation != null ? w.IdSanPhamNavigation.TrangThai : "",
                        SoLuongTonKho = w.IdSanPhamNavigation != null ? w.IdSanPhamNavigation.SoLuongTonKho : 0,
                        NgayThem = w.NgayThem
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<WishlistItemDto>>.SuccessResponse(wishlist));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<WishlistItemDto>>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy danh sách yêu thích",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Thêm sản phẩm vào danh sách yêu thích
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<WishlistItemDto>>> AddToWishlist([FromBody] AddToWishlistRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<WishlistItemDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<WishlistItemDto>.ErrorResponse("Unauthorized"));
                }

                // Check if product exists
                var product = await _context.SanPhams.FindAsync(request.IdSanPham);
                if (product == null)
                {
                    return NotFound(ApiResponse<WishlistItemDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // Check if already in wishlist
                var existingItem = await _context.DanhSachYeuThiches
                    .FirstOrDefaultAsync(w => w.IdNguoiDung == userId && w.IdSanPham == request.IdSanPham);

                if (existingItem != null)
                {
                    return BadRequest(ApiResponse<WishlistItemDto>.ErrorResponse("Sản phẩm đã có trong danh sách yêu thích"));
                }

                // Add to wishlist
                var wishlistItem = new DanhSachYeuThich
                {
                    IdNguoiDung = userId,
                    IdSanPham = request.IdSanPham,
                    NgayThem = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };

                _context.DanhSachYeuThiches.Add(wishlistItem);
                await _context.SaveChangesAsync();

                // Return the added item
                var itemDto = new WishlistItemDto
                {
                    IdYeuThich = wishlistItem.IdYeuThich,
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    HinhAnh = product.HinhAnh,
                    GiaBan = product.GiaBan,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    TrangThai = product.TrangThai,
                    SoLuongTonKho = product.SoLuongTonKho,
                    NgayThem = wishlistItem.NgayThem
                };

                return Ok(ApiResponse<WishlistItemDto>.SuccessResponse(itemDto, "Đã thêm vào danh sách yêu thích"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<WishlistItemDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi thêm sản phẩm vào danh sách yêu thích",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa sản phẩm khỏi danh sách yêu thích
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveFromWishlist(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
                }

                var wishlistItem = await _context.DanhSachYeuThiches
                    .FirstOrDefaultAsync(w => w.IdYeuThich == id && w.IdNguoiDung == userId);

                if (wishlistItem == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm trong danh sách yêu thích"));
                }

                _context.DanhSachYeuThiches.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Đã xóa khỏi danh sách yêu thích"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa sản phẩm khỏi danh sách yêu thích",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa sản phẩm khỏi wishlist bằng productId (alternative method)
        /// </summary>
        [HttpDelete("product/{productId}")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveByProductId(int productId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
                }

                var wishlistItem = await _context.DanhSachYeuThiches
                    .FirstOrDefaultAsync(w => w.IdSanPham == productId && w.IdNguoiDung == userId);

                if (wishlistItem == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm trong danh sách yêu thích"));
                }

                _context.DanhSachYeuThiches.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Đã xóa khỏi danh sách yêu thích"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa sản phẩm khỏi danh sách yêu thích",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Kiểm tra sản phẩm có trong danh sách yêu thích không
        /// </summary>
        [HttpGet("check/{productId}")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckInWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Unauthorized"));
                }

                var exists = await _context.DanhSachYeuThiches
                    .AnyAsync(w => w.IdNguoiDung == userId && w.IdSanPham == productId);

                return Ok(ApiResponse<bool>.SuccessResponse(exists));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    "Đã xảy ra lỗi khi kiểm tra sản phẩm",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy số lượng sản phẩm trong wishlist
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<int>>> GetWishlistCount()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<int>.ErrorResponse("Unauthorized"));
                }

                var count = await _context.DanhSachYeuThiches
                    .CountAsync(w => w.IdNguoiDung == userId);

                return Ok(ApiResponse<int>.SuccessResponse(count));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<int>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy số lượng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa toàn bộ wishlist
        /// </summary>
        [HttpDelete("clear")]
        public async Task<ActionResult<ApiResponse<object>>> ClearWishlist()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
                }

                var wishlistItems = await _context.DanhSachYeuThiches
                    .Where(w => w.IdNguoiDung == userId)
                    .ToListAsync();

                if (wishlistItems.Any())
                {
                    _context.DanhSachYeuThiches.RemoveRange(wishlistItems);
                    await _context.SaveChangesAsync();
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "Đã xóa toàn bộ danh sách yêu thích"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa danh sách yêu thích",
                    new List<string> { ex.Message }));
            }
        }
    }
}
