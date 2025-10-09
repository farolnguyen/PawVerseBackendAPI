using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Brand;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BrandsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả thương hiệu
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetBrands()
        {
            try
            {
                var brands = await _context.ThuongHieus
                    .Select(b => new BrandDto
                    {
                        IdThuongHieu = b.IdThuongHieu,
                        TenThuongHieu = b.TenThuongHieu,
                        MoTa = b.MoTa,
                        Logo = b.Logo,
                        TrangThai = b.TrangThai,
                        SoLuongSanPham = b.SanPhams.Count
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<BrandDto>>.SuccessResponse(brands));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<BrandDto>>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy danh sách thương hiệu",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy chi tiết thương hiệu theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BrandDto>>> GetBrand(int id)
        {
            try
            {
                var brand = await _context.ThuongHieus
                    .Where(b => b.IdThuongHieu == id)
                    .Select(b => new BrandDto
                    {
                        IdThuongHieu = b.IdThuongHieu,
                        TenThuongHieu = b.TenThuongHieu,
                        MoTa = b.MoTa,
                        Logo = b.Logo,
                        TrangThai = b.TrangThai,
                        SoLuongSanPham = b.SanPhams.Count
                    })
                    .FirstOrDefaultAsync();

                if (brand == null)
                {
                    return NotFound(ApiResponse<BrandDto>.ErrorResponse("Không tìm thấy thương hiệu"));
                }

                return Ok(ApiResponse<BrandDto>.SuccessResponse(brand));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy thông tin thương hiệu",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Tạo thương hiệu mới (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<BrandDto>>> CreateBrand([FromBody] CreateBrandRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<BrandDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                // Check if brand name already exists
                var exists = await _context.ThuongHieus.AnyAsync(b => b.TenThuongHieu == request.TenThuongHieu);
                if (exists)
                {
                    return BadRequest(ApiResponse<BrandDto>.ErrorResponse("Tên thương hiệu đã tồn tại"));
                }

                var brand = new ThuongHieu
                {
                    TenThuongHieu = request.TenThuongHieu,
                    MoTa = request.MoTa,
                    Logo = request.Logo,
                    TrangThai = request.TrangThai
                };

                _context.ThuongHieus.Add(brand);
                await _context.SaveChangesAsync();

                var brandDto = new BrandDto
                {
                    IdThuongHieu = brand.IdThuongHieu,
                    TenThuongHieu = brand.TenThuongHieu,
                    MoTa = brand.MoTa,
                    Logo = brand.Logo,
                    TrangThai = brand.TrangThai,
                    SoLuongSanPham = 0
                };

                return CreatedAtAction(nameof(GetBrand), new { id = brand.IdThuongHieu },
                    ApiResponse<BrandDto>.SuccessResponse(brandDto, "Tạo thương hiệu thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi tạo thương hiệu",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Cập nhật thương hiệu (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<BrandDto>>> UpdateBrand(int id, [FromBody] UpdateBrandRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<BrandDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var brand = await _context.ThuongHieus.FindAsync(id);
                if (brand == null)
                {
                    return NotFound(ApiResponse<BrandDto>.ErrorResponse("Không tìm thấy thương hiệu"));
                }

                // Check if new name already exists (excluding current brand)
                var nameExists = await _context.ThuongHieus.AnyAsync(b =>
                    b.TenThuongHieu == request.TenThuongHieu && b.IdThuongHieu != id);
                if (nameExists)
                {
                    return BadRequest(ApiResponse<BrandDto>.ErrorResponse("Tên thương hiệu đã tồn tại"));
                }

                brand.TenThuongHieu = request.TenThuongHieu;
                brand.MoTa = request.MoTa;
                brand.Logo = request.Logo;
                brand.TrangThai = request.TrangThai;

                await _context.SaveChangesAsync();

                var productCount = await _context.SanPhams.CountAsync(p => p.IdThuongHieu == id);

                var brandDto = new BrandDto
                {
                    IdThuongHieu = brand.IdThuongHieu,
                    TenThuongHieu = brand.TenThuongHieu,
                    MoTa = brand.MoTa,
                    Logo = brand.Logo,
                    TrangThai = brand.TrangThai,
                    SoLuongSanPham = productCount
                };

                return Ok(ApiResponse<BrandDto>.SuccessResponse(brandDto, "Cập nhật thương hiệu thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi cập nhật thương hiệu",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa thương hiệu (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBrand(int id)
        {
            try
            {
                var brand = await _context.ThuongHieus.FindAsync(id);
                if (brand == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy thương hiệu"));
                }

                // Check if brand has products
                var hasProducts = await _context.SanPhams.AnyAsync(p => p.IdThuongHieu == id);
                if (hasProducts)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Không thể xóa thương hiệu đã có sản phẩm. Vui lòng đổi trạng thái thành 'Không hoạt động'"));
                }

                _context.ThuongHieus.Remove(brand);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Xóa thương hiệu thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa thương hiệu",
                    new List<string> { ex.Message }));
            }
        }
    }
}
