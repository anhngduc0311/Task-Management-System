#!/bin/bash

# --- KIỂM TRA QUYỀN ROOT ---
if [ "$EUID" -ne 0 ]; then
  echo "❌ Vui lòng chạy script này với quyền root (sudo ./deploy.sh)"
  exit 1
fi

echo "================================================================="
echo "   🚀 BẮT ĐẦU TRIỂN KHAI TỰ ĐỘNG TASK MANAGEMENT SYSTEM 🚀   "
echo "================================================================="
echo ""

# --- 1. CÀI ĐẶT DOCKER & DOCKER COMPOSE ---
if ! command -v docker &> /dev/null; then
  echo "🔹 Đang cài đặt Docker..."
  apt-get update
  apt-get install -y ca-certificates curl gnupg
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
    tee /etc/apt/sources.list.d/docker.list > /dev/null

  apt-get update
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  echo "✅ Cài đặt Docker thành công!"
else
  echo "✅ Docker đã được cài đặt sẵn."
fi

# --- 2. CẤU HÌNH API URL CHO FRONTEND (RELATIVE PATH) ---
echo "🔹 Cấu hình API URL cho Angular sang đường dẫn tương đối..."
for file in TaskManagementUI/src/app/core/services/*.ts; do
  if [ -f "$file" ]; then
    sed -i "s|'http://localhost:5035/api|'/api|g" "$file"
  fi
done
echo "✅ Đã cập nhật xong URL API trong các file Angular Service."

# --- 3. CẤU HÌNH REVERSE PROXY CHO NGINX FRONTEND ---
echo "🔹 Cấu hình Nginx Reverse Proxy..."
cat << 'EOF' > TaskManagementUI/nginx.conf
server {
    listen 80;
    server_name localhost;

    location / {
        root /usr/share/nginx/html;
        index index.html index.htm;
        try_files $uri $uri/ /index.html;
    }

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
        
        # Hỗ trợ upload file dung lượng lớn (20MB)
        client_max_body_size 20M;
    }

    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
EOF
echo "✅ Đã thiết lập xong Nginx Reverse Proxy."

# --- 4. KHỞI CHẠY DOCKER COMPOSE ---
echo "🔹 Khởi tạo các container và Build..."
# Thay đổi cổng Frontend mặc định từ 4200 thành 80 trong docker-compose.yml nếu cần
sed -i 's/"4200:80"/"80:80"/g' docker-compose.yml

docker compose up -d --build
echo "✅ Khởi chạy các container Docker thành công!"

# --- 5. CHỜ SQL SERVER KHỞI CHẠY HOÀN TẤT ---
echo "🔹 Đang chờ SQL Server khởi động hoàn tất..."
DB_PASS="sa_Password123!" # Mật khẩu mặc định trong docker-compose.yml
until docker exec -t task-management-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_PASS" -C -Q "SELECT 1" &> /dev/null; do
  echo -n "."
  sleep 2
done
echo ""
echo "✅ SQL Server đã sẵn sàng!"

# --- 6. CHẠY DATABASE MIGRATIONS ---
echo "🔹 Đang chạy database migrations..."
# Lấy tên network thực tế của docker compose
NET_NAME=$(docker network ls --filter name=task-management -q | head -n 1)
if [ -z "$NET_NAME" ]; then
  NET_NAME="task-management-system_default"
else
  NET_NAME=$(docker network inspect "$NET_NAME" -f '{{.Name}}')
fi

docker run --rm \
  --network "$NET_NAME" \
  -v "$(pwd):/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  sh -c "dotnet tool install --global dotnet-ef && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update --project TaskManagement.Infrastructure --startup-project TaskManagement.API --connection 'Server=db,1433;Database=TaskManagementDb;User Id=sa;Password=$DB_PASS;TrustServerCertificate=True;'"

echo "✅ Đã tạo bảng và seed dữ liệu mặc định thành công!"

# --- 7. THIẾT LẬP SCRIPTS BACKUP & CRONJOB ---
echo "🔹 Đang thiết lập script sao lưu tự động..."
mkdir -p backups

# Tạo script backup DB
cat << EOF > backup-db.sh
#!/bin/bash
BACKUP_DIR="$(pwd)/backups"
TIMESTAMP=\$(date +"%Y%m%d_%H%M%S")
docker exec -t task-management-db /opt/mssql-tools18/bin/sqlcmd \\
  -S localhost -U sa -P "$DB_PASS" -C \\
  -Q "BACKUP DATABASE [TaskManagementDb] TO DISK = N'/var/opt/mssql/TaskManagementDb_\$TIMESTAMP.bak' WITH NOFORMAT, NOINIT, NAME = 'TaskManagementDb-Full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
docker cp task-management-db:/var/opt/mssql/TaskManagementDb_\$TIMESTAMP.bak \$BACKUP_DIR/
docker exec -t task-management-db rm /var/opt/mssql/TaskManagementDb_\$TIMESTAMP.bak
find \$BACKUP_DIR -type f -name "*.bak" -mtime +30 -delete
EOF
chmod +x backup-db.sh

# Tạo script backup Storage
cat << 'EOF' > backup-storage.sh
#!/bin/bash
BACKUP_DIR="$(pwd)/backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
VOLUME_PATH="/var/lib/docker/volumes/task-management_file-uploads/_data"
if [ -d "$VOLUME_PATH" ]; then
  tar -czf $BACKUP_DIR/uploads_$TIMESTAMP.tar.gz -C $VOLUME_PATH .
  find $BACKUP_DIR -type f -name "uploads_*.tar.gz" -mtime +30 -delete
fi
EOF
chmod +x backup-storage.sh

# Thêm cronjob (chạy vào 2:00 sáng và 2:30 sáng)
(crontab -l 2>/dev/null | grep -v "backup-db.sh"; echo "0 2 * * * $(pwd)/backup-db.sh >> $(pwd)/backups/backup.log 2>&1") | crontab -
(crontab -l 2>/dev/null | grep -v "backup-storage.sh"; echo "30 2 * * * $(pwd)/backup-storage.sh >> $(pwd)/backups/backup.log 2>&1") | crontab -
echo "✅ Đã đăng ký cronjob sao lưu tự động thành công!"

echo ""
echo "================================================================="
echo "   🎉 HỆ THỐNG ĐÃ ĐƯỢC TRIỂN KHAI VÀ KHỞI CHẠY THÀNH CÔNG 🎉   "
echo "================================================================="
echo "  - Địa chỉ Website: http://13.207.203.108"
echo "  - Cổng API Backend: http://13.207.203.108/api"
echo "  - Kiểm tra trạng thái: docker compose ps"
echo "  - Xem logs: docker compose logs -f"
echo "================================================================="
