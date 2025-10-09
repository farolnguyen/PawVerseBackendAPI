# PawVerse API

API Backend for PawVerse Pet Shop Mobile Application

## Giai Đoạn 1: Thiết Lập Dự Án - ✅ HOÀN THÀNH

### Đã Hoàn Thành

1. **✅ Tạo dự án ASP.NET Core Web API**
   - Framework: .NET 8.0
   - Template: webapi

2. **✅ Cài đặt các NuGet packages cần thiết:**
   - `Microsoft.EntityFrameworkCore.SqlServer` (8.0.5)
   - `Microsoft.EntityFrameworkCore.Design` (8.0.5)
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (8.0.0)
   - `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.0)
   - `Swashbuckle.AspNetCore` (6.5.0)

3. **✅ Tạo cấu trúc thư mục:**
   ```
   PawVerseAPI/
   ├── Controllers/
   │   └── TestController.cs
   ├── Models/
   │   ├── Entities/      (Đã copy tất cả từ dự án cũ)
   │   └── DTOs/          (Sẵn sàng cho các DTO)
   ├── Data/
   │   └── ApplicationDbContext.cs
   ├── Services/
   │   ├── Interfaces/
   │   └── Implementations/
   ├── Helpers/
   ├── Middleware/
   └── Program.cs
   ```

4. **✅ Copy Models và DbContext từ dự án PawVerse cũ**
   - Đã copy tất cả Entity models
   - Đã copy ApplicationDbContext
   - Đã cập nhật namespace sang `PawVerseAPI.Models.Entities`

5. **✅ Cấu hình appsettings.json:**
   - Connection string đến SQL Server
   - JWT settings (Key, Issuer, Audience, ExpiryInMinutes)
   - Google/GitHub OAuth settings
   - Coze API settings

6. **✅ Cấu hình Program.cs:**
   - DbContext với SQL Server
   - ASP.NET Core Identity
   - JWT Authentication
   - CORS policy "AllowAll"
   - Swagger/OpenAPI với JWT support
   - Controllers với JSON serialization

7. **✅ Tạo Test Controller:**
   - `GET /api/test` - Test basic API
   - `GET /api/test/health` - Health check endpoint

8. **✅ Build thành công:**
   - Dự án build thành công
   - Chỉ có warnings (không có errors)

## Cấu Trúc Database

Sử dụng chung database với dự án PawVerse cũ:
- Server: `FAROL-PC\SQLEXPRESS`
- Database: `PawVerse`

## Cấu Hình JWT

```json
{
  "Jwt": {
    "Key": "PawVerseAPI-SecretKey-ForJWT-Authentication-2025-MinLength32Chars",
    "Issuer": "PawVerseAPI",
    "Audience": "PawVerseAPI-Users",
    "ExpiryInMinutes": 60
  }
}
```

## Swagger UI

Khi chạy ứng dụng, Swagger UI sẽ có sẵn tại:
- URL: `https://localhost:{port}/`
- Hỗ trợ JWT Bearer authentication

## Chạy Ứng Dụng

```bash
cd D:\1Hutech\workspace05102025\PawVerseAPI
dotnet run
```

Hoặc với profile cụ thể:
```bash
dotnet run --launch-profile https
```

## CORS Policy

API đã được cấu hình CORS "AllowAll" để cho phép:
- Bất kỳ origin nào
- Bất kỳ HTTP method nào
- Bất kỳ header nào

Điều này phù hợp cho development. Trong production, cần hạn chế CORS policy.

## Giai Đoạn 2: Authentication & Authorization API - ✅ HOÀN THÀNH

### Đã Hoàn Thành

1. **✅ Tạo các DTOs cho Authentication:**
   - `RegisterRequest` - Đăng ký tài khoản
   - `LoginRequest` - Đăng nhập
   - `LoginResponse` - Response với JWT token
   - `UserDto` - Thông tin người dùng
   - `UserProfileDto` - Profile chi tiết
   - `UpdateProfileRequest` - Cập nhật profile
   - `ChangePasswordRequest` - Đổi mật khẩu
   - `RefreshTokenRequest` - Làm mới token
   - `ApiResponse<T>` - Generic response wrapper

2. **✅ Tạo JwtHelper service:**
   - `GenerateJwtToken()` - Tạo JWT token
   - `GenerateRefreshToken()` - Tạo refresh token
   - `GetPrincipalFromExpiredToken()` - Validate expired token
   - `GetTokenExpiryTime()` - Lấy thời gian hết hạn

3. **✅ Tạo AuthController với các endpoints:**
   - `POST /api/auth/register` - Đăng ký tài khoản mới
   - `POST /api/auth/login` - Đăng nhập
   - `POST /api/auth/refresh-token` - Làm mới JWT token
   - `GET /api/auth/me` - Lấy thông tin user hiện tại (requires auth)
   - `PUT /api/auth/profile` - Cập nhật profile (requires auth)
   - `PUT /api/auth/change-password` - Đổi mật khẩu (requires auth)

4. **✅ Tạo RoleSeeder:**
   - Tự động tạo roles "User" và "Admin" khi khởi động
   - User mới mặc định được gán role "User"

5. **✅ Build thành công:**
   - Tất cả endpoints hoạt động
   - Validation đầy đủ
   - Error handling chuẩn

### Các Endpoint API Authentication

#### 1. Register (Đăng ký)
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "confirmPassword": "password123",
  "fullName": "Nguyen Van A",
  "phoneNumber": "0123456789"
}
```

#### 2. Login (Đăng nhập)
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

#### 3. Get Current User
```http
GET /api/auth/me
Authorization: Bearer {token}
```

#### 4. Update Profile
```http
PUT /api/auth/profile
Authorization: Bearer {token}
Content-Type: application/json

{
  "fullName": "Nguyen Van A Updated",
  "phoneNumber": "0987654321",
  "diaChi": "123 Street, City",
  "gioiTinh": "Nam",
  "ngaySinh": "1990-01-01"
}
```

#### 5. Change Password
```http
PUT /api/auth/change-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "oldPassword": "oldpassword123",
  "newPassword": "newpassword123",
  "confirmNewPassword": "newpassword123"
}
```

#### 6. Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "token": "expired-jwt-token",
  "refreshToken": "refresh-token"
}
```

## Giai Đoạn 3: Products & Categories API - ✅ HOÀN THÀNH

### Đã Hoàn Thành

1. **✅ Tạo DTOs cho Products:**
   - `ProductDto` - Product với thông tin đầy đủ, tính toán giá khuyến mãi
   - `CreateProductRequest` - Tạo sản phẩm mới với validation
   - `UpdateProductRequest` - Cập nhật sản phẩm
   - `ProductFilterRequest` - Filtering, sorting, pagination parameters

2. **✅ Tạo DTOs cho Categories:**
   - `CategoryDto` - Category với số lượng sản phẩm
   - `CreateCategoryRequest` - Tạo danh mục mới
   - `UpdateCategoryRequest` - Cập nhật danh mục

3. **✅ Tạo DTOs cho Brands:**
   - `BrandDto` - Brand với số lượng sản phẩm
   - `CreateBrandRequest` - Tạo thương hiệu mới
   - `UpdateBrandRequest` - Cập nhật thương hiệu

4. **✅ Tạo Pagination Helper:**
   - `PagedResult<T>` - Generic pagination wrapper
   - `PaginationHelper` - Static helper cho pagination
   - Max page size limit: 100 items

5. **✅ Tạo ProductsController (5 endpoints):**
   - `GET /api/products` - Danh sách sản phẩm (filtering, sorting, pagination)
   - `GET /api/products/{id}` - Chi tiết sản phẩm (tăng view count)
   - `POST /api/products` - Tạo sản phẩm (Admin only)
   - `PUT /api/products/{id}` - Cập nhật sản phẩm (Admin only)
   - `DELETE /api/products/{id}` - Xóa sản phẩm (Admin only)

6. **✅ Tạo CategoriesController (5 endpoints):**
   - `GET /api/categories` - Danh sách danh mục
   - `GET /api/categories/{id}` - Chi tiết danh mục
   - `POST /api/categories` - Tạo danh mục (Admin only)
   - `PUT /api/categories/{id}` - Cập nhật danh mục (Admin only)
   - `DELETE /api/categories/{id}` - Xóa danh mục (Admin only)

7. **✅ Tạo BrandsController (5 endpoints):**
   - `GET /api/brands` - Danh sách thương hiệu
   - `GET /api/brands/{id}` - Chi tiết thương hiệu
   - `POST /api/brands` - Tạo thương hiệu (Admin only)
   - `PUT /api/brands/{id}` - Cập nhật thương hiệu (Admin only)
   - `DELETE /api/brands/{id}` - Xóa thương hiệu (Admin only)

### Tính Năng Filtering & Sorting cho Products

**Filter Parameters:**
- `searchTerm` - Tìm kiếm theo tên, mô tả
- `idDanhMuc` - Lọc theo danh mục
- `idThuongHieu` - Lọc theo thương hiệu
- `trangThai` - Lọc theo trạng thái
- `giaMin`, `giaMax` - Lọc theo khoảng giá
- `coKhuyenMai` - Chỉ sản phẩm có khuyến mãi
- `sanPhamMoi` - Sản phẩm mới (30 ngày gần nhất)
- `sanPhamBanChay` - Sản phẩm bán chạy (>10 đơn)

**Sort Parameters:**
- `sortBy` - NgayTao, TenSanPham, GiaBan, SoLuongDaBan, SoLanXem
- `sortOrder` - asc, desc

**Pagination:**
- `pageNumber` - Trang hiện tại (default: 1)
- `pageSize` - Số items/trang (default: 20, max: 100)

### API Examples

#### Get Products với Filters
```http
GET /api/products?searchTerm=thuc+an&idDanhMuc=1&coKhuyenMai=true&sortBy=giaban&sortOrder=asc&pageNumber=1&pageSize=20
```

#### Get Product Detail
```http
GET /api/products/123
```

#### Create Product (Admin)
```http
POST /api/products
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "tenSanPham": "Thức ăn cho chó",
  "tenAlias": "thuc-an-cho-cho",
  "idDanhMuc": 1,
  "idThuongHieu": 2,
  "trongLuong": "1kg",
  "mauSac": "Nâu",
  "xuatXu": "Việt Nam",
  "moTa": "Mô tả sản phẩm...",
  "soLuongTonKho": 100,
  "giaBan": 150000,
  "giaVon": 100000,
  "giaKhuyenMai": 135000,
  "hinhAnh": "/images/products/sp1.jpg",
  "ngaySanXuat": "2025-01-01",
  "hanSuDung": "2026-01-01",
  "trangThai": "Còn hàng"
}
```

## Giai Đoạn 4: Shopping Cart API - ✅ HOÀN THÀNH

### Đã Hoàn Thành

1. **✅ Tạo DTOs cho Cart:**
   - `CartDto` - Giỏ hàng với computed properties (TongSoLuong, TongTien, SoMucHang)
   - `CartItemDto` - Item trong giỏ với thông tin sản phẩm, giá hiển thị, thành tiền
   - `AddToCartRequest` - Thêm sản phẩm vào giỏ với validation
   - `UpdateCartItemRequest` - Cập nhật số lượng

2. **✅ Tạo CartController (6 endpoints):**
   - `GET /api/cart` - Lấy giỏ hàng hiện tại (Auth required)
   - `POST /api/cart/items` - Thêm sản phẩm vào giỏ (Auth required)
   - `PUT /api/cart/items/{id}` - Cập nhật số lượng (Auth required)
   - `DELETE /api/cart/items/{id}` - Xóa sản phẩm khỏi giỏ (Auth required)
   - `DELETE /api/cart/clear` - Xóa toàn bộ giỏ hàng (Auth required)
   - `GET /api/cart/count` - Lấy tổng số lượng items (Auth required)

3. **✅ Business Logic:**
   - Auto tạo giỏ hàng nếu user chưa có
   - Merge items nếu sản phẩm đã có trong giỏ
   - Validate tồn kho trước khi thêm/cập nhật
   - Validate trạng thái sản phẩm (Còn hàng)
   - Real-time stock checking

4. **✅ Computed Properties:**
   - `GiaHienThi` - Giá sau khuyến mãi hoặc giá gốc
   - `ThanhTien` - Tổng tiền = GiaHienThi × SoLuong
   - `CoKhuyenMai` - Check có khuyến mãi
   - `ConHang` - Check còn hàng
   - `TongSoLuong` - Tổng số lượng tất cả items
   - `TongTien` - Tổng tiền giỏ hàng
   - `SoMucHang` - Số loại sản phẩm khác nhau

### Tính Năng Đặc Biệt

**Smart Add to Cart:**
- Nếu sản phẩm đã có trong giỏ → Tự động tăng số lượng
- Validate số lượng không vượt quá tồn kho
- Kiểm tra trạng thái sản phẩm

**Stock Protection:**
- Không cho thêm/update vượt quá tồn kho
- Thông báo rõ số lượng còn lại
- Kiểm tra trạng thái "Còn hàng"

**Auto Cart Creation:**
- Tự động tạo giỏ hàng cho user mới
- Mỗi user chỉ có 1 giỏ hàng duy nhất

### API Examples

#### 1. Get Cart
```http
GET /api/cart
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "userId": "user-id",
    "items": [
      {
        "id": 1,
        "sanPhamId": 10,
        "tenSanPham": "Thức ăn cho chó",
        "hinhAnh": "/images/product.jpg",
        "giaBan": 150000,
        "giaKhuyenMai": 135000,
        "soLuong": 2,
        "soLuongTonKho": 50,
        "trangThai": "Còn hàng",
        "giaHienThi": 135000,
        "thanhTien": 270000,
        "coKhuyenMai": true,
        "conHang": true
      }
    ],
    "tongSoLuong": 2,
    "tongTien": 270000,
    "soMucHang": 1
  }
}
```

#### 2. Add to Cart
```http
POST /api/cart/items
Authorization: Bearer {token}
Content-Type: application/json

{
  "sanPhamId": 10,
  "soLuong": 2
}
```

#### 3. Update Cart Item
```http
PUT /api/cart/items/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "soLuong": 5
}
```

#### 4. Remove from Cart
```http
DELETE /api/cart/items/1
Authorization: Bearer {token}
```

#### 5. Clear Cart
```http
DELETE /api/cart/clear
Authorization: Bearer {token}
```

#### 6. Get Cart Count (for badge)
```http
GET /api/cart/count
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": 5
}
```

## Giai Đoạn 5: Order & Checkout API - ✅ HOÀN THÀNH

### Đã Hoàn Thành

1. **✅ Tạo DTOs cho Order:**
   - `OrderDto` - Đơn hàng với thông tin đầy đủ, computed SoLuongSanPham
   - `OrderItemDto` - Chi tiết sản phẩm trong đơn với computed ThanhTien
   - `CreateOrderRequest` - Tạo đơn từ giỏ hàng với validation
   - `UpdateOrderStatusRequest` - Cập nhật trạng thái (Admin)
   - `OrderFilterRequest` - Filter & pagination cho danh sách đơn

2. **✅ Tạo OrdersController (6 endpoints):**
   - `POST /api/orders` - Tạo đơn hàng từ giỏ (Auth required)
   - `GET /api/orders` - Lịch sử đơn hàng của user (Auth required)
   - `GET /api/orders/{id}` - Chi tiết đơn hàng (Auth required)
   - `PUT /api/orders/{id}/cancel` - Hủy đơn (Auth required)
   - `GET /api/orders/admin` - Quản lý tất cả đơn hàng (Admin only)
   - `PUT /api/orders/{id}/status` - Cập nhật trạng thái (Admin only)

3. **✅ Checkout Flow - Smart Order Creation:**
   - ✅ Validate giỏ hàng không rỗng
   - ✅ Validate tất cả sản phẩm còn hàng
   - ✅ Validate số lượng tồn kho đủ
   - ✅ Tính toán tự động: Tổng tiền sản phẩm + Phí vận chuyển - Coupon
   - ✅ Apply coupon (Percent or Fixed)
   - ✅ Tạo đơn hàng + Chi tiết đơn hàng
   - ✅ Update stock: Giảm tồn kho, tăng số lượng đã bán
   - ✅ Clear giỏ hàng sau khi đặt thành công
   - ✅ Return order details ngay lập tức

4. **✅ Order Management:**
   - Filter theo trạng thái, khoảng thời gian
   - Search theo tên KH, SĐT, mã đơn
   - Sorting linh hoạt
   - Pagination
   - User chỉ thấy đơn của mình
   - Admin thấy tất cả đơn

5. **✅ Cancel Order Logic:**
   - Chỉ cho hủy khi trạng thái "Chờ xác nhận"
   - Auto restore tồn kho khi hủy
   - Update NgayHuy timestamp
   - Security: User chỉ hủy được đơn của mình

6. **✅ Admin Functions:**
   - Xem tất cả đơn hàng
   - Update trạng thái đơn
   - Update ngày giao hàng dự kiến
   - Hủy đơn (với restore stock)

### Order Status Lifecycle

```
Chờ xác nhận → Đã xác nhận → Đang giao hàng → Đã giao hàng
      ↓
   Đã hủy (can cancel only at "Chờ xác nhận")
```

### API Examples

#### 1. Create Order (Checkout)
```http
POST /api/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "tenKhachHang": "Nguyen Van A",
  "soDienThoai": "0123456789",
  "diaChiGiaoHang": "123 Nguyen Van Linh, Q7, TP.HCM",
  "phuongThucThanhToan": "COD",
  "idVanChuyen": 1,
  "idCoupon": 5,
  "ghiChu": "Giao giờ hành chính"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Đặt hàng thành công",
  "data": {
    "idDonHang": 101,
    "tenKhachHang": "Nguyen Van A",
    "ngayDatHang": "2025-10-05T03:30:00",
    "ngayGiaoHangDuKien": "2025-10-08T03:30:00",
    "trangThai": "Chờ xác nhận",
    "tongTien": 500000,
    "soLuongSanPham": 3
  }
}
```

#### 2. Get My Orders with Filter
```http
GET /api/orders?trangThai=Chờ xác nhận&pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

#### 3. Get Order Detail
```http
GET /api/orders/101
Authorization: Bearer {token}
```

**Response includes full items:**
```json
{
  "success": true,
  "data": {
    "idDonHang": 101,
    "items": [
      {
        "idSanPham": 10,
        "tenSanPham": "Thức ăn cho chó",
        "hinhAnh": "/images/product.jpg",
        "soLuong": 2,
        "donGia": 135000,
        "thanhTien": 270000
      }
    ],
    "tongTien": 500000,
    ...
  }
}
```

#### 4. Cancel Order
```http
PUT /api/orders/101/cancel
Authorization: Bearer {token}
```

#### 5. Admin - Get All Orders
```http
GET /api/orders/admin?trangThai=Chờ xác nhận&tuNgay=2025-10-01
Authorization: Bearer {admin-token}
```

#### 6. Admin - Update Order Status
```http
PUT /api/orders/101/status
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "trangThai": "Đã xác nhận",
  "ngayGiaoHangDuKien": "2025-10-08T00:00:00"
}
```

### Smart Features

**Auto Stock Management:**
- Create order → Giảm tồn kho, tăng đã bán
- Cancel order → Restore tồn kho, giảm đã bán
- Validate trước mỗi action

**Coupon Support:**
- Percent discount với/không có giảm tối đa
- Fixed discount
- Auto apply khi checkout

**Security & Authorization:**
- User chỉ thấy/thao tác đơn của mình
- Admin có full quyền
- Validate ownership cho mọi action

## Giai Đoạn 6: Wishlist API - ✅ HOÀN THÀNH

### Đã Hoàn Thành

1. **✅ Tạo DTOs cho Wishlist:**
   - `WishlistItemDto` - Item trong wishlist với thông tin sản phẩm đầy đủ
   - `AddToWishlistRequest` - Request thêm sản phẩm vào wishlist

2. **✅ Tạo WishlistController (6 endpoints):**
   - `GET /api/wishlist` - Lấy danh sách yêu thích (Auth required)
   - `POST /api/wishlist` - Thêm sản phẩm vào wishlist (Auth required)
   - `DELETE /api/wishlist/{id}` - Xóa khỏi wishlist by wishlist ID (Auth required)
   - `DELETE /api/wishlist/product/{productId}` - Xóa by product ID (Auth required)
   - `GET /api/wishlist/check/{productId}` - Kiểm tra sản phẩm trong wishlist (Auth required)
   - `GET /api/wishlist/count` - Lấy số lượng items (Auth required)
   - `DELETE /api/wishlist/clear` - Xóa toàn bộ wishlist (Auth required) - **BONUS**

3. **✅ Computed Properties:**
   - `GiaHienThi` - Giá sau khuyến mãi
   - `CoKhuyenMai` - Check có khuyến mãi
   - `ConHang` - Check còn hàng và trạng thái

4. **✅ Smart Features:**
   - Duplicate prevention - Không cho thêm trùng
   - Sort by NgayThem DESC - Mới nhất trước
   - Security - User chỉ thao tác wishlist của mình
   - Alternative delete - Có thể xóa bằng wishlistId hoặc productId

### API Examples

#### 1. Get Wishlist
```http
GET /api/wishlist
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "idYeuThich": 1,
      "idSanPham": 10,
      "tenSanPham": "Thức ăn cho chó",
      "hinhAnh": "/images/product.jpg",
      "giaBan": 150000,
      "giaKhuyenMai": 135000,
      "trangThai": "Còn hàng",
      "soLuongTonKho": 50,
      "ngayThem": "2025-10-05T03:30:00",
      "giaHienThi": 135000,
      "coKhuyenMai": true,
      "conHang": true
    }
  ]
}
```

#### 2. Add to Wishlist
```http
POST /api/wishlist
Authorization: Bearer {token}
Content-Type: application/json

{
  "idSanPham": 10
}
```

**Success Response:**
```json
{
  "success": true,
  "message": "Đã thêm vào danh sách yêu thích",
  "data": { ... }
}
```

**Duplicate Error:**
```json
{
  "success": false,
  "message": "Sản phẩm đã có trong danh sách yêu thích"
}
```

#### 3. Remove from Wishlist
```http
DELETE /api/wishlist/1
Authorization: Bearer {token}
```

Or by product ID:
```http
DELETE /api/wishlist/product/10
Authorization: Bearer {token}
```

#### 4. Check if Product in Wishlist
```http
GET /api/wishlist/check/10
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": true  // or false
}
```

#### 5. Get Wishlist Count (for badge)
```http
GET /api/wishlist/count
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": 5
}
```

#### 6. Clear Wishlist
```http
DELETE /api/wishlist/clear
Authorization: Bearer {token}
```

### Use Cases

**Product Card - Heart Icon:**
```javascript
// Check if favorited
GET /api/wishlist/check/{productId}

// Toggle favorite
if (isFavorited) {
  DELETE /api/wishlist/product/{productId}
} else {
  POST /api/wishlist { idSanPham: productId }
}
```

**Wishlist Page:**
```javascript
// Load wishlist
GET /api/wishlist

// Remove item
DELETE /api/wishlist/{wishlistId}
```

**Header Badge:**
```javascript
// Update count
GET /api/wishlist/count
```

## Tổng Kết API đã Hoàn Thành

### 🎉 Tất Cả Giai Đoạn Đã Hoàn Thành!

**Total: 41 Endpoints**

| Module | Endpoints | Status |
|--------|-----------|---------|
| Authentication | 6 | ✅ |
| Products | 5 | ✅ |
| Categories | 5 | ✅ |
| Brands | 5 | ✅ |
| Cart | 6 | ✅ |
| Orders | 6 | ✅ |
| Wishlist | 7 | ✅ |

### Authorization Breakdown
- **Public:** 8 endpoints
- **User (Authenticated):** 22 endpoints
- **Admin Only:** 11 endpoints

### Completed Features
✅ Authentication & Authorization (JWT)
✅ Product Management (CRUD, Filter, Search, Pagination)
✅ Category & Brand Management
✅ Shopping Cart (Smart merge, Stock validation)
✅ Order & Checkout (Auto stock update, Coupon support)
✅ Wishlist (Duplicate prevention, Quick check)
✅ Role-based Access Control
✅ Swagger UI with JWT support
✅ Consistent API Response format
✅ Comprehensive error handling

### Build Status
✅ **Build Successful**
- 0 Errors
- 40 Warnings (nullable reference types)

## Các Bước Tiếp Theo (Optional)

### Giai Đoạn 7: Integration APIs (0.5 ngày)

## Lưu Ý

- Dự án này sử dụng chung database với dự án PawVerse cũ
- Namespace đã được thay đổi từ `PawVerse.Models` sang `PawVerseAPI.Models.Entities`
- JWT Authentication đã được cấu hình sẵn
- Swagger UI đã tích hợp JWT Bearer token

## 🔧 Quick Commands Reference

### **Start API:**
```powershell
dotnet run --launch-profile https
```

### **PowerShell Execution Policy (if needed):**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### **Port Management:**

**Check what's running on port:**
```powershell
# Method 1: netstat
netstat -ano | findstr :7038
netstat -ano | findstr :5000

# Method 2: PowerShell
Get-NetTCPConnection -LocalPort 7038 -ErrorAction SilentlyContinue
Get-Process -Id (Get-NetTCPConnection -LocalPort 7038).OwningProcess
```

**Kill processes:**
```powershell
# Kill specific process by PID
taskkill /PID <process-id> /F

# Kill all dotnet processes
taskkill /IM dotnet.exe /F

# Kill all PawVerseAPI processes
taskkill /IM PawVerseAPI.exe /F

# One-liner to kill process on port 7038
$p = Get-NetTCPConnection -LocalPort 7038 -ErrorAction SilentlyContinue; if($p){taskkill /PID $p.OwningProcess /F}
```

**List all .NET processes:**
```powershell
# Simple list
tasklist | findstr dotnet

# Detailed PowerShell
Get-Process -Name dotnet* | Select-Object Id, ProcessName, CPU, WorkingSet
```

### **Database Commands:**
```powershell
# Update database
dotnet ef database update

# Add migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove
```

---

**Ngày tạo:** 05/10/2025  
**Phiên bản:** 1.0.0  
**Tác giả:** PawVerse Development Team
