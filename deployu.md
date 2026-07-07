# HƯỚNG DẪN TRIỂN KHAI HỆ THỐNG LÊN UBUNTU BẰNG DOCKER

Tài liệu này hướng dẫn chi tiết cách cấu hình và triển khai hệ thống **Task Management System** lên máy chủ Ubuntu sử dụng **Docker** và **Docker Compose**.

---

## 📋 MỤC LỤC
1. [Mô hình triển khai](#1-mô-hình-triển-khai)
2. [Yêu cầu hệ thống (Prerequisites)](#2-yêu-cầu-hệ-thống)
3. [Cấu hình URL API cho Frontend (Quan trọng)](#3-cấu-hình-url-api-cho-frontend)
4. [Cấu hình docker-compose.yml & Biến môi trường](#4-cấu-hình-docker-composeyml--biến-môi-trường)
5. [Các bước triển khai chi tiết trên Ubuntu](#5-các-bước-triển-khai-chi-tiết)
6. [Chạy Database Migration trong Docker](#6-chạy-database-migration-trong-docker)
7. [Vận hành, Giám sát & Sao lưu](#7-vận-hành-giám-sát--sao-lưu)

---

## 1. Mô hình triển khai

Hệ thống được đóng gói thành **3 dịch vụ (services)** chạy độc lập trong mạng nội bộ của Docker:

1. **`db` (SQL Server 2022)**: Hệ quản trị cơ sở dữ liệu nội bộ. Chỉ mở cổng `1433` nội bộ hoặc giới hạn IP ngoài để bảo mật.
2. **`backend` (.NET 10 Web API)**: Chạy trên cổng nội bộ `8080`, xử lý các yêu cầu nghiệp vụ và lưu trữ dữ liệu.
3. **`frontend` (Angular SPA served by Nginx)**: Chạy trên cổng `80` (trong container) và ánh xạ ra ngoài cổng `80` hoặc `4200` của VPS. Nginx sẽ đóng vai trò **Reverse Proxy** chuyển tiếp các request `/api` đến container `backend`.

```text
 Client Browser (Internet)
         |
         | HTTP (Port 80/4200)
         v
 [ Nginx (Frontend Container) ]
   |                      |
   | Lấy giao diện        | Proxy pass sang backend (/api/*)
   v                      v
 [ Angular SPA ]    [ .NET Web API (Backend Container:8080) ]
                          |
                          | Kết nối DB (Port 1433)
                          v
                    [ SQL Server (Database Container:1433) ]
```

---

## 2. Yêu cầu hệ thống

Trước khi bắt đầu, máy chủ Ubuntu của bạn cần cài đặt sẵn Docker và Docker Compose.

### Cài đặt Docker trên Ubuntu (nếu chưa có):
```bash
# Cập nhật danh sách gói
sudo apt-get update

# Cài đặt các gói hỗ trợ HTTPS
sudo apt-get install -y ca-certificates curl gnupg

# Thêm khóa GPG chính thức của Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Thiết lập repository của Docker
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Cài đặt Docker Engine & Docker Compose
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Kiểm tra phiên bản cài đặt thành công
docker --version
docker compose version
```

---

## 3. Cấu hình URL API cho Frontend

Mặc định ở môi trường phát triển (Local Development), các service trong Angular được cấu hình cứng địa chỉ `http://localhost:5035/api`. 

Khi đưa lên VPS Ubuntu, trình duyệt của người dùng sẽ không thể kết nối tới `localhost:5035` của họ. Do đó, ta sẽ chuyển cấu hình sang dạng **Đường dẫn tương đối (Relative URL)** `/api` và sử dụng Nginx làm **Reverse Proxy**.

### Bước 3.1: Đổi URL API trong Frontend Angular
Cập nhật tất cả các địa chỉ API trong thư mục `TaskManagementUI/src/app/core/services/` từ `http://localhost:5035/api` thành `/api`.

*(Ví dụ trong [auth.service.ts](file:///c:/Users/akzan/Desktop/Task%20Management%20System/TaskManagementUI/src/app/core/services/auth.service.ts)):*
```typescript
// Trước: private readonly baseUrl = 'http://localhost:5035/api/auth';
// Sau:
private readonly baseUrl = '/api/auth';
```
Thực hiện tương tự cho tất cả các dịch vụ khác (`task.service.ts`, `project.service.ts`, `comment.service.ts`, `attachment.service.ts`, `report.service.ts`, `audit-log.service.ts`, `user.service.ts`, `dynamic-field.service.ts`).

### Bước 3.2: Cập nhật cấu hình Nginx của Frontend
Cập nhật file cấu hình [nginx.conf](file:///c:/Users/akzan/Desktop/Task%20Management%20System/TaskManagementUI/nginx.conf) của Angular để tự động chuyển tiếp request `/api` đến container `backend`.

```nginx
server {
    listen 80;
    server_name localhost;

    # Giao diện Angular SPA
    location / {
        root /usr/share/nginx/html;
        index index.html index.htm;
        try_files $uri $uri/ /index.html;
    }

    # Reverse Proxy cho API Backend
    location /api {
        proxy_pass http://backend:8080/api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Hỗ trợ Upload file dung lượng lớn (Tối đa 20MB theo spec)
        client_max_body_size 20M;
    }

    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
```

---

## 4. Cấu hình docker-compose.yml & Biến môi trường

Trước khi chạy hệ thống, hãy chỉnh sửa tệp [docker-compose.yml](file:///c:/Users/akzan/Desktop/Task%20Management%20System/docker-compose.yml) ở thư mục gốc để đảm bảo tính bảo mật cho môi trường Production:

1. **Thay đổi mật khẩu SA của SQL Server**: Thay thế chuỗi mật khẩu mặc định bằng một chuỗi bảo mật mạnh.
2. **Cập nhật chuỗi kết nối (ConnectionString) của Backend**: Phải khớp mật khẩu SA đã sửa ở trên.
3. **Thay đổi cổng kết nối của Frontend (nếu cần)**: Hiện tại là cổng `4200` ánh xạ ra ngoài. Nếu muốn chạy trực tiếp trên cổng HTTP mặc định, đổi cổng thành `"80:80"`.

*(Mẫu cấu hình docker-compose.yml đề xuất cho Production):*
```yaml
version: '3.8'

services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: task-management-db
    restart: always
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrongPassword_2026!  # ĐỔI MẬT KHẨU NÀY
    ports:
      - "127.0.0.1:1433:1433" # Chỉ cho phép localhost VPS truy cập trực tiếp cổng này để bảo mật
    volumes:
      - mssql-data:/var/opt/mssql

  backend:
    build:
      context: .
      dockerfile: TaskManagement.API/Dockerfile
    container_name: task-management-api
    restart: always
    depends_on:
      - db
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      # Đổi mật khẩu trong ConnectionString khớp với phía trên
      - ConnectionStrings__DefaultConnection=Server=db,1433;Database=TaskManagementDb;User Id=sa;Password=YourStrongPassword_2026!;TrustServerCertificate=True;
      - FileStorage__UploadPath=/app/Uploads
      - RateLimiting__LoginPermitLimit=5
      - RateLimiting__UploadPermitLimit=5
      - RateLimiting__CommentPermitLimit=10
      - JwtSettings__Issuer=TaskManagementAPI
      - JwtSettings__Audience=TaskManagementUI
      - JwtSettings__Key=YourSuperSecretKeyGoesHere_AtLeast32BytesLong2026! # Khóa bí mật JWT bảo mật
    volumes:
      - file-uploads:/app/Uploads

  frontend:
    build:
      context: ./TaskManagementUI
      dockerfile: Dockerfile
    container_name: task-management-ui
    restart: always
    depends_on:
      - backend
    ports:
      - "80:80" # Chạy trực tiếp qua cổng 80 (HTTP) của VPS

volumes:
  mssql-data:
    driver: local
  file-uploads:
    driver: local
```

---

## 5. Các bước triển khai chi tiết

Thực hiện các bước sau trên Terminal của VPS Ubuntu:

### Bước 5.1: Đồng bộ mã nguồn lên VPS
Bạn có thể sử dụng Git hoặc lệnh `scp`/`rsync` để tải toàn bộ thư mục dự án lên Ubuntu:
```bash
# SSH vào VPS Ubuntu
ssh username@your-vps-ip

# Di chuyển đến thư mục làm việc mong muốn
cd /var/www
# clone dự án từ Repository của bạn
git clone <your-repo-url> task-management
cd task-management
```

### Bước 5.2: Thực hiện build và start các container
Chạy Docker Compose để dựng môi trường tự động:
```bash
# Build images và khởi chạy ở chế độ chạy ngầm (detached mode)
docker compose up -d --build
```
Quá trình build sẽ mất từ 3-7 phút tùy vào tốc độ mạng và CPU của VPS (Docker sẽ tiến hành tải SDK, restore package, build Angular và .NET).

### Bước 5.3: Kiểm tra trạng thái triển khai
```bash
# Kiểm tra danh sách container đang chạy
docker compose ps

# Xem log của hệ thống để phát hiện lỗi (nếu có)
docker compose logs -f
```
If cả 3 container (`task-management-db`, `task-management-api`, `task-management-ui`) hiển thị trạng thái `Up` (Running), hệ thống đã khởi chạy thành công.

---

## 6. Chạy Database Migration trong Docker

Vì cơ sở dữ liệu SQL Server lúc đầu mới khởi tạo sẽ trống rỗng, bạn cần chạy Entity Framework Core Migration để tạo cấu trúc bảng và ghi dữ liệu seed mặc định.

Do container `backend` chỉ chứa runtime .NET và không có .NET SDK hay EF Core CLI, chúng ta sẽ chạy lệnh Migration thông qua một container SDK tạm thời kết nối cùng mạng Docker:

### Thực thi lệnh Migration:
```bash
docker run --rm \
  --network task-management_default \
  -v "$(pwd):/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  sh -c "dotnet tool install --global dotnet-ef && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update --project TaskManagement.Infrastructure --startup-project TaskManagement.API --connection 'Server=db,1433;Database=TaskManagementDb;User Id=sa;Password=YourStrongPassword_2026!;TrustServerCertificate=True;'"
```

> [!NOTE]
> - Cần đảm bảo tham số mạng `--network` trùng khớp với tên mạng được sinh ra bởi Docker Compose (kiểm tra bằng lệnh `docker network ls`, thường có định dạng `<tên_thư_mục_gốc>_default`).
> - Chuỗi kết nối trong lệnh trên phải trỏ tới host `db` (tên dịch vụ cơ sở dữ liệu trong docker-compose) và trùng khớp mật khẩu SA đã cấu hình.

---

## 7. Vận hành, Giám sát & Sao lưu

### 7.1. Xem Logs
```bash
# Xem log của API Backend
docker compose logs -f backend

# Xem log của Nginx Frontend
docker compose logs -f frontend
```

### 7.2. Sao lưu dữ liệu (Backup)
Để tránh mất mát dữ liệu, cần thiết lập tác vụ sao lưu định kỳ cho database và file đính kèm.

#### 1. Script sao lưu Database (`backup-db.sh`):
Tạo file script `/var/www/task-management/backup-db.sh` trên Ubuntu:
```bash
#!/bin/bash
BACKUP_DIR="/var/www/task-management/backups"
mkdir -p $BACKUP_DIR
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Thực hiện lệnh backup SQL Server ngay trong container db
docker exec -t task-management-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrongPassword_2026!" -C \
  -Q "BACKUP DATABASE [TaskManagementDb] TO DISK = N'/var/opt/mssql/TaskManagementDb_$TIMESTAMP.bak' WITH NOFORMAT, NOINIT, NAME = 'TaskManagementDb-Full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

# Di chuyển tệp .bak từ Volume docker ra ngoài VPS
docker cp task-management-db:/var/opt/mssql/TaskManagementDb_$TIMESTAMP.bak $BACKUP_DIR/

# Xóa tệp backup tạm thời bên trong container
docker exec -t task-management-db rm /var/opt/mssql/TaskManagementDb_$TIMESTAMP.bak

# Xóa các bản sao lưu cũ hơn 30 ngày để tiết kiệm dung lượng
find $BACKUP_DIR -type f -name "*.bak" -mtime +30 -delete

echo "Database backup completed: $BACKUP_DIR/TaskManagementDb_$TIMESTAMP.bak"
```

#### 2. Script sao lưu tệp tải lên (`backup-storage.sh`):
```bash
#!/bin/bash
BACKUP_DIR="/var/www/task-management/backups"
mkdir -p $BACKUP_DIR
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Đường dẫn volume chứa tệp tải lên thực tế trên host Ubuntu
VOLUME_PATH="/var/lib/docker/volumes/task-management_file-uploads/_data"

# Nén thư mục uploads thành file zip
tar -czf $BACKUP_DIR/uploads_$TIMESTAMP.tar.gz -C $VOLUME_PATH .

# Xóa bản sao lưu cũ hơn 30 ngày
find $BACKUP_DIR -type f -name "uploads_*.tar.gz" -mtime +30 -delete

echo "Storage backup completed: $BACKUP_DIR/uploads_$TIMESTAMP.tar.gz"
```

#### 3. Lên lịch tự động bằng Cronjob:
Mở trình quản lý cronjob của Ubuntu:
```bash
sudo crontab -e
```
Thêm hai dòng sau vào cuối file để tự động chạy sao lưu vào lúc **2 giờ sáng** hàng ngày:
```text
0 2 * * * /bin/bash /var/www/task-management/backup-db.sh >> /var/log/cron-backup.log 2>&1
30 2 * * * /bin/bash /var/www/task-management/backup-storage.sh >> /var/log/cron-backup.log 2>&1
```

---
*Chúc bạn triển khai dự án thành công lên máy chủ Ubuntu!*
