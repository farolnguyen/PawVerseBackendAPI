using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Helpers;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Product;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm với filtering, sorting và pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts([FromQuery] ProductFilterRequest filter)
        {
            try
            {
                var query = _context.SanPhams
                    .Include(p => p.IdDanhMucNavigation)
                    .Include(p => p.IdThuongHieuNavigation)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchLower = filter.SearchTerm.ToLower();
                    query = query.Where(p =>
                        p.TenSanPham.ToLower().Contains(searchLower) ||
                        p.MoTa.ToLower().Contains(searchLower));
                }

                if (filter.IdDanhMuc.HasValue)
                {
                    query = query.Where(p => p.IdDanhMuc == filter.IdDanhMuc.Value);
                }

                if (filter.IdThuongHieu.HasValue)
                {
                    query = query.Where(p => p.IdThuongHieu == filter.IdThuongHieu.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                {
                    query = query.Where(p => p.TrangThai == filter.TrangThai);
                }

                if (filter.GiaMin.HasValue)
                {
                    query = query.Where(p => (p.GiaKhuyenMai ?? p.GiaBan) >= filter.GiaMin.Value);
                }

                if (filter.GiaMax.HasValue)
                {
                    query = query.Where(p => (p.GiaKhuyenMai ?? p.GiaBan) <= filter.GiaMax.Value);
                }

                if (filter.CoKhuyenMai == true)
                {
                    query = query.Where(p => p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan);
                }

                if (filter.SanPhamMoi == true)
                {
                    var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                    query = query.Where(p => p.NgayTao >= thirtyDaysAgo);
                }

                if (filter.SanPhamBanChay == true)
                {
                    query = query.Where(p => p.SoLuongDaBan > 10); // Threshold
                }

                // Apply sorting
                query = filter.SortBy.ToLower() switch
                {
                    "tensanpham" => filter.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(p => p.TenSanPham)
                        : query.OrderByDescending(p => p.TenSanPham),
                    "giaban" => filter.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(p => p.GiaKhuyenMai ?? p.GiaBan)
                        : query.OrderByDescending(p => p.GiaKhuyenMai ?? p.GiaBan),
                    "soluongdaban" => filter.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(p => p.SoLuongDaBan)
                        : query.OrderByDescending(p => p.SoLuongDaBan),
                    "solanxem" => filter.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(p => p.SoLanXem)
                        : query.OrderByDescending(p => p.SoLanXem),
                    _ => filter.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(p => p.NgayTao)
                        : query.OrderByDescending(p => p.NgayTao)
                };

                // Map to DTO
                var dtoQuery = query.Select(p => new ProductDto
                {
                    IdSanPham = p.IdSanPham,
                    TenSanPham = p.TenSanPham,
                    TenAlias = p.TenAlias,
                    IdDanhMuc = p.IdDanhMuc,
                    TenDanhMuc = p.IdDanhMucNavigation != null ? p.IdDanhMucNavigation.TenDanhMuc : "",
                    IdThuongHieu = p.IdThuongHieu,
                    TenThuongHieu = p.IdThuongHieuNavigation != null ? p.IdThuongHieuNavigation.TenThuongHieu : "",
                    TrongLuong = p.TrongLuong,
                    MauSac = p.MauSac,
                    XuatXu = p.XuatXu,
                    MoTa = p.MoTa,
                    SoLuongTonKho = p.SoLuongTonKho,
                    SoLuongDaBan = p.SoLuongDaBan,
                    GiaBan = p.GiaBan,
                    GiaVon = p.GiaVon,
                    GiaKhuyenMai = p.GiaKhuyenMai,
                    HinhAnh = p.HinhAnh,
                    NgaySanXuat = p.NgaySanXuat,
                    HanSuDung = p.HanSuDung,
                    TrangThai = p.TrangThai,
                    SoLanXem = p.SoLanXem,
                    NgayTao = p.NgayTao,
                    NgayCapNhat = p.NgayCapNhat
                });

                // Apply pagination
                var pagedResult = await PaginationHelper.CreateAsync(dtoQuery, filter.PageNumber, filter.PageSize);

                return Ok(ApiResponse<PagedResult<ProductDto>>.SuccessResponse(pagedResult));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<ProductDto>>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy danh sách sản phẩm",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Lấy chi tiết sản phẩm theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            try
            {
                var product = await _context.SanPhams
                    .Include(p => p.IdDanhMucNavigation)
                    .Include(p => p.IdThuongHieuNavigation)
                    .FirstOrDefaultAsync(p => p.IdSanPham == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<ProductDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // Increase view count
                product.SoLanXem++;
                await _context.SaveChangesAsync();

                var productDto = new ProductDto
                {
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    TenAlias = product.TenAlias,
                    IdDanhMuc = product.IdDanhMuc,
                    TenDanhMuc = product.IdDanhMucNavigation?.TenDanhMuc ?? "",
                    IdThuongHieu = product.IdThuongHieu,
                    TenThuongHieu = product.IdThuongHieuNavigation?.TenThuongHieu ?? "",
                    TrongLuong = product.TrongLuong,
                    MauSac = product.MauSac,
                    XuatXu = product.XuatXu,
                    MoTa = product.MoTa,
                    SoLuongTonKho = product.SoLuongTonKho,
                    SoLuongDaBan = product.SoLuongDaBan,
                    GiaBan = product.GiaBan,
                    GiaVon = product.GiaVon,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    HinhAnh = product.HinhAnh,
                    NgaySanXuat = product.NgaySanXuat,
                    HanSuDung = product.HanSuDung,
                    TrangThai = product.TrangThai,
                    SoLanXem = product.SoLanXem,
                    NgayTao = product.NgayTao,
                    NgayCapNhat = product.NgayCapNhat
                };

                return Ok(ApiResponse<ProductDto>.SuccessResponse(productDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi lấy thông tin sản phẩm",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Tạo sản phẩm mới (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                // Validate category exists
                var categoryExists = await _context.DanhMucs.AnyAsync(c => c.IdDanhMuc == request.IdDanhMuc);
                if (!categoryExists)
                {
                    return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Danh mục không tồn tại"));
                }

                // Validate brand exists
                var brandExists = await _context.ThuongHieus.AnyAsync(b => b.IdThuongHieu == request.IdThuongHieu);
                if (!brandExists)
                {
                    return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Thương hiệu không tồn tại"));
                }

                // Validate dates
                if (request.HanSuDung <= request.NgaySanXuat)
                {
                    return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Hạn sử dụng phải sau ngày sản xuất"));
                }

                var product = new SanPham
                {
                    TenSanPham = request.TenSanPham,
                    TenAlias = request.TenAlias,
                    IdDanhMuc = request.IdDanhMuc,
                    IdThuongHieu = request.IdThuongHieu,
                    TrongLuong = request.TrongLuong,
                    MauSac = request.MauSac,
                    XuatXu = request.XuatXu,
                    MoTa = request.MoTa,
                    SoLuongTonKho = request.SoLuongTonKho,
                    SoLuongDaBan = 0,
                    GiaBan = request.GiaBan,
                    GiaVon = request.GiaVon,
                    GiaKhuyenMai = request.GiaKhuyenMai,
                    HinhAnh = request.HinhAnh,
                    NgaySanXuat = request.NgaySanXuat,
                    HanSuDung = request.HanSuDung,
                    TrangThai = request.TrangThai,
                    SoLanXem = 0,
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now,
                    IdDanhMucNavigationIdDanhMuc = request.IdDanhMuc,
                    IdThuongHieuNavigationIdThuongHieu = request.IdThuongHieu
                };

                _context.SanPhams.Add(product);
                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(product)
                    .Reference(p => p.IdDanhMucNavigation)
                    .LoadAsync();
                await _context.Entry(product)
                    .Reference(p => p.IdThuongHieuNavigation)
                    .LoadAsync();

                var productDto = new ProductDto
                {
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    TenAlias = product.TenAlias,
                    IdDanhMuc = product.IdDanhMuc,
                    TenDanhMuc = product.IdDanhMucNavigation?.TenDanhMuc ?? "",
                    IdThuongHieu = product.IdThuongHieu,
                    TenThuongHieu = product.IdThuongHieuNavigation?.TenThuongHieu ?? "",
                    TrongLuong = product.TrongLuong,
                    MauSac = product.MauSac,
                    XuatXu = product.XuatXu,
                    MoTa = product.MoTa,
                    SoLuongTonKho = product.SoLuongTonKho,
                    SoLuongDaBan = product.SoLuongDaBan,
                    GiaBan = product.GiaBan,
                    GiaVon = product.GiaVon,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    HinhAnh = product.HinhAnh,
                    NgaySanXuat = product.NgaySanXuat,
                    HanSuDung = product.HanSuDung,
                    TrangThai = product.TrangThai,
                    SoLanXem = product.SoLanXem,
                    NgayTao = product.NgayTao,
                    NgayCapNhat = product.NgayCapNhat
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.IdSanPham },
                    ApiResponse<ProductDto>.SuccessResponse(productDto, "Tạo sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi tạo sản phẩm",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Cập nhật sản phẩm (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<ProductDto>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // Validate category exists
                var categoryExists = await _context.DanhMucs.AnyAsync(c => c.IdDanhMuc == request.IdDanhMuc);
                if (!categoryExists)
                {
                    return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Danh mục không tồn tại"));
                }

                // Validate brand exists
                var brandExists = await _context.ThuongHieus.AnyAsync(b => b.IdThuongHieu == request.IdThuongHieu);
                if (!brandExists)
                {
                    return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Thương hiệu không tồn tại"));
                }

                // Validate dates
                if (request.HanSuDung <= request.NgaySanXuat)
                {
                    return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Hạn sử dụng phải sau ngày sản xuất"));
                }

                // Update properties
                product.TenSanPham = request.TenSanPham;
                product.TenAlias = request.TenAlias;
                product.IdDanhMuc = request.IdDanhMuc;
                product.IdThuongHieu = request.IdThuongHieu;
                product.TrongLuong = request.TrongLuong;
                product.MauSac = request.MauSac;
                product.XuatXu = request.XuatXu;
                product.MoTa = request.MoTa;
                product.SoLuongTonKho = request.SoLuongTonKho;
                product.GiaBan = request.GiaBan;
                product.GiaVon = request.GiaVon;
                product.GiaKhuyenMai = request.GiaKhuyenMai;
                product.HinhAnh = request.HinhAnh;
                product.NgaySanXuat = request.NgaySanXuat;
                product.HanSuDung = request.HanSuDung;
                product.TrangThai = request.TrangThai;
                product.NgayCapNhat = DateTime.Now;
                product.IdDanhMucNavigationIdDanhMuc = request.IdDanhMuc;
                product.IdThuongHieuNavigationIdThuongHieu = request.IdThuongHieu;

                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(product)
                    .Reference(p => p.IdDanhMucNavigation)
                    .LoadAsync();
                await _context.Entry(product)
                    .Reference(p => p.IdThuongHieuNavigation)
                    .LoadAsync();

                var productDto = new ProductDto
                {
                    IdSanPham = product.IdSanPham,
                    TenSanPham = product.TenSanPham,
                    TenAlias = product.TenAlias,
                    IdDanhMuc = product.IdDanhMuc,
                    TenDanhMuc = product.IdDanhMucNavigation?.TenDanhMuc ?? "",
                    IdThuongHieu = product.IdThuongHieu,
                    TenThuongHieu = product.IdThuongHieuNavigation?.TenThuongHieu ?? "",
                    TrongLuong = product.TrongLuong,
                    MauSac = product.MauSac,
                    XuatXu = product.XuatXu,
                    MoTa = product.MoTa,
                    SoLuongTonKho = product.SoLuongTonKho,
                    SoLuongDaBan = product.SoLuongDaBan,
                    GiaBan = product.GiaBan,
                    GiaVon = product.GiaVon,
                    GiaKhuyenMai = product.GiaKhuyenMai,
                    HinhAnh = product.HinhAnh,
                    NgaySanXuat = product.NgaySanXuat,
                    HanSuDung = product.HanSuDung,
                    TrangThai = product.TrangThai,
                    SoLanXem = product.SoLanXem,
                    NgayTao = product.NgayTao,
                    NgayCapNhat = product.NgayCapNhat
                };

                return Ok(ApiResponse<ProductDto>.SuccessResponse(productDto, "Cập nhật sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi cập nhật sản phẩm",
                    new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Xóa sản phẩm (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // Check if product is in any orders
                var hasOrders = await _context.ChiTietDonHangs.AnyAsync(ct => ct.IdSanPham == id);
                if (hasOrders)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Không thể xóa sản phẩm đã có trong đơn hàng. Vui lòng đổi trạng thái thành 'Ngừng kinh doanh'"));
                }

                _context.SanPhams.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Đã xảy ra lỗi khi xóa sản phẩm",
                    new List<string> { ex.Message }));
            }
        }
    }
}
