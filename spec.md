# SPEC — Task Management System
**Phiên bản:** 1.0 | **Ngày:** 2026-07-07 | **Trạng thái:** Sẵn sàng thực thi

---

## 📋 MỤC LỤC

1. [IDEA — Ý tưởng và bối cảnh](#1-idea)
2. [REQUIREMENTS — Yêu cầu hệ thống](#2-requirements)
3. [DESIGN — Thiết kế chi tiết](#3-design)
4. [TASKS — Danh sách task thực thi](#4-tasks)

---

## 1. IDEA

### 1.1. Bối cảnh vấn đề

Các nhóm làm việc nhỏ và vừa đang phụ thuộc vào Excel, bảng tính, tin nhắn chat hoặc email để phối hợp công việc. Điều này gây ra:

- **Mất kiểm soát**: Không biết ai đang làm gì, task nào bị trễ.
- **Thiếu minh bạch**: Không có lịch sử thay đổi, không rõ ai quyết định gì.
- **Phân quyền kém**: Mọi người đều nhìn thấy và sửa mọi thứ.
- **Không thể mở rộng**: Khi team lớn hơn, Excel không còn dùng được.

### 1.2. Giải pháp

Xây dựng một **hệ thống quản lý công việc theo nhóm (Team Task Management System)** dạng web app, cung cấp:

- Không gian làm việc riêng theo **Project**.
- Tạo và phân công **Task** cho từng thành viên.
- Theo dõi **trạng thái, ưu tiên, deadline** của từng task.
- **Bình luận** và **đính kèm file** ngay trong task.
- **Phân quyền theo vai trò** (Admin / Project Manager / Member / Guest).
- **Lịch sử thay đổi** có thể truy vết (Audit Log).

### 1.3. Định vị sản phẩm

| Tiêu chí | Định vị |
|---|---|
| **Đối tượng** | Nhóm nội bộ 5–50 người |
| **Phong cách** | Jira-lite: đủ tính năng nhưng không quá phức tạp |
| **Giai đoạn** | MVP — ra sản phẩm dùng được, không nhồi tính năng |
| **Kiến trúc** | Modular Monolith, mở rộng được sau MVP |
| **Ưu tiên** | Dễ dùng → Ổn định → Bảo mật → Truy vết → Mở rộng |

### 1.4. Những gì KHÔNG có trong MVP

> [!IMPORTANT]
> Các tính năng sau **bị khóa hoàn toàn** khỏi phạm vi MVP. Bất kỳ yêu cầu thêm tính năng này đều phải có quyết định chính thức.

- Biểu đồ Gantt
- AI gợi ý / tự động hóa
- Time tracking phức tạp
- Real-time bằng SignalR (dùng polling/manual refresh trong MVP)
- Mobile native app
- Microservices
- Search engine riêng (Elasticsearch)
- Multi-tenant SaaS

---

## 2. REQUIREMENTS

### 2.1. Functional Requirements (FR)

#### FR-01: Xác thực người dùng
- **FR-01.1** Người dùng có thể đăng nhập bằng Email + Password.
- **FR-01.2** Backend xác thực thông tin và trả về JWT token.
- **FR-01.3** Frontend lưu token theo cơ chế an toàn (không lưu trong localStorage nếu có thể).
- **FR-01.4** Token hết hạn phải được xử lý: refresh token hoặc yêu cầu đăng nhập lại.
- **FR-01.5** Đăng xuất phải vô hiệu hoá session/token phía server.

#### FR-02: Quản lý người dùng
- **FR-02.1** Admin có thể xem danh sách người dùng.
- **FR-02.2** Admin có thể tạo, vô hiệu hóa tài khoản người dùng.
- **FR-02.3** Admin có thể gán vai trò hệ thống cho người dùng.
- **FR-02.4** Người dùng có thể cập nhật thông tin cá nhân (tên, avatar).
- **FR-02.5** Người dùng có thể đổi mật khẩu.

#### FR-03: Quản lý Project
- **FR-03.1** Admin / Project Manager có thể tạo project mới.
- **FR-03.2** Project có: Tên, Mô tả, Chủ sở hữu (Owner), Trạng thái, Ngày tạo.
- **FR-03.3** Admin / PM có thể sửa thông tin project.
- **FR-03.4** Admin có thể xóa project (xóa mềm).
- **FR-03.5** Người dùng chỉ thấy project mà họ là thành viên.

#### FR-04: Quản lý thành viên Project
- **FR-04.1** PM/Admin có thể thêm thành viên vào project.
- **FR-04.2** PM/Admin có thể gán vai trò trong project cho từng thành viên (PM / Member / Guest).
- **FR-04.3** PM/Admin có thể xóa thành viên khỏi project.
- **FR-04.4** Một user có thể có vai trò khác nhau ở các project khác nhau.

#### FR-05: Quản lý Task
- **FR-05.1** PM/Member có thể tạo task trong project.
- **FR-05.2** Task có: Tiêu đề, Mô tả, Trạng thái, Ưu tiên, Assignee, DueDate, Người tạo.
- **FR-05.3** Task có các trạng thái: `Todo → In Progress → In Review → Done → Cancelled`.
- **FR-05.4** Task có các mức ưu tiên: `Low / Medium / High / Critical`.
- **FR-05.5** PM/Admin có thể giao task cho thành viên trong project.
- **FR-05.6** Người được giao có thể tự đổi trạng thái task của mình.
- **FR-05.7** PM/Admin có thể sửa mọi thuộc tính của task.
- **FR-05.8** Member chỉ được sửa task của mình (giới hạn trường: description, status).
- **FR-05.9** PM/Admin có thể xóa task (xóa mềm).
- **FR-05.10** Task hỗ trợ **optimistic concurrency**: không được ghi đè ngầm khi có xung đột.

#### FR-06: Bình luận Task
- **FR-06.1** Member / PM / Admin có thể thêm bình luận vào task.
- **FR-06.2** Người dùng chỉ sửa/xóa bình luận của chính họ.
- **FR-06.3** Admin có thể xóa bất kỳ bình luận nào.
- **FR-06.4** Nội dung bình luận phải được sanitize trước khi lưu/hiển thị nếu hỗ trợ rich text.

#### FR-07: Đính kèm File
- **FR-07.1** Member / PM / Admin có thể upload file vào task.
- **FR-07.2** Tải file phải đi qua API backend với kiểm tra quyền.
- **FR-07.3** Frontend kiểm tra sơ bộ loại file và kích thước trước khi gửi.
- **FR-07.4** Backend kiểm tra lại loại file và kích thước.
- **FR-07.5** File thật được lưu trên storage bên ngoài (Azure Blob / AWS S3 / server folder).
- **FR-07.6** Database chỉ lưu metadata (tên, loại, kích thước, storage key, người upload).
- **FR-07.7** PM/Admin hoặc người upload có thể xóa file.

#### FR-08: Tìm kiếm & Lọc Task
- **FR-08.1** Người dùng có thể lọc task theo: Project, Assignee, Status, Priority, DueDate.
- **FR-08.2** Người dùng có thể tìm kiếm task theo tiêu đề (LIKE search cơ bản).
- **FR-08.3** Kết quả phải được phân trang.

#### FR-09: Dashboard / Danh sách Task
- **FR-09.1** Hiển thị danh sách task theo project.
- **FR-09.2** Hiển thị task được giao cho "tôi" (My Tasks).
- **FR-09.3** Hiển thị số lượng task theo trạng thái (thống kê cơ bản).

#### FR-10: Audit Log
- **FR-10.1** Hệ thống ghi log tự động cho các hành động: tạo task, sửa task, đổi trạng thái, đổi assignee, đổi deadline, xóa task, thêm/xóa thành viên project, thay đổi quyền.
- **FR-10.2** PM/Admin có thể xem audit log của project.
- **FR-10.3** Mỗi log entry ghi: EntityType, EntityId, Action, ChangedBy, ChangedAt, OldValue, NewValue.

---

### 2.2. Non-Functional Requirements (NFR)

#### NFR-01: Bảo mật
- Mật khẩu phải được hash (bcrypt hoặc tương đương), **không lưu plain text**.
- Mọi endpoint quan trọng phải xác thực JWT.
- Backend phải kiểm tra quyền theo tài nguyên (resource-based), không chỉ theo role.
- Rate limiting cho: Login, Upload file, Comment.
- Input validation ở cả frontend (UX) và backend (bắt buộc).
- Không expose dữ liệu nhạy cảm trong response không cần thiết.

#### NFR-02: Hiệu năng
- Danh sách task phải có **pagination** (mặc định 20–50 items/trang).
- Query sử dụng projection DTO, không `Include` tràn lan.
- Index trên các cột thường dùng để lọc: `ProjectId`, `AssigneeId`, `Status`, `DueDate`.
- Không load comment/attachment đầy đủ trong list view.
- Theo dõi slow query ở môi trường staging/production.

#### NFR-03: Độ tin cậy
- Backup database định kỳ.
- Backup file storage.
- Có quy trình restore và test restore định kỳ.
- Không migrate production tự động thiếu kiểm soát.

#### NFR-04: Vận hành
- Logging lỗi backend.
- Health check endpoint (`GET /health`).
- Môi trường tách biệt: Development / Staging / Production.
- CI/CD cơ bản: build, test, lint, deploy staging, deploy production sau duyệt.
- Containerize bằng Docker.

#### NFR-05: Giới hạn dữ liệu (cần chốt trước production)
| Trường | Giới hạn đề xuất |
|---|---|
| Task Title | ≤ 200 ký tự |
| Description | ≤ 5.000 ký tự (cấu hình) |
| Comment | ≤ 2.000 ký tự (cấu hình) |
| File upload | ≤ 20 MB/file (cấu hình) |
| Attachment/task | ≤ 10 files (cấu hình) |

---

## 3. DESIGN

### 3.1. Kiến trúc tổng thể

```text
┌─────────────────────────────────────────────┐
│               User Browser                   │
└─────────────────┬───────────────────────────┘
                  │ HTTPS
┌─────────────────▼───────────────────────────┐
│            Angular SPA                       │
│  Components / Pages / Route Guards / State   │
└─────────────────┬───────────────────────────┘
                  │ REST API (HTTPS/JSON)
┌─────────────────▼───────────────────────────┐
│           .NET Web API                        │
│  ┌──────────────────────────────────────┐   │
│  │  API Layer (Controllers + DTOs)      │   │
│  ├──────────────────────────────────────┤   │
│  │  Application Layer (Use Cases)       │   │
│  ├──────────────────────────────────────┤   │
│  │  Domain Layer (Entities + Rules)     │   │
│  ├──────────────────────────────────────┤   │
│  │  Infrastructure Layer (EF Core)      │   │
│  └──────────────────────────────────────┘   │
└──────────┬──────────────────────┬───────────┘
           │                      │
┌──────────▼──────────┐  ┌───────▼───────────┐
│  SQL Server/AzureSQL│  │  File Storage      │
│  (Data + Audit Log) │  │  (Blob/S3/Folder)  │
└─────────────────────┘  └───────────────────┘
```

### 3.2. Tech Stack

| Layer | Công nghệ |
|---|---|
| **Frontend** | Angular 17+, RxJS, Angular Material hoặc PrimeNG |
| **Backend** | .NET 10, ASP.NET Core Web API |
| **ORM** | Entity Framework Core 10 |
| **Database** | SQL Server / Azure SQL |
| **Auth** | JWT Bearer Token |
| **File Storage** | Azure Blob Storage / AWS S3 / Local folder |
| **Container** | Docker + Docker Compose |
| **CI/CD** | GitHub Actions / Azure DevOps |

### 3.3. Database Schema

#### Bảng `Users`
```sql
Users (
  Id             UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  FullName       NVARCHAR(200)     NOT NULL,
  Email          NVARCHAR(256)     NOT NULL UNIQUE,
  PasswordHash   NVARCHAR(512)     NULL,         -- NULL nếu dùng OAuth
  ExternalAuthId NVARCHAR(256)     NULL,
  Status         NVARCHAR(20)      NOT NULL DEFAULT 'Active',  -- Active | Inactive
  AvatarUrl      NVARCHAR(512)     NULL,
  CreatedAt      DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt      DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `Roles`
```sql
Roles (
  Id     INT           PRIMARY KEY IDENTITY,
  Name   NVARCHAR(50)  NOT NULL UNIQUE  -- Admin | ProjectManager | Member | Guest
)
```

#### Bảng `UserRoles`
```sql
UserRoles (
  UserId  UNIQUEIDENTIFIER  FK → Users.Id,
  RoleId  INT               FK → Roles.Id,
  PRIMARY KEY (UserId, RoleId)
)
```

#### Bảng `Projects`
```sql
Projects (
  Id          UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  Name        NVARCHAR(200)     NOT NULL,
  Description NVARCHAR(2000)    NULL,
  OwnerId     UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  Status      NVARCHAR(20)      NOT NULL DEFAULT 'Active',  -- Active | Archived | Deleted
  CreatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `ProjectMembers`
```sql
ProjectMembers (
  ProjectId     UNIQUEIDENTIFIER  FK → Projects.Id,
  UserId        UNIQUEIDENTIFIER  FK → Users.Id,
  RoleInProject NVARCHAR(50)      NOT NULL,  -- ProjectManager | Member | Guest
  JoinedAt      DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  Status        NVARCHAR(20)      NOT NULL DEFAULT 'Active',
  PRIMARY KEY (ProjectId, UserId)
)
```

#### Bảng `Tasks`
```sql
Tasks (
  Id          UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProjectId   UNIQUEIDENTIFIER  NOT NULL FK → Projects.Id,
  Title       NVARCHAR(200)     NOT NULL,
  Description NVARCHAR(5000)    NULL,
  Status      NVARCHAR(20)      NOT NULL DEFAULT 'Todo',
              -- Todo | InProgress | InReview | Done | Cancelled
  Priority    NVARCHAR(20)      NOT NULL DEFAULT 'Medium',
              -- Low | Medium | High | Critical
  AssigneeId  UNIQUEIDENTIFIER  NULL FK → Users.Id,
  CreatedById UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  DueDate     DATE              NULL,
  IsDeleted   BIT               NOT NULL DEFAULT 0,
  CreatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  RowVersion  ROWVERSION        NOT NULL  -- Optimistic concurrency
)

-- Indexes
CREATE INDEX IX_Tasks_ProjectId ON Tasks(ProjectId) WHERE IsDeleted = 0;
CREATE INDEX IX_Tasks_AssigneeId ON Tasks(AssigneeId) WHERE IsDeleted = 0;
CREATE INDEX IX_Tasks_Status ON Tasks(ProjectId, Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Tasks_DueDate ON Tasks(DueDate) WHERE IsDeleted = 0;
```

#### Bảng `TaskComments`
```sql
TaskComments (
  Id        UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  TaskId    UNIQUEIDENTIFIER  NOT NULL FK → Tasks.Id,
  UserId    UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  Content   NVARCHAR(2000)    NOT NULL,
  IsDeleted BIT               NOT NULL DEFAULT 0,
  CreatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
CREATE INDEX IX_TaskComments_TaskId ON TaskComments(TaskId) WHERE IsDeleted = 0;
```

#### Bảng `TaskAttachments`
```sql
TaskAttachments (
  Id           UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  TaskId       UNIQUEIDENTIFIER  NOT NULL FK → Tasks.Id,
  UploadedById UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  FileName     NVARCHAR(260)     NOT NULL,
  StorageKey   NVARCHAR(512)     NOT NULL,  -- Path hoặc blob name trong storage
  ContentType  NVARCHAR(100)     NOT NULL,
  FileSize     BIGINT            NOT NULL,
  IsDeleted    BIT               NOT NULL DEFAULT 0,
  CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `AuditLogs`
```sql
AuditLogs (
  Id          BIGINT            PRIMARY KEY IDENTITY,
  EntityType  NVARCHAR(100)     NOT NULL,  -- Task | Project | ProjectMember | ...
  EntityId    NVARCHAR(100)     NOT NULL,
  Action      NVARCHAR(100)     NOT NULL,  -- Created | Updated | Deleted | StatusChanged | ...
  ChangedById UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  ChangedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  OldValue    NVARCHAR(MAX)     NULL,      -- JSON
  NewValue    NVARCHAR(MAX)     NULL,      -- JSON
  IpAddress   NVARCHAR(50)      NULL,
  UserAgent   NVARCHAR(500)     NULL
)
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_ChangedAt ON AuditLogs(ChangedAt DESC);
```

---

### 3.4. API Endpoints

#### Authentication
```
POST   /api/auth/login          → Đăng nhập, trả JWT
POST   /api/auth/logout         → Đăng xuất
POST   /api/auth/refresh-token  → Làm mới token
```

#### Users
```
GET    /api/users               → [Admin] Danh sách users (paginated)
GET    /api/users/{id}          → Chi tiết user
PUT    /api/users/{id}          → Cập nhật thông tin cá nhân
POST   /api/users/{id}/change-password → Đổi mật khẩu
PUT    /api/users/{id}/status   → [Admin] Vô hiệu hoá / kích hoạt
```

#### Projects
```
GET    /api/projects            → Danh sách project của tôi (paginated)
POST   /api/projects            → Tạo project mới
GET    /api/projects/{id}       → Chi tiết project
PUT    /api/projects/{id}       → Sửa project
DELETE /api/projects/{id}       → Xóa mềm project (Admin)
```

#### Project Members
```
GET    /api/projects/{id}/members         → Danh sách thành viên
POST   /api/projects/{id}/members         → Thêm thành viên
PUT    /api/projects/{id}/members/{uid}   → Đổi vai trò thành viên
DELETE /api/projects/{id}/members/{uid}   → Xóa thành viên
```

#### Tasks
```
GET    /api/projects/{pid}/tasks          → Danh sách task (filter + paginate)
POST   /api/projects/{pid}/tasks          → Tạo task mới
GET    /api/tasks/{id}                    → Chi tiết task
PUT    /api/tasks/{id}                    → Sửa task (kèm RowVersion)
PATCH  /api/tasks/{id}/status             → Đổi trạng thái
PATCH  /api/tasks/{id}/assignee           → Đổi assignee
DELETE /api/tasks/{id}                    → Xóa mềm task

GET    /api/tasks/my-tasks                → Task được giao cho tôi
```

#### Comments
```
GET    /api/tasks/{tid}/comments          → Danh sách comment
POST   /api/tasks/{tid}/comments          → Thêm comment
PUT    /api/tasks/{tid}/comments/{cid}    → Sửa comment
DELETE /api/tasks/{tid}/comments/{cid}    → Xóa mềm comment
```

#### Attachments
```
GET    /api/tasks/{tid}/attachments       → Danh sách attachment
POST   /api/tasks/{tid}/attachments       → Upload file
GET    /api/attachments/{id}/download     → Tải file (có kiểm tra quyền)
DELETE /api/attachments/{id}              → Xóa attachment
```

#### Audit Logs
```
GET    /api/projects/{pid}/audit-logs     → [PM/Admin] Log của project
GET    /api/tasks/{tid}/audit-logs        → [PM/Admin] Log của task
```

#### System
```
GET    /health                            → Health check
```

---

### 3.5. Cấu trúc dự án Backend (.NET)

```
TaskManagement.sln
│
├── TaskManagement.API/              # Presentation Layer
│   ├── Controllers/
│   ├── Middleware/
│   ├── Filters/
│   └── Program.cs
│
├── TaskManagement.Application/      # Application Layer
│   ├── UseCases/
│   │   ├── Tasks/
│   │   │   ├── CreateTask/
│   │   │   ├── UpdateTask/
│   │   │   ├── DeleteTask/
│   │   │   └── UpdateTaskStatus/
│   │   ├── Projects/
│   │   ├── Comments/
│   │   └── Attachments/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│       ├── PermissionService.cs
│       └── AuditService.cs
│
├── TaskManagement.Domain/           # Domain Layer
│   ├── Entities/
│   ├── Enums/
│   └── Rules/
│
└── TaskManagement.Infrastructure/   # Infrastructure Layer
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   ├── Migrations/
    │   └── Repositories/
    ├── Storage/
    │   └── FileStorageService.cs
    └── Identity/
        └── JwtTokenService.cs
```

### 3.6. Cấu trúc dự án Frontend (Angular)

```
src/
├── app/
│   ├── core/
│   │   ├── auth/         (guards, interceptors, auth service)
│   │   └── services/     (api service, error handler)
│   ├── shared/
│   │   └── components/   (button, modal, paginator, avatar, ...)
│   ├── features/
│   │   ├── auth/         (login page)
│   │   ├── dashboard/    (my tasks, overview)
│   │   ├── projects/     (list, create, detail, members)
│   │   └── tasks/        (list, create, detail, comment, attachment)
│   └── app.routes.ts
```

### 3.7. Permission Matrix (Chi tiết)

| Hành động | Admin | PM (dự án) | Member (dự án) | Guest (dự án) | Người không thuộc dự án |
|---|:---:|:---:|:---:|:---:|:---:|
| Xem project | ✅ | ✅ | ✅ | ✅ | ❌ |
| Tạo project | ✅ | ✅* | ❌ | ❌ | ❌ |
| Sửa project | ✅ | ✅ | ❌ | ❌ | ❌ |
| Xóa project | ✅ | ❌ | ❌ | ❌ | ❌ |
| Quản lý thành viên | ✅ | ✅ | ❌ | ❌ | ❌ |
| Xem task | ✅ | ✅ | ✅ | ✅ | ❌ |
| Tạo task | ✅ | ✅ | ✅ | ❌ | ❌ |
| Sửa task (toàn bộ) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Sửa task của mình | ✅ | ✅ | ✅ | ❌ | ❌ |
| Đổi status task | ✅ | ✅ | ✅ (task mình) | ❌ | ❌ |
| Xóa task | ✅ | ✅ | ❌ | ❌ | ❌ |
| Thêm comment | ✅ | ✅ | ✅ | ✅ | ❌ |
| Xóa comment của mình | ✅ | ✅ | ✅ | ✅ | ❌ |
| Xóa comment người khác | ✅ | ✅ | ❌ | ❌ | ❌ |
| Upload file | ✅ | ✅ | ✅ | ❌ | ❌ |
| Tải file | ✅ | ✅ | ✅ | ✅ | ❌ |
| Xem audit log | ✅ | ✅ | ❌ | ❌ | ❌ |

*PM có thể tạo project nếu có quyền hệ thống tương ứng.

---

## 4. TASKS

> [!NOTE]
> Các task được phân theo module và có thể thực thi **song song** hoặc **tuần tự** tùy theo resource. Mỗi task con được ước tính **độ phức tạp** (S/M/L) và **độ ưu tiên** (P1/P2/P3).

---

### PHASE 0 — Setup & Foundation

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T0.1 | Khởi tạo solution .NET Web API (Clean Architecture template) | S | P1 | |
| T0.2 | Khởi tạo project Angular với Angular CLI | S | P1 | |
| T0.3 | Cấu hình Docker Compose cho Backend + DB local | M | P1 | |
| T0.4 | Cấu hình appsettings cho 3 môi trường (Dev/Staging/Prod) | S | P1 | |
| T0.5 | Cài đặt EF Core, cấu hình DbContext, kết nối SQL Server | S | P1 | |
| T0.6 | Setup CI/CD pipeline cơ bản (build + test) | M | P2 | |
| T0.7 | Setup Serilog hoặc NLog để logging | S | P2 | |

---

### PHASE 1 — Domain & Database

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T1.1 | Tạo Entity `User` với đầy đủ properties | S | P1 | |
| T1.2 | Tạo Entity `Role` và `UserRole` (RBAC) | S | P1 | |
| T1.3 | Tạo Entity `Project` | S | P1 | |
| T1.4 | Tạo Entity `ProjectMember` với role trong project | S | P1 | |
| T1.5 | Tạo Entity `Task` với concurrency token (RowVersion) | M | P1 | |
| T1.6 | Tạo Entity `TaskComment` với soft delete | S | P1 | |
| T1.7 | Tạo Entity `TaskAttachment` (metadata only) | S | P1 | |
| T1.8 | Tạo Entity `AuditLog` | S | P1 | |
| T1.9 | Viết EF Core migrations cho toàn bộ schema | M | P1 | |
| T1.10 | Tạo indexes trên Tasks (ProjectId, AssigneeId, Status, DueDate) | S | P1 | |
| T1.11 | Seed data: Roles mặc định (Admin, ProjectManager, Member, Guest) | S | P1 | |
| T1.12 | Seed data: Admin user mặc định | S | P1 | |
| T1.13 | Viết unit test cho Domain entities (business rules) | M | P2 | |

---

### PHASE 2 — Authentication & Authorization

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T2.1 | Implement đăng nhập: validate email/password, trả JWT | M | P1 | |
| T2.2 | Implement JWT token generation (access token + refresh token) | M | P1 | |
| T2.3 | Implement middleware xác thực JWT trên mọi endpoint | S | P1 | |
| T2.4 | Implement `PermissionService`: kiểm tra quyền theo resource | L | P1 | Quan trọng nhất |
| T2.5 | Viết `[RequireProjectMembership]` attribute cho controller | M | P1 | |
| T2.6 | Implement đổi mật khẩu | S | P2 | |
| T2.7 | Implement đăng xuất (revoke refresh token) | S | P2 | |
| T2.8 | Viết integration test cho login flow | M | P2 | |
| T2.9 | Viết test cho permission service (boundary cases) | M | P1 | Viết test trước khi dùng |

---

### PHASE 3 — Core API (Backend)

#### 3A — User Management API
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3A.1 | `GET /api/users` — Danh sách users (Admin only, paginated) | S | P2 |
| T3A.2 | `GET /api/users/{id}` — Chi tiết user | S | P2 |
| T3A.3 | `PUT /api/users/{id}` — Cập nhật thông tin cá nhân | S | P2 |
| T3A.4 | `PUT /api/users/{id}/status` — Vô hiệu hoá / kích hoạt (Admin) | S | P2 |

#### 3B — Project API
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3B.1 | `GET /api/projects` — Danh sách project của tôi | S | P1 |
| T3B.2 | `POST /api/projects` — Tạo project mới | S | P1 |
| T3B.3 | `GET /api/projects/{id}` — Chi tiết project (kiểm tra membership) | S | P1 |
| T3B.4 | `PUT /api/projects/{id}` — Sửa project | S | P1 |
| T3B.5 | `DELETE /api/projects/{id}` — Xóa mềm project (Admin) | S | P2 |

#### 3C — Project Member API
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3C.1 | `GET /api/projects/{id}/members` — Danh sách thành viên | S | P1 |
| T3C.2 | `POST /api/projects/{id}/members` — Thêm thành viên | M | P1 |
| T3C.3 | `PUT /api/projects/{id}/members/{uid}` — Đổi vai trò | S | P1 |
| T3C.4 | `DELETE /api/projects/{id}/members/{uid}` — Xóa thành viên | S | P1 |

#### 3D — Task API
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3D.1 | `GET /api/projects/{pid}/tasks` — Danh sách task (filter + paginate) | M | P1 |
| T3D.2 | `POST /api/projects/{pid}/tasks` — Tạo task (validate assignee thuộc project) | M | P1 |
| T3D.3 | `GET /api/tasks/{id}` — Chi tiết task | S | P1 |
| T3D.4 | `PUT /api/tasks/{id}` — Sửa task kèm RowVersion (optimistic concurrency) | L | P1 |
| T3D.5 | `PATCH /api/tasks/{id}/status` — Đổi trạng thái | M | P1 |
| T3D.6 | `PATCH /api/tasks/{id}/assignee` — Đổi assignee | M | P1 |
| T3D.7 | `DELETE /api/tasks/{id}` — Xóa mềm task | S | P1 |
| T3D.8 | `GET /api/tasks/my-tasks` — Task giao cho tôi | S | P2 |

#### 3E — Comment API
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3E.1 | `GET /api/tasks/{tid}/comments` — Danh sách comment | S | P1 |
| T3E.2 | `POST /api/tasks/{tid}/comments` — Thêm comment (sanitize content) | M | P1 |
| T3E.3 | `PUT /api/tasks/{tid}/comments/{cid}` — Sửa comment (chỉ của mình) | S | P2 |
| T3E.4 | `DELETE /api/tasks/{tid}/comments/{cid}` — Xóa comment | S | P2 |

#### 3F — Attachment API
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3F.1 | `POST /api/tasks/{tid}/attachments` — Upload file (kiểm tra loại, kích thước) | L | P1 |
| T3F.2 | `GET /api/tasks/{tid}/attachments` — Danh sách attachments | S | P1 |
| T3F.3 | `GET /api/attachments/{id}/download` — Tải file (kiểm tra quyền, tạo stream/URL) | L | P1 |
| T3F.4 | `DELETE /api/attachments/{id}` — Xóa attachment (xóa DB + storage) | M | P2 |
| T3F.5 | Implement `FileStorageService` (interface + local/blob implementation) | M | P1 | |

#### 3G — Audit Log
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T3G.1 | Implement `AuditService` trung tâm (không để controller tự ghi) | M | P1 |
| T3G.2 | Tích hợp audit vào: CreateTask, UpdateTask, DeleteTask | M | P1 |
| T3G.3 | Tích hợp audit vào: status change, assignee change, deadline change | M | P1 |
| T3G.4 | Tích hợp audit vào: add/remove project member | S | P1 |
| T3G.5 | `GET /api/projects/{pid}/audit-logs` — Xem log project (PM/Admin) | S | P2 |
| T3G.6 | `GET /api/tasks/{tid}/audit-logs` — Xem log task | S | P2 |

---

### PHASE 4 — Frontend (Angular)

#### 4A — Core & Auth
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T4A.1 | Setup Angular routing với lazy loading | S | P1 |
| T4A.2 | Implement `AuthService` (login, logout, token storage) | M | P1 |
| T4A.3 | Implement `AuthGuard` và `RoleGuard` | M | P1 |
| T4A.4 | Implement `AuthInterceptor` gắn JWT vào mọi request | S | P1 |
| T4A.5 | Implement `ErrorInterceptor` xử lý 401/403/409/500 | M | P1 |
| T4A.6 | Trang Login UI | S | P1 |

#### 4B — Layout & Shared
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T4B.1 | Layout chính: Sidebar + Header + Content area | M | P1 |
| T4B.2 | Component: `PaginatorComponent` | S | P1 |
| T4B.3 | Component: `ConfirmDialogComponent` | S | P1 |
| T4B.4 | Component: `LoadingSpinnerComponent` | S | P1 |
| T4B.5 | Component: `AvatarComponent` | S | P2 |
| T4B.6 | Toast notification service | S | P2 |

#### 4C — Project Feature
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T4C.1 | Trang danh sách Projects | M | P1 |
| T4C.2 | Form tạo / sửa Project | S | P1 |
| T4C.3 | Trang chi tiết Project (header info + tabs) | M | P1 |
| T4C.4 | Tab Thành viên: danh sách, thêm, xóa, đổi vai trò | M | P1 |

#### 4D — Task Feature
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T4D.1 | Trang danh sách Task theo Project (bảng + filter + paginate) | L | P1 |
| T4D.2 | Form tạo Task (title, desc, assignee, priority, duedate) | M | P1 |
| T4D.3 | Trang chi tiết Task (drawer hoặc page riêng) | L | P1 |
| T4D.4 | Component đổi Status của task (dropdown/button) | M | P1 |
| T4D.5 | Form sửa Task với optimistic concurrency (kèm RowVersion) | M | P1 |
| T4D.6 | Trang "My Tasks" — task được giao cho tôi | S | P2 |

#### 4E — Comment & Attachment
| # | Task | Độ phức tạp | Ưu tiên |
|---|---|:---:|:---:|
| T4E.1 | Component `CommentList` — hiển thị danh sách comment | M | P1 |
| T4E.2 | Component `CommentForm` — thêm/sửa comment | S | P1 |
| T4E.3 | Component `AttachmentList` — danh sách file đính kèm | S | P1 |
| T4E.4 | Component `FileUploader` — chọn file, preview, upload | M | P1 |
| T4E.5 | Xử lý tải file (click → gọi download API → save as) | M | P1 |

---

### PHASE 5 — Security, Performance & Operations

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T5.1 | Cấu hình rate limiting cho Login, Upload, Comment | S | P1 | |
| T5.2 | Cấu hình CORS chặt chẽ | S | P1 | |
| T5.3 | Thêm HTTP Security Headers (CSP, HSTS, X-Frame-Options) | S | P2 | |
| T5.4 | Kiểm tra tất cả endpoint: không cho phép truy cập khi không có quyền | M | P1 | |
| T5.5 | Viết integration tests cho các case truy cập trái phép | M | P1 | |
| T5.6 | Implement Health Check endpoint `/health` | S | P1 | |
| T5.7 | Cấu hình pagination mặc định, max page size | S | P1 | |
| T5.8 | Kiểm tra và tối ưu N+1 query trong list endpoints | M | P2 | |
| T5.9 | Viết Dockerfile cho Backend | S | P1 | |
| T5.10 | Viết Dockerfile cho Frontend | S | P1 | |
| T5.11 | Viết `docker-compose.yml` cho môi trường dev | S | P1 | |
| T5.12 | Cấu hình backup database (script hoặc Azure policy) | M | P2 | |
| T5.13 | Cấu hình backup file storage | S | P2 | |

---

### PHASE 6 — Testing & QA

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T6.1 | Unit test `PermissionService` — tất cả edge cases | M | P1 | |
| T6.2 | Unit test các Use Case chính (CreateTask, UpdateTask, etc.) | M | P1 | |
| T6.3 | Integration test: Login flow | S | P1 | |
| T6.4 | Integration test: CRUD Task với kiểm tra quyền | L | P1 | |
| T6.5 | Integration test: Upload + Download file | M | P2 | |
| T6.6 | Integration test: Audit log được ghi đúng | M | P2 | |
| T6.7 | Thực hiện kiểm tra thủ công (UAT) các luồng chính | L | P1 | |

---

### Tóm tắt độ ưu tiên

```
P1 = Phải có trước khi release MVP
P2 = Nên có nhưng có thể làm sau khi P1 xong
P3 = Nice-to-have, chuyển sang Phase 2
```

### Thứ tự thực thi gợi ý

```
Phase 0 (Setup)
    → Phase 1 (Domain + DB)
    → Phase 2 (Auth) [song song với Phase 1 ở cuối]
    → Phase 3A-3D (Core API)
    → Phase 4A-4D (Core Frontend) [song song với Phase 3]
    → Phase 3E-3G (Comment, Attachment, Audit)
    → Phase 4E (Comment, Attachment UI)
    → Phase 5 (Security + Ops)
    → Phase 6 (Testing + QA)
```

---

> [!TIP]
> **Điểm cần chốt trước khi bắt đầu code:**
> 1. Loại file storage sẽ dùng (Local folder / Azure Blob / AWS S3)? trà lời: dùng local folder
> 2. Kích thước file tối đa và loại file được phép? trả lời: 20mb, file dạng ảnh
> 3. Member có được sửa task của người khác không? trả lời: ko
> 4. Có cần Guest role trong MVP không? trà lời: ko
> 5. Phiên JWT hết hạn sau bao lâu? trà lời: 15 phút  
