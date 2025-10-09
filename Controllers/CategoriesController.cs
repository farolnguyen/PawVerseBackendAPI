using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Category;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả danh mục
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            try
            {
                var categories = await _context.DanhMucs
                    .Select(c => new CategoryDto
                    {
                        IdDanhMuc = c.IdDanhMuc,
                        TenDanhMuc = c.TenDanhMuc,
                        MoTa = c.MoTa,
                        HinhAnh = c.HinhAnh,
                        TrangThai = c.TrangThai,
                        SoLuongSanPham = c.SanPhams.Count
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy danh sách danh mục",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy chi tiết danh mục theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
        {
            try
            {
                var category = await _context.DanhMucs
                    .Where(c => c.IdDanhMuc == id)
                    .Select(c => new CategoryDto
                    {
                        IdDanhMuc = c.IdDanhMuc,
                        TenDanhMuc = c.TenDanhMuc,
                        MoTa = c.MoTa,
                        HinhAnh = c.HinhAnh,
                        TrangThai = c.TrangThai,
                        SoLuongSanPham = c.SanPhams.Count
                    })
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Không tìm thấy danh mục"));
                }

                return Ok(ApiResponse<CategoryDto>.SuccessResponse(category));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy thông tin danh mục",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Tạo danh mục mới (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                // Check if category name already exists
                var exists = await _context.DanhMucs.AnyAsync(c => c.TenDanhMuc == request.TenDanhMuc);
                if (exists)
                {
                    return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Tên danh mục đã tồn tại"));
                }

                var category = new DanhMuc
                {
                    TenDanhMuc = request.TenDanhMuc,
                    MoTa = request.MoTa,
                    HinhAnh = request.HinhAnh,
                    TrangThai = request.TrangThai
                };

                _context.DanhMucs.Add(category);
                await _context.SaveChangesAsync();

                var categoryDto = new CategoryDto
                {
                    IdDanhMuc = category.IdDanhMuc,
                    TenDanhMuc = category.TenDanhMuc,
                    MoTa = category.MoTa,
                    HinhAnh = category.HinhAnh,
                    TrangThai = category.TrangThai,
                    SoLuongSanPham = 0
                };

                return CreatedAtAction(nameof(GetCategory), new { id = category.IdDanhMuc },
                    ApiResponse<CategoryDto>.SuccessResponse(categoryDto, "Tạo danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi tạo danh mục",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Cập nhật danh mục (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var category = await _context.DanhMucs.FindAsync(id);
                if (category == null)
                {
                    return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Không tìm thấy danh mục"));
                }

                // Check if new name already exists (excluding current category)
                var nameExists = await _context.DanhMucs.AnyAsync(c =>
                    c.TenDanhMuc == request.TenDanhMuc && c.IdDanhMuc != id);
                if (nameExists)
                {
                    return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Tên danh mục đã tồn tại"));
                }

                category.TenDanhMuc = request.TenDanhMuc;
                category.MoTa = request.MoTa;
                category.HinhAnh = request.HinhAnh;
                category.TrangThai = request.TrangThai;

                await _context.SaveChangesAsync();

                var productCount = await _context.SanPhams.CountAsync(p => p.IdDanhMuc == id);

                var categoryDto = new CategoryDto
                {
                    IdDanhMuc = category.IdDanhMuc,
                    TenDanhMuc = category.TenDanhMuc,
                    MoTa = category.MoTa,
                    HinhAnh = category.HinhAnh,
                    TrangThai = category.TrangThai,
                    SoLuongSanPham = productCount
                };

                return Ok(ApiResponse<CategoryDto>.SuccessResponse(categoryDto, "Cập nhật danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi cập nhật danh mục",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa danh mục (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.DanhMucs.FindAsync(id);
                if (category == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy danh mục"));
                }

                // Check if category has products
                var hasProducts = await _context.SanPhams.AnyAsync(p => p.IdDanhMuc == id);
                if (hasProducts)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Không thể xóa danh mục đã có sản phẩm. Vui lòng đổi trạng thái thành 'Không hoạt động'"));
                }

                _context.DanhMucs.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Xóa danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa danh mục",
                    new List<string> { ex.Message }));
            }
        }
    }
}
