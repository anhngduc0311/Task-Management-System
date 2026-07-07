# Task Management System

Hệ thống Quản lý Công việc (Task Management System) được xây dựng theo kiến trúc Clean Architecture cho phần Backend (sử dụng .NET 10.0 Web API) và giao diện SPA hiện đại cho phần Frontend (sử dụng Angular).

---

## 🛠️ Công nghệ Sử dụng

### Backend (Web API)
- **Framework:** .NET 10.0 Web API (Clean Architecture)
- **Database:** Microsoft SQL Server & Entity Framework Core (EF Core)
- **Bảo mật:** JWT Authentication & Custom Dynamic Rate Limiting & Security Headers (HSTS, CSP, X-Frame-Options)
- **Kiểm thử:** xUnit, WebApplicationFactory (Integration Testing), InMemory Database
- **Giám sát:** ASP.NET Core Health Checks

### Frontend (UI)
- **Framework:** Angular SPA
- **Styling:** Custom CSS, Responsive Layout
- **WebServer (Docker):** Nginx với cấu hình Reverse Proxy & URL Rewrite cho Router Angular

---

## 📋 Yêu cầu Hệ thống (Prerequisites)
- .NET 10.0 SDK trở lên
- Node.js (phiên bản v18 trở lên) & npm
- SQL Server (hoặc chạy qua Docker)
- Docker & Docker Compose (nếu chạy container hóa)

---

## 🚀 Hướng dẫn Chạy Cục bộ (Local Development)

### 1. Cấu hình Cơ sở dữ liệu (Database)
Cập nhật chuỗi kết nối cơ sở dữ liệu SQL Server trong file [appsettings.json](file:///c:/Users/akzan/Desktop/Task%20Management%20System/TaskManagement.API/appsettings.json) nếu cần thiết:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TaskManagementDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Chạy migration để cập nhật cấu trúc bảng và khởi tạo dữ liệu mẫu (Seed):
```bash
# Chạy trong thư mục TaskManagement.API
dotnet ef database update
```

### 2. Chạy Backend (Web API)
Di chuyển vào thư mục dự án API và khởi động server:
```bash
cd TaskManagement.API
dotnet run
```
API sẽ khởi chạy tại địa chỉ mặc định:
- HTTP: `http://localhost:5087`
- Swagger UI (tài liệu tài nguyên endpoint): `http://localhost:5087/swagger`
- Health check (kiểm tra trạng thái hệ thống): `http://localhost:5087/health`

### 3. Chạy Frontend (Angular SPA)
Di chuyển vào thư mục giao diện UI, cài đặt thư viện và khởi động dev server:
```bash
cd TaskManagementUI
npm install
npm start
```
Ứng dụng Frontend sẽ chạy tại địa chỉ: `http://localhost:4200`

---

## 🐳 Khởi chạy bằng Docker Compose (Khuyên dùng)
Dự án đã tích hợp sẵn tệp [docker-compose.yml](file:///c:/Users/akzan/Desktop/Task%20Management%20System/docker-compose.yml) cấu hình đầy đủ SQL Server, API và Frontend.

Để dựng môi trường và khởi chạy toàn bộ hệ thống bằng một dòng lệnh:
```bash
docker compose up --build -d
```

Các cổng kết nối qua container:
- **Frontend SPA:** `http://localhost:8080` (chạy qua Nginx)
- **Backend API:** `http://localhost:5087` (có reverse proxy tương thích CORS)
- **Database Server:** `localhost,1433` (SQL Server)

Dừng container:
```bash
docker compose down
```

---

## 🧪 Kiểm thử Tự động (Automated Testing)
Hệ thống tích hợp đầy đủ bộ Unit Tests và Integration Tests chất lượng cao.

Để thực thi tất cả các bài kiểm tra tự động:
```bash
dotnet test
```

Các bộ kiểm thử bao gồm:
- **Unit Tests:** Kiểm thử chi tiết quyền truy cập [PermissionService](file:///c:/Users/akzan/Desktop/Task%20Management%20System/TaskManagement.Application/Services/PermissionService.cs) (Project Owner, Inactive member, comment deletion, file attachment) và các quy tắc nghiệp vụ trạng thái task.
- **Integration Tests:** Giả lập API Web Host sử dụng InMemory database, kiểm tra toàn bộ luồng Auth (Login/Refresh Token/Logout), CRUD Task phân quyền, tải lên/tải xuống file và ghi nhận nhật ký hệ thống (Audit Logs).

---

## 💾 Công cụ Vận hành & Sao lưu (Backup)
Các kịch bản sao lưu tự động đã được lập trình sẵn bằng PowerShell trong thư mục [scripts/backups](file:///c:/Users/akzan/Desktop/Task%20Management%20System/scripts/backups):

1. **Sao lưu Database SQL Server:**
   ```powershell
   ./scripts/backups/backup-db.ps1
   ```
   *Tự động xuất tệp tin backup `.bak` từ container Docker hoặc instance local của bạn.*

2. **Sao lưu Kho lưu trữ File đính kèm (Attachments Storage):**
   ```powershell
   ./scripts/backups/backup-storage.ps1
   ```
   *Tự động nén và đóng gói toàn bộ thư mục `uploads` thành định dạng lưu trữ dạng `.zip` kèm mốc thời gian.*
