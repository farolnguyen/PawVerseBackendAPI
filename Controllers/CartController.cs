using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Cart;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CartDto>.ErrorResponse("Unauthorized"));
                }

                var cart = await GetOrCreateCartAsync(userId);

                var cartDto = await MapCartToDtoAsync(cart);

                return Ok(ApiResponse<CartDto>.SuccessResponse(cartDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy giỏ hàng",
                    new List<string> { ex.Message }));
            }
        }

        [HttpPost("items")]
        public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<CartDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CartDto>.ErrorResponse("Unauthorized"));
                }

                // Check if product exists and is available
                var product = await _context.SanPhams.FindAsync(request.SanPhamId);
                if (product == null)
                {
                    return NotFound(ApiResponse<CartDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                if (product.TrangThai != "Còn hàng")
                {
                    return BadRequest(ApiResponse<CartDto>.ErrorResponse("Sản phẩm không còn hàng"));
                }

                if (product.SoLuongTonKho < request.SoLuong)
                {
                    return BadRequest(ApiResponse<CartDto>.ErrorResponse(
                        $"Số lượng không đủ. Chỉ còn {product.SoLuongTonKho} sản phẩm"));
                }

                var cart = await GetOrCreateCartAsync(userId);

                // Check if product already in cart
                var existingItem = cart.GioHangChiTiets
                    .FirstOrDefault(item => item.SanPhamId == request.SanPhamId);

                if (existingItem != null)
                {
                    // Update quantity
                    var newQuantity = existingItem.SoLuong + request.SoLuong;
                    if (newQuantity > product.SoLuongTonKho)
                    {
                        return BadRequest(ApiResponse<CartDto>.ErrorResponse(
                            $"Số lượng vượt quá tồn kho. Chỉ còn {product.SoLuongTonKho} sản phẩm"));
                    }
                    existingItem.SoLuong = newQuantity;
                }
                else
                {
                    // Add new item
                    var cartItem = new GioHangChiTiet
                    {
                        GioHangId = cart.Id,
                        SanPhamId = request.SanPhamId,
                        SoLuong = request.SoLuong
                    };
                    cart.GioHangChiTiets.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                // Reload cart with all data
                var updatedCart = await GetCartWithDetailsAsync(cart.Id);
                var cartDto = await MapCartToDtoAsync(updatedCart!);

                return Ok(ApiResponse<CartDto>.SuccessResponse(cartDto, "Đã thêm vào giỏ hàng"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng",
                    new List<string> { ex.Message }));
            }
        }

        [HttpPut("items/{id}")]
        public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<CartDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CartDto>.ErrorResponse("Unauthorized"));
                }

                var cart = await GetOrCreateCartAsync(userId);

                var cartItem = cart.GioHangChiTiets.FirstOrDefault(item => item.Id == id);
                if (cartItem == null)
                {
                    return NotFound(ApiResponse<CartDto>.ErrorResponse("Không tìm thấy sản phẩm trong giỏ hàng"));
                }

                // Check stock availability
                var product = await _context.SanPhams.FindAsync(cartItem.SanPhamId);
                if (product == null)
                {
                    return NotFound(ApiResponse<CartDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                if (request.SoLuong > product.SoLuongTonKho)
                {
                    return BadRequest(ApiResponse<CartDto>.ErrorResponse(
                        $"Số lượng vượt quá tồn kho. Chỉ còn {product.SoLuongTonKho} sản phẩm"));
                }

                cartItem.SoLuong = request.SoLuong;
                await _context.SaveChangesAsync();

                // Reload cart with all data
                var updatedCart = await GetCartWithDetailsAsync(cart.Id);
                var cartDto = await MapCartToDtoAsync(updatedCart!);

                return Ok(ApiResponse<CartDto>.SuccessResponse(cartDto, "Đã cập nhật giỏ hàng"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi cập nhật giỏ hàng",
                    new List<string> { ex.Message }));
            }
        }

        [HttpDelete("items/{id}")]
        public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<CartDto>.ErrorResponse("Unauthorized"));
                }

                var cart = await GetOrCreateCartAsync(userId);

                var cartItem = cart.GioHangChiTiets.FirstOrDefault(item => item.Id == id);
                if (cartItem == null)
                {
                    return NotFound(ApiResponse<CartDto>.ErrorResponse("Không tìm thấy sản phẩm trong giỏ hàng"));
                }

                cart.GioHangChiTiets.Remove(cartItem);
                _context.GioHangChiTiets.Remove(cartItem);
                await _context.SaveChangesAsync();

                // Reload cart with all data
                var updatedCart = await GetCartWithDetailsAsync(cart.Id);
                var cartDto = await MapCartToDtoAsync(updatedCart!);

                return Ok(ApiResponse<CartDto>.SuccessResponse(cartDto, "Đã xóa sản phẩm khỏi giỏ hàng"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa sản phẩm khỏi giỏ hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpDelete("clear")]
        public async Task<ActionResult<ApiResponse<object>>> ClearCart()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
                }

                var cart = await _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Giỏ hàng đã trống"));
                }

                _context.GioHangChiTiets.RemoveRange(cart.GioHangChiTiets);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Đã xóa toàn bộ giỏ hàng"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa giỏ hàng",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy số lượng sản phẩm trong giỏ hàng
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<int>>> GetCartCount()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<int>.ErrorResponse("Unauthorized"));
                }

                var cart = await _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var count = cart?.GioHangChiTiets.Sum(item => item.SoLuong) ?? 0;

                return Ok(ApiResponse<int>.SuccessResponse(count));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<int>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy số lượng giỏ hàng",
                    new List<string> { ex.Message }));
            }
        }

        #region Helper Methods

        private async Task<GioHang> GetOrCreateCartAsync(string userId)
        {
            var cart = await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new GioHang
                {
                    UserId = userId
                };
                _context.GioHangs.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        private async Task<GioHang?> GetCartWithDetailsAsync(int cartId)
        {
            return await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                    .ThenInclude(item => item.SanPham)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        private async Task<CartDto> MapCartToDtoAsync(GioHang cart)
        {
            // Ensure products are loaded
            var cartWithProducts = await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                    .ThenInclude(item => item.SanPham)
                .FirstOrDefaultAsync(c => c.Id == cart.Id);

            if (cartWithProducts == null)
            {
                return new CartDto
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    Items = new List<CartItemDto>()
                };
            }

            var items = cartWithProducts.GioHangChiTiets.Select(item => new CartItemDto
            {
                Id = item.Id,
                SanPhamId = item.SanPhamId,
                TenSanPham = item.SanPham?.TenSanPham ?? "",
                HinhAnh = item.SanPham?.HinhAnh,
                GiaBan = item.SanPham?.GiaBan ?? 0,
                GiaKhuyenMai = item.SanPham?.GiaKhuyenMai,
                SoLuong = item.SoLuong,
                SoLuongTonKho = item.SanPham?.SoLuongTonKho ?? 0,
                TrangThai = item.SanPham?.TrangThai ?? ""
            }).ToList();

            return new CartDto
            {
                Id = cartWithProducts.Id,
                UserId = cartWithProducts.UserId,
                Items = items
            };
        }

        #endregion
    }
}
