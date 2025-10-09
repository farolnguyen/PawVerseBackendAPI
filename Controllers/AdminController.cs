using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Admin;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Get all users with pagination and filters
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserAdminDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(u =>
                        u.Email.ToLower().Contains(search) ||
                        u.FullName.ToLower().Contains(search) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
                    );
                }

                // Get total count before role filter
                var totalCount = await query.CountAsync();

                // Get paginated users
                var users = await query
                    .OrderByDescending(u => u.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserAdminDto
                    {
                        Id = u.Id,
                        Email = u.Email ?? string.Empty,
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        Avatar = u.Avatar,
                        NgayTao = u.NgayTao,
                        LockoutEnabled = u.LockoutEnabled,
                        LockoutEnd = u.LockoutEnd
                    })
                    .ToListAsync();

                // Get roles for each user
                foreach (var user in users)
                {
                    var appUser = await _userManager.FindByIdAsync(user.Id);
                    if (appUser != null)
                    {
                        user.Roles = (await _userManager.GetRolesAsync(appUser)).ToList();
                    }
                }

                // Apply role filter after getting roles
                if (!string.IsNullOrWhiteSpace(role))
                {
                    users = users.Where(u => u.Roles.Contains(role)).ToList();
                    totalCount = users.Count;
                }

                var result = new PagedResult<UserAdminDto>
                {
                    Items = users,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(ApiResponse<PagedResult<UserAdminDto>>.SuccessResponse(result, "Lấy danh sách users thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<UserAdminDto>>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<ActionResult<ApiResponse<UserAdminDto>>> GetUserById(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserAdminDto>.ErrorResponse("Không tìm thấy user"));
                }

                var userDto = new UserAdminDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Avatar = user.Avatar,
                    NgayTao = user.NgayTao,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                };

                return Ok(ApiResponse<UserAdminDto>.SuccessResponse(userDto, "Lấy thông tin user thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UserAdminDto>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Lock user account
        /// </summary>
        [HttpPut("users/{id}/lock")]
        public async Task<ActionResult<ApiResponse<object>>> LockUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy user"));
                }

                // Prevent locking own account
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == id)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Không thể khóa tài khoản của chính mình"));
                }

                // Lock account
                var result = await _userManager.SetLockoutEnabledAsync(user, true);
                if (result.Succeeded)
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Đã khóa tài khoản user"));
                }

                return BadRequest(ApiResponse<object>.ErrorResponse("Không thể khóa tài khoản"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Unlock user account
        /// </summary>
        [HttpPut("users/{id}/unlock")]
        public async Task<ActionResult<ApiResponse<object>>> UnlockUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy user"));
                }

                // Unlock account
                var result = await _userManager.SetLockoutEnabledAsync(user, false);
                if (result.Succeeded)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Đã mở khóa tài khoản user"));
                }

                return BadRequest(ApiResponse<object>.ErrorResponse("Không thể mở khóa tài khoản"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy user"));
                }

                // Prevent deleting own account
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == id)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Không thể xóa tài khoản của chính mình"));
                }

                // Delete user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Đã xóa user thành công"));
                }

                return BadRequest(ApiResponse<object>.ErrorResponse("Không thể xóa user"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update user roles
        /// </summary>
        [HttpPut("users/{id}/roles")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateUserRoles(
            string id,
            [FromBody] UpdateUserRolesRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy user"));
                }

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove all current roles
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Không thể cập nhật roles"));
                }

                // Add new roles
                var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (addResult.Succeeded)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Đã cập nhật roles thành công"));
                }

                return BadRequest(ApiResponse<object>.ErrorResponse("Không thể cập nhật roles"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        [HttpGet("dashboard/stats")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var totalProducts = await _context.SanPhams.CountAsync();
                
                // TODO: Get orders and revenue from actual data
                var stats = new DashboardStatsDto
                {
                    TotalUsers = totalUsers,
                    TotalProducts = totalProducts,
                    TotalOrders = 0,   // TODO: Implement
                    TotalRevenue = 0   // TODO: Implement
                };

                return Ok(ApiResponse<DashboardStatsDto>.SuccessResponse(stats, "Lấy thống kê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        // ==================== PRODUCT MANAGEMENT ====================

        /// <summary>
        /// Get all products with pagination and filters
        /// </summary>
        [HttpGet("products")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductAdminDto>>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? idDanhMuc = null,
            [FromQuery] int? idThuongHieu = null)
        {
            try
            {
                var query = _context.SanPhams
                    .Include(p => p.IdDanhMucNavigation)
                    .Include(p => p.IdThuongHieuNavigation)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(p => p.TenSanPham.ToLower().Contains(search));
                }

                // Apply category filter
                if (idDanhMuc.HasValue)
                {
                    query = query.Where(p => p.IdDanhMuc == idDanhMuc.Value);
                }

                // Apply brand filter
                if (idThuongHieu.HasValue)
                {
                    query = query.Where(p => p.IdThuongHieu == idThuongHieu.Value);
                }

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderByDescending(p => p.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductAdminDto
                    {
                        IdSanPham = p.IdSanPham,
                        TenSanPham = p.TenSanPham,
                        MoTa = p.MoTa,
                        GiaBan = p.GiaBan,
                        GiaVon = p.GiaVon,
                        GiaKhuyenMai = p.GiaKhuyenMai,
                        SoLuongTonKho = p.SoLuongTonKho,
                        SoLuongDaBan = p.SoLuongDaBan,
                        HinhAnh = p.HinhAnh,
                        IdDanhMuc = p.IdDanhMuc,
                        TenDanhMuc = p.IdDanhMucNavigation != null ? p.IdDanhMucNavigation.TenDanhMuc : null,
                        IdThuongHieu = p.IdThuongHieu,
                        TenThuongHieu = p.IdThuongHieuNavigation != null ? p.IdThuongHieuNavigation.TenThuongHieu : null,
                        TrangThai = p.TrangThai,
                        NgayTao = p.NgayTao,
                        NgayCapNhat = p.NgayCapNhat
                    })
                    .ToListAsync();

                var result = new PagedResult<ProductAdminDto>
                {
                    Items = products,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(ApiResponse<PagedResult<ProductAdminDto>>.SuccessResponse(result, "Lấy danh sách sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<ProductAdminDto>>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("products/{id}")]
        public async Task<ActionResult<ApiResponse<ProductAdminDto>>> GetProductById(int id)
        {
            try
            {
                var product = await _context.SanPhams
                    .Include(p => p.IdDanhMucNavigation)
                    .Include(p => p.IdThuongHieuNavigation)
                    .FirstOrDefaultAsync(p => p.IdSanPham == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<ProductAdminDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                var productDto = new ProductAdminDto
                {
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    MoTa = product.MoTa,
                    GiaBan = product.GiaBan,
                    GiaVon = product.GiaVon,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    SoLuongTonKho = product.SoLuongTonKho,
                    SoLuongDaBan = product.SoLuongDaBan,
                    HinhAnh = product.HinhAnh,
                    IdDanhMuc = product.IdDanhMuc,
                    TenDanhMuc = product.IdDanhMucNavigation?.TenDanhMuc,
                    IdThuongHieu = product.IdThuongHieu,
                    TenThuongHieu = product.IdThuongHieuNavigation?.TenThuongHieu,
                    TrangThai = product.TrangThai,
                    NgayTao = product.NgayTao,
                    NgayCapNhat = product.NgayCapNhat
                };

                return Ok(ApiResponse<ProductAdminDto>.SuccessResponse(productDto, "Lấy thông tin sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductAdminDto>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Create new product
        /// </summary>
        [HttpPost("products")]
        public async Task<ActionResult<ApiResponse<ProductAdminDto>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var product = new SanPham
                {
                    TenSanPham = request.TenSanPham,
                    TenAlias = request.TenAlias,
                    MoTa = request.MoTa,
                    GiaBan = request.GiaBan,
                    GiaVon = request.GiaVon,
                    GiaKhuyenMai = request.GiaKhuyenMai,
                    SoLuongTonKho = request.SoLuongTonKho,
                    HinhAnh = request.HinhAnh,
                    IdDanhMuc = request.IdDanhMuc,
                    IdThuongHieu = request.IdThuongHieu,
                    TrongLuong = request.TrongLuong,
                    MauSac = request.MauSac,
                    XuatXu = request.XuatXu,
                    NgaySanXuat = request.NgaySanXuat,
                    HanSuDung = request.HanSuDung,
                    TrangThai = request.TrangThai,
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now,
                    SoLuongDaBan = 0,
                    SoLanXem = 0
                };

                _context.SanPhams.Add(product);
                await _context.SaveChangesAsync();

                var productDto = new ProductAdminDto
                {
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    MoTa = product.MoTa,
                    GiaBan = product.GiaBan,
                    GiaVon = product.GiaVon,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    SoLuongTonKho = product.SoLuongTonKho,
                    SoLuongDaBan = product.SoLuongDaBan,
                    HinhAnh = product.HinhAnh,
                    IdDanhMuc = product.IdDanhMuc,
                    IdThuongHieu = product.IdThuongHieu,
                    TrangThai = product.TrangThai,
                    NgayTao = product.NgayTao,
                    NgayCapNhat = product.NgayCapNhat
                };

                return Ok(ApiResponse<ProductAdminDto>.SuccessResponse(productDto, "Tạo sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductAdminDto>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update product
        /// </summary>
        [HttpPut("products/{id}")]
        public async Task<ActionResult<ApiResponse<ProductAdminDto>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<ProductAdminDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                product.TenSanPham = request.TenSanPham;
                product.TenAlias = request.TenAlias;
                product.MoTa = request.MoTa;
                product.GiaBan = request.GiaBan;
                product.GiaVon = request.GiaVon;
                product.GiaKhuyenMai = request.GiaKhuyenMai;
                product.SoLuongTonKho = request.SoLuongTonKho;
                product.HinhAnh = request.HinhAnh;
                product.IdDanhMuc = request.IdDanhMuc;
                product.IdThuongHieu = request.IdThuongHieu;
                product.TrongLuong = request.TrongLuong;
                product.MauSac = request.MauSac;
                product.XuatXu = request.XuatXu;
                product.NgaySanXuat = request.NgaySanXuat;
                product.HanSuDung = request.HanSuDung;
                product.TrangThai = request.TrangThai;
                product.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();

                var productDto = new ProductAdminDto
                {
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    MoTa = product.MoTa,
                    GiaBan = product.GiaBan,
                    GiaVon = product.GiaVon,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    SoLuongTonKho = product.SoLuongTonKho,
                    SoLuongDaBan = product.SoLuongDaBan,
                    HinhAnh = product.HinhAnh,
                    IdDanhMuc = product.IdDanhMuc,
                    IdThuongHieu = product.IdThuongHieu,
                    TrangThai = product.TrangThai,
                    NgayTao = product.NgayTao,
                    NgayCapNhat = product.NgayCapNhat
                };

                return Ok(ApiResponse<ProductAdminDto>.SuccessResponse(productDto, "Cập nhật sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductAdminDto>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete product
        /// </summary>
        [HttpDelete("products/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                _context.SanPhams.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update product stock
        /// </summary>
        [HttpPut("products/{id}/stock")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStock(int id, [FromBody] UpdateStockRequest request)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                product.SoLuongTonKho = request.SoLuongTonKho;
                product.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Cập nhật số lượng tồn kho thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Toggle product visibility
        /// </summary>
        [HttpPut("products/{id}/toggle-visibility")]
        public async Task<ActionResult<ApiResponse<object>>> ToggleVisibility(int id)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                product.TrangThai = product.TrangThai == "Còn hàng" ? "Hết hàng" : "Còn hàng";
                product.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, $"Trạng thái sản phẩm: {product.TrangThai}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        // ==================== ORDER MANAGEMENT ====================
        // See separate file for Order Management endpoints
    }
}
