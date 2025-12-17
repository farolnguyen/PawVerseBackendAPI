# 🐾 PawVerse API

Backend REST API cho ứng dụng thương mại điện tử chuyên về sản phẩm thú cưng, được xây dựng bằng .NET 8 Web API với tích hợp AI.

## 📋 Tổng Quan

PawVerse là một nền tảng e-commerce toàn diện dành cho thú cưng, cung cấp:
- **E-commerce hoàn chỉnh**: Quản lý sản phẩm, đơn hàng, thanh toán
- **AI-Powered Features**: Nhận diện giống chó/mèo, chatbot tư vấn, virtual try-on
- **Authentication**: JWT + OAuth (Google, GitHub)
- **Admin Dashboard**: Quản lý sản phẩm, đơn hàng, thống kê

## ✨ Tính Năng Chính

### 🛒 E-Commerce Core
- **Sản phẩm**: CRUD, tìm kiếm, lọc theo danh mục/thương hiệu, sắp xếp
- **Giỏ hàng**: Thêm/xóa/cập nhật, tính tổng tự động
- **Đơn hàng**: Đặt hàng, theo dõi trạng thái (6 trạng thái), lịch sử
- **Wishlist**: Lưu sản phẩm yêu thích
- **Thanh toán**: COD, thẻ tín dụng, ví điện tử

### 🤖 AI Features
- **Breed Detection**: Nhận diện giống chó/mèo bằng YOLOv8 + CNN, gợi ý sản phẩm phù hợp
- **AI Chatbot**: Tư vấn sản phẩm thông minh với RAG (Retrieval-Augmented Generation)
- **Virtual Try-On**: Demo AI try-on với Stable Diffusion + ControlNet (Kaggle)

### 👥 User Management
- **Authentication**: JWT, Google/GitHub OAuth
- **Authorization**: Role-based (Admin, User)
- **Profile**: Quản lý thông tin cá nhân, đổi mật khẩu

### 📊 Admin Panel
- **Dashboard**: Thống kê doanh thu, đơn hàng, sản phẩm
- **Quản lý**: Sản phẩm, danh mục, thương hiệu, đơn hàng
- **Báo cáo**: Doanh thu theo thời gian, top sản phẩm

## 🛠️ Công Nghệ

- **.NET 8**: Web API framework
- **Entity Framework Core**: ORM, SQL Server
- **ASP.NET Identity**: Authentication & Authorization
- **JWT Bearer**: Token-based authentication
- **Python**: AI services (YOLOv8, Transformers, Diffusers)
- **Swagger/OpenAPI**: API documentation

## 📦 Cấu Trúc Project

```
PawVerseAPI/
├── Controllers/          # API endpoints
│   ├── AuthController.cs         # Authentication
│   ├── ProductsController.cs     # Sản phẩm
│   ├── OrdersController.cs       # Đơn hàng
│   ├── BreedDetectionController  # AI nhận diện
│   ├── ChatbotController.cs      # AI chatbot
│   └── ...
├── Models/              # Entity models
├── Data/                # DbContext, migrations
├── Services/            # Business logic
├── Python/              # AI models & scripts
│   ├── breed_detection.py        # YOLOv8 + CNN
│   ├── inference_pipeline.py     # Try-on pipeline
│   └── tryon_streamlit_app.py    # Demo UI
├── wwwroot/             # Static files (images)
└── Program.cs           # App configuration
```

## 🚀 Cách Chạy Dự Án

### 1️⃣ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB hoặc SQL Server Express)
- Python 3.11+ (cho AI features)
- Visual Studio 2022 hoặc VS Code

### 2️⃣ Clone Repository

```bash
git clone <repository-url>
cd PawVerseAPI
```

### 3️⃣ Cấu Hình Database

**Cập nhật `appsettings.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PawVerseDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "your-secret-key-here-min-32-chars",
    "Issuer": "PawVerseAPI",
    "Audience": "PawVerseClient",
    "ExpiresInMinutes": 60
  }
}
```

**Apply Migrations:**

```bash
# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update
```

### 4️⃣ Chạy Backend API

```bash
# Development mode
dotnet run

# Hoặc với hot reload
dotnet watch run
```

API sẽ chạy tại: **https://localhost:7139** (hoặc http://localhost:5139)

### 5️⃣ Truy Cập Swagger UI

Mở browser: **https://localhost:7139**

Swagger UI cung cấp:
- API documentation đầy đủ
- Test endpoints trực tiếp
- Schema definitions

### 6️⃣ Cài Đặt AI Features (Optional)

**Setup Python Environment:**

```bash
cd Python

# Tạo virtual environment
python -m venv venv

# Activate (Windows)
venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

**Download AI Models:**

```bash
# YOLOv8 (breed detection)
python breed_detection.py  # Auto-download on first run

# Hugging Face models (chatbot)
# Models download tự động khi API call lần đầu
```

## 🔐 Authentication

### Đăng Ký & Đăng Nhập

```http
POST /api/auth/register
POST /api/auth/login
```

### Sử dụng JWT Token

```http
Authorization: Bearer {your-jwt-token}
```

### OAuth (Google/GitHub)

Configure trong `appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    }
  }
}
```

## 📚 API Endpoints

### Products
- `GET /api/products` - Danh sách sản phẩm
- `GET /api/products/{id}` - Chi tiết sản phẩm
- `GET /api/products/search?keyword={keyword}` - Tìm kiếm

### Cart
- `GET /api/cart` - Xem giỏ hàng
- `POST /api/cart` - Thêm sản phẩm
- `PUT /api/cart/{id}` - Cập nhật số lượng
- `DELETE /api/cart/{id}` - Xóa sản phẩm

### Orders
- `POST /api/orders` - Đặt hàng
- `GET /api/orders` - Lịch sử đơn hàng
- `GET /api/orders/{id}` - Chi tiết đơn hàng
- `PUT /api/orders/{id}/cancel` - Hủy đơn hàng

### AI Features
- `POST /api/breed-detection` - Nhận diện giống (upload ảnh)
- `POST /api/chatbot/send-message` - Chat với AI

### Admin
- `GET /api/admin/statistics` - Thống kê tổng quan
- `GET /api/admin/orders` - Quản lý đơn hàng
- `PUT /api/admin/orders/{id}/status` - Cập nhật trạng thái

## 🧪 Testing

```bash
# Run tests
dotnet test

# Test API với Swagger UI
# https://localhost:7139
```

## 📝 Seed Data

Database được seed tự động với:
- **Sample products**: ~50 sản phẩm
- **Categories**: Thức ăn, đồ chơi, phụ kiện, chăm sóc
- **Brands**: Royal Canin, Whiskas, Pedigree, etc.

## 🐛 Troubleshooting

### Database Connection Error
```bash
# Kiểm tra connection string
# Đảm bảo SQL Server đang chạy
# Chạy lại migrations
dotnet ef database update
```

### Port Already in Use
```bash
# Thay đổi port trong Properties/launchSettings.json
```

### AI Models Not Loading
```bash
# Kiểm tra Python environment
python --version  # >= 3.11

# Reinstall dependencies
pip install -r Python/requirements.txt
```

## 📄 License

This project is for educational purposes.

## 👥 Contributors

- Backend API: .NET 8, Entity Framework Core
- AI Features: YOLOv8, Transformers, Stable Diffusion
- Mobile App: Flutter (separate repo)
- Web Frontend: React (separate repo)

## 🔗 Related Repositories

- **Mobile App**: PawVerseMobile (Flutter), PawVerseFrontend (React)
https://drive.google.com/drive/folders/1P5wuWVVmG-dcCUO_Ujkx1krAudrAaTJ9?usp=sharing
- **AI Try-On Demo**: Kaggle Notebook (Python/notebooks/)
https://www.kaggle.com/code/farolnguyen1/dacn-task-2-streamlit-demo

---

**Built with ❤️ for pets and their owners** 🐕🐈
