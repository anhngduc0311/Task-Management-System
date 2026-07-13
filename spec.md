# SPEC — Task Management System
**Phiên bản:** 2.0 | **Ngày:** 2026-07-13 | **Trạng thái:** Sẵn sàng thực thi

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
- Quản lý **Sản phẩm** với danh mục cha/con, biến thể, đơn vị tính quy đổi, nhà cung cấp, xuất xứ.
- Quản lý **Tồn kho** theo kho: phiếu nhập, phiếu xuất, phiếu chuyển kho.
- **Báo cáo tồn kho** theo sản phẩm, kho, biến thể với lịch sử phát sinh.

### 1.3. Định vị sản phẩm

| Tiêu chí | Định vị |
|---|---|
| **Đối tượng** | Nhóm nội bộ 5–50 người |
| **Phong cách** | Jira-lite + ERP-lite: quản lý công việc kết hợp quản lý sản phẩm & tồn kho |
| **Giai đoạn** | MVP Phase 2 — bổ sung module QLSP + Tồn kho |
| **Kiến trúc** | Modular Monolith, mỗi module là bounded context riêng |
| **Ưu tiên** | Dễ dùng → Ổn định → Bảo mật → Truy vết → Mở rộng |

### 1.4. Những gì KHÔNG có trong MVP

> [!IMPORTANT]
> Các tính năng sau **bị khóa hoàn toàn** khỏi phạm vi MVP (ngoại trừ Biểu đồ Gantt đã được triển khai bổ sung ở Phase 7). Bất kỳ yêu cầu thêm tính năng này đều phải có quyết định chính thức.

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
- **FR-01.6** Người dùng mới có thể tự đăng ký tài khoản (Register) bằng Email, Tên đầy đủ và Mật khẩu.

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
- **FR-04.1** PM/Admin có thể thêm thành viên vào project bằng cách nhập Email của họ.
- **FR-04.2** PM/Admin có thể gán vai trò trong project cho từng thành viên (PM / Member / Guest).
- **FR-04.3** PM/Admin có thể xóa thành viên khỏi project.
- **FR-04.4** Một user có thể có vai trò khác nhau ở các project khác nhau.

#### FR-05: Quản lý Task
- **FR-05.1** PM/Member có thể tạo task trong project.
- **FR-05.2** Task có: Tiêu đề, Mô tả, Trạng thái, Ưu tiên, Assignee, DueDate, Người tạo, Ngày hoàn thành thực tế (CompletedAt).
- **FR-05.3** Task có các trạng thái: `Todo → In Progress → In Review → Done → Cancelled`.
- **FR-05.4** Task có các mức ưu tiên: `Low / Medium / High / Critical`.
- **FR-05.5** PM/Admin có thể giao task cho thành viên trong project.
- **FR-05.6** Người được giao có thể tự đổi trạng thái task của mình.
- **FR-05.7** PM/Admin có thể sửa mọi thuộc tính của task.
- **FR-05.8** Member chỉ được sửa task của mình (giới hạn trường: description, status).
- **FR-05.9** PM/Admin có thể xóa task (xóa mềm).
- **FR-05.10** Task hỗ trợ **optimistic concurrency**: không được ghi đè ngầm khi có xung đột.
- **FR-05.11** Hỗ trợ phân cấp Task cha - con (Parent-Child Hierarchy): Cho phép một Task chứa các Task con. Giao diện hiển thị trực quan và hệ thống tự động kiểm tra đệ quy chống vòng lặp liên kết (circular dependency).
- **FR-05.12** Kiểm tra ràng buộc khi xóa Task cha: Không cho phép xóa Task cha nếu có các Task con chưa hoàn thành (`Todo`/`InProgress`/`InReview`). Khi xóa Task cha thành công (xóa mềm), các Task con sẽ được gỡ liên kết tự động (sét `ParentTaskId` thành null).
- **FR-05.13** Hỗ trợ cấu hình Trường dữ liệu động (Dynamic Fields) tùy chỉnh theo từng dự án với các kiểu dữ liệu: Text, Number, Date, Boolean, Select, MultiSelect. Có thể thiết lập bắt buộc nhập (IsRequired), giá trị mặc định (DefaultValue) và thứ tự hiển thị (DisplayOrder).

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
- **FR-09.4** Tích hợp biểu đồ Gantt (Gantt Chart Tab) trong giao diện chi tiết dự án hiển thị thời gian bắt đầu và kết thúc của các task một cách trực quan.

#### FR-10: Audit Log
- **FR-10.1** Hệ thống ghi log tự động cho các hành động: tạo task, sửa task, đổi trạng thái, đổi assignee, đổi deadline, thay đổi Task cha/con, xóa task, thêm/xóa thành viên project, thay đổi quyền, tạo/sửa/xóa trường dữ liệu động.
- **FR-10.2** PM/Admin có thể xem audit log của project.
- **FR-10.3** Mỗi log entry ghi: EntityType, EntityId, Action, ChangedBy, ChangedAt, OldValue, NewValue.

#### FR-11: Báo cáo hiệu suất công việc (Work Performance & Reports)
- **FR-11.1** Cung cấp giao diện báo cáo tổng hợp trực quan cho phép xem nhanh hiệu suất dự án.
- **FR-11.2** Thống kê các chỉ số KPI: Tổng số Task, Task đã hoàn thành (số lượng hoàn thành đúng hạn), Task quá hạn (Overdue), và tỷ lệ hoàn thành (Completion Rate).
- **FR-11.3** Hiển thị biểu đồ phân tích: Trạng thái task (Donut chart), Mức độ ưu tiên task (Bar chart), và Danh sách những người được giao việc tích cực nhất (Top Assignees).
- **FR-11.4** Bộ lọc nâng cao: Lọc theo dự án, người nhận, trạng thái, mức độ ưu tiên, khoảng thời gian tạo task, và đặc biệt là lọc động dựa theo các Trường dữ liệu động (Dynamic Fields) của dự án đang chọn.
- **FR-11.5** Danh sách chi tiết các Task quá hạn, Task chưa hoàn thành, Task đã hoàn thành có phân trang (pagination) và liên kết mở nhanh chi tiết Task.

#### FR-12: Quản lý đơn vị tính
- **FR-12.1** Hệ thống có bảng quản lý đơn vị tính (Units) với Id, Code, Name, IsActive.
- **FR-12.2** Mỗi sản phẩm có một đơn vị tính cơ sở (BaseUnitId).
- **FR-12.3** Hỗ trợ quy đổi đơn vị: mỗi sản phẩm có bảng ProductUnitConversions định nghĩa tỷ lệ quy đổi từ đơn vị khác về đơn vị cơ sở (ví dụ: 1 Thùng = 24 Lon).
- **FR-12.4** Tồn kho phải lưu theo đơn vị cơ sở để tránh sai lệch.
- **FR-12.5** Khi nhập/xuất/chuyển kho có thể chọn đơn vị tính khác, backend tự quy đổi số lượng về đơn vị cơ sở.
- **FR-12.6** Không cho phép tỷ lệ quy đổi bằng 0 hoặc âm.
- **FR-12.7** Không cho phép xóa đơn vị tính đang được sản phẩm sử dụng.

#### FR-13: Quản lý danh mục sản phẩm
- **FR-13.1** Danh mục sản phẩm (ProductCategories) hỗ trợ cha/con nhiều cấp qua ParentId.
- **FR-13.2** Danh mục có: Id, ParentId, Code, Name, Description, IsActive, DisplayOrder.
- **FR-13.3** Không cho danh mục tự làm cha của chính nó.
- **FR-13.4** Không cho tạo vòng lặp danh mục (circular reference).
- **FR-13.5** Tìm kiếm theo danh mục cha phải trả ra cả sản phẩm thuộc danh mục con.
- **FR-13.6** Không cho xóa cứng danh mục đang có sản phẩm.
- **FR-13.7** Có thể xóa mềm hoặc tắt trạng thái sử dụng.

#### FR-14: Quản lý nhãn sản phẩm
- **FR-14.1** Một sản phẩm có thể có nhiều nhãn (Hàng bán chạy, Hàng mới, Hàng khuyến mãi, Hàng dễ vỡ).
- **FR-14.2** Có bảng ProductLabels (Id, Name, Code, Color, IsActive) và bảng liên kết ProductProductLabels.
- **FR-14.3** Có thể lọc sản phẩm theo nhãn.

#### FR-15: Quản lý xuất xứ
- **FR-15.1** Có bảng Origins (Id, Name, Code, IsActive) quản lý xuất xứ sản phẩm.
- **FR-15.2** Sản phẩm gắn với một xuất xứ (OriginId).
- **FR-15.3** Hỗ trợ filter sản phẩm theo xuất xứ.

#### FR-16: Quản lý nhà cung cấp
- **FR-16.1** Có module quản lý nhà cung cấp (Suppliers) với: Id, Code, Name, Phone, Email, Address, TaxCode, ContactPerson, IsActive, CreatedAt, UpdatedAt.
- **FR-16.2** Sản phẩm có thể gắn với một hoặc nhiều nhà cung cấp qua bảng ProductSuppliers.
- **FR-16.3** CRUD nhà cung cấp với phân quyền.
- **FR-16.4** Không cho xóa cứng nhà cung cấp đang được sản phẩm sử dụng.

#### FR-17: Quản lý sản phẩm
- **FR-17.1** CRUD sản phẩm với các thông tin: mã, tên, đơn vị tính, đơn giá, nhãn, ảnh, danh mục, trạng thái, mô tả, nhà cung cấp, xuất xứ, thuộc tính.
- **FR-17.2** Mã sản phẩm (ProductCode) là duy nhất, không được trùng, có thể nhập thủ công hoặc sinh tự động, dùng để tìm kiếm nhanh.
- **FR-17.3** Không cho xóa cứng sản phẩm nếu đã phát sinh tồn kho hoặc chứng từ kho.
- **FR-17.4** Sản phẩm có trạng thái: `Active | Inactive | Discontinued`.
- **FR-17.5** Sản phẩm Inactive không hiện trong danh sách chọn khi tạo phiếu mới.
- **FR-17.6** Sản phẩm Discontinued không cho nhập thêm hàng nếu business rule yêu cầu.
- **FR-17.7** Đơn giá mặc định (DefaultPrice), không được âm, có thể nhập theo đơn vị tính. Backend quy đổi đơn giá nếu cần.
- **FR-17.8** Upload nhiều ảnh sản phẩm, chọn ảnh chính (IsPrimary), giới hạn loại file: jpg, jpeg, png, webp. Giới hạn dung lượng theo cấu hình. Database chỉ lưu metadata (FileName, StorageKey, Url, IsPrimary, DisplayOrder).
- **FR-17.9** Mô tả chi tiết bằng rich text editor. Backend lưu dạng HTML đã sanitize hoặc JSON editor content. Phải chống XSS, không render HTML chưa sanitize. Giới hạn độ dài theo cấu hình.
- **FR-17.10** Một sản phẩm tối đa 2 nhóm thuộc tính (ProductAttributeGroups). Ví dụ: Màu sắc + Size. Không cho tạo attribute group thứ 3.
- **FR-17.11** Mỗi nhóm thuộc tính có nhiều giá trị (ProductAttributeValues). Ví dụ: Màu sắc = Đỏ, Xanh, Đen.
- **FR-17.12** Hệ thống tạo biến thể sản phẩm (ProductVariants) từ tổ hợp thuộc tính. Mỗi biến thể có SKU riêng, giá riêng nếu cần, ảnh riêng nếu cần, tồn kho riêng.
- **FR-17.13** Nếu sản phẩm không có thuộc tính → tồn kho theo sản phẩm gốc. Nếu có biến thể → tồn kho theo biến thể.
- **FR-17.14** Không cho trùng SKU variant. Không cho trùng tên thuộc tính trong cùng sản phẩm.
- **FR-17.15** Khi sản phẩm đã có tồn kho, hạn chế sửa/xóa variant để tránh sai lệch lịch sử.
- **FR-17.16** Sản phẩm hỗ trợ optimistic concurrency (RowVersion).

#### FR-18: Tìm kiếm & lọc sản phẩm
- **FR-18.1** Tìm kiếm theo tên sản phẩm, mã sản phẩm (LIKE search).
- **FR-18.2** Lọc theo danh mục, bao gồm tự động lấy sản phẩm thuộc danh mục con (includeChildCategories).
- **FR-18.3** Lọc theo nhà cung cấp, xuất xứ, trạng thái sử dụng, nhãn sản phẩm.
- **FR-18.4** Có pagination, sort theo: tên, mã, ngày tạo, đơn giá.
- **FR-18.5** Hỗ trợ API search: `POST /api/products/search` với request body chứa keyword, filters, pagination, sort.
- **FR-18.6** Query tối ưu: dùng projection DTO, không Include tràn lan, không concat SQL string nguy hiểm.
- **FR-18.7** Hiển thị tồn kho tổng nếu cần trong danh sách sản phẩm.
- **FR-18.8** Kiểm tra quyền xem sản phẩm nếu hệ thống có phân quyền.

#### FR-19: Quản lý kho hàng
- **FR-19.1** CRUD kho hàng (Warehouses) với: Id, Code, Name, Address, ManagerName, IsActive, CreatedAt, UpdatedAt.
- **FR-19.2** Không xóa cứng kho nếu đã có giao dịch tồn kho.
- **FR-19.3** Kho Inactive không cho tạo phiếu mới.

#### FR-20: Tồn kho & Phiếu nhập/xuất/chuyển kho
- **FR-20.1** Tồn kho theo Warehouse × Product (hoặc ProductVariant nếu có biến thể). Số lượng lưu theo đơn vị cơ sở.
- **FR-20.2** Có bảng StockBalances lưu số dư tồn kho hiện tại (QuantityOnHand, QuantityReserved).
- **FR-20.3** Có bảng StockMovements lưu lịch sử phát sinh với MovementType: `Import | Export | TransferIn | TransferOut | Adjustment`.
- **FR-20.4** Mọi thay đổi tồn kho phải sinh StockMovement. Backend là nguồn sự thật duy nhất.
- **FR-20.5** Không được sửa trực tiếp StockBalance từ frontend.
- **FR-20.6** Phiếu nhập kho (ImportReceipt): Status `Draft → Confirmed → Cancelled`. Khi Confirmed → tăng tồn kho + ghi StockMovement loại Import.
- **FR-20.7** Phiếu xuất kho (ExportReceipt): Status `Draft → Confirmed → Cancelled`. Khi Confirmed → giảm tồn kho + ghi StockMovement loại Export.
- **FR-20.8** Phiếu chuyển kho (TransferReceipt): Status `Draft → Confirmed → Cancelled`. Khi Confirmed → giảm tồn kho nguồn (TransferOut) + tăng tồn kho đích (TransferIn).
- **FR-20.9** Không cho xuất/chuyển quá tồn nếu AllowNegativeStock = false (mặc định false).
- **FR-20.10** Phiếu Confirmed không được sửa dòng hàng. Draft có thể sửa tự do.
- **FR-20.11** Mỗi phiếu phải có ít nhất một dòng hàng. Quantity phải > 0.
- **FR-20.12** Phiếu chuyển kho: FromWarehouseId ≠ ToWarehouseId.
- **FR-20.13** Toàn bộ thao tác kho phải nằm trong một database transaction.
- **FR-20.14** Hủy phiếu đã Confirmed cần tạo movement adjustment/phiếu đảo theo rule rõ ràng.
- **FR-20.15** Phiếu nhập/xuất mỗi dòng có: ProductId, ProductVariantId (nullable), UnitId, Quantity, QuantityBase, UnitPrice, TotalAmount, Note.
- **FR-20.16** Phiếu chuyển kho mỗi dòng có: ProductId, ProductVariantId (nullable), UnitId, Quantity, QuantityBase, Note (không có UnitPrice).

#### FR-21: Báo cáo tồn kho
- **FR-21.1** Xem tồn kho theo sản phẩm, theo kho, theo variant.
- **FR-21.2** Xem lịch sử nhập/xuất/chuyển kho (StockMovements).
- **FR-21.3** Filter theo: Product, ProductCode, Category, Warehouse, Supplier, Origin, Date range.
- **FR-21.4** Có pagination.
- **FR-21.5** Có khả năng export Excel ở phase sau.

#### FR-22: AuditLog & Permission cho module QLSP + Kho
- **FR-22.1** Ghi AuditLog cho tất cả hành động CRUD trên: sản phẩm, danh mục, nhà cung cấp, đơn vị tính, quy đổi đơn vị, kho, phiếu nhập/xuất/chuyển kho, điều chỉnh tồn kho. AuditLog ghi: EntityType, EntityId, Action, ChangedById, ChangedAt, OldValue, NewValue, Metadata.
- **FR-22.2** Thiết kế 4 vai trò cho module: Admin (toàn quyền), Inventory Manager (quản lý toàn bộ QLSP + Kho), Warehouse Staff (tạo phiếu Draft, xem sản phẩm và tồn kho), Viewer (chỉ xem).
- **FR-22.3** PermissionService kiểm tra quyền theo action cho từng vai trò.
- **FR-22.4** Warehouse Staff không được xác nhận phiếu nếu chưa có quyền.
- **FR-22.5** Viewer không được tạo/sửa/xóa/xác nhận bất kỳ thực thể nào.

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
| Product Name | ≤ 300 ký tự |
| Product Code | ≤ 50 ký tự |
| Product Description (Rich Text) | ≤ 50.000 ký tự (cấu hình) |
| Product Image | ≤ 5 MB/ảnh (cấu hình), tối đa 10 ảnh/sản phẩm |
| Product Image Types | jpg, jpeg, png, webp |
| Variant SKU | ≤ 50 ký tự |
| Receipt Lines/receipt | ≤ 100 dòng (cấu hình) |
| Category Depth | ≤ 5 cấp (cấu hình) |
| Attribute Groups/product | Tối đa 2 |

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
  RefreshToken   NVARCHAR(512)     NULL,         -- Token để làm mới phiên đăng nhập
  RefreshTokenExpiryTime DATETIME2 NULL,         -- Thời gian hết hạn refresh token
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
  Id           UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProjectId    UNIQUEIDENTIFIER  NOT NULL FK → Projects.Id,
  Title        NVARCHAR(200)     NOT NULL,
  Description  NVARCHAR(5000)    NULL,
  Status       NVARCHAR(20)      NOT NULL DEFAULT 'Todo',
               -- Todo | InProgress | InReview | Done | Cancelled
  Priority     NVARCHAR(20)      NOT NULL DEFAULT 'Medium',
               -- Low | Medium | High | Critical
  AssigneeId   UNIQUEIDENTIFIER  NULL FK → Users.Id,
  CreatedById  UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  DueDate      DATE              NULL,
  CompletedAt  DATETIME2         NULL,          -- Thời gian hoàn thành thực tế (Khi chuyển sang Done)
  ParentTaskId UNIQUEIDENTIFIER  NULL FK → Tasks.Id, -- Khóa ngoại trỏ đến Task cha (tự tham chiếu)
  IsDeleted    BIT               NOT NULL DEFAULT 0,
  CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  RowVersion   ROWVERSION        NOT NULL  -- Concurrency check (optimistic concurrency)
)

-- Indexes
CREATE INDEX IX_Tasks_ProjectId ON Tasks(ProjectId) WHERE IsDeleted = 0;
CREATE INDEX IX_Tasks_AssigneeId ON Tasks(AssigneeId) WHERE IsDeleted = 0;
CREATE INDEX IX_Tasks_Status ON Tasks(ProjectId, Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Tasks_DueDate ON Tasks(DueDate) WHERE IsDeleted = 0;
```

#### Bảng `DynamicFieldDefinitions` (Bổ sung mới)
```sql
DynamicFieldDefinitions (
  Id           UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProjectId    UNIQUEIDENTIFIER  NOT NULL FK → Projects.Id,
  FieldName    NVARCHAR(100)     NOT NULL,
  FieldKey     NVARCHAR(100)     NOT NULL,     -- Key định danh (ví dụ: customer_name, build_version)
  FieldType    NVARCHAR(50)      NOT NULL,     -- Text | Number | Date | Boolean | Select | MultiSelect
  IsRequired   BIT               NOT NULL DEFAULT 0,
  Options      NVARCHAR(MAX)     NULL,         -- Định dạng JSON Array chứa danh sách các tùy chọn
  DefaultValue NVARCHAR(MAX)     NULL,
  DisplayOrder INT               NOT NULL DEFAULT 0,
  IsActive     BIT               NOT NULL DEFAULT 1,
  CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `TaskDynamicFieldValues` (Bổ sung mới)
```sql
TaskDynamicFieldValues (
  TaskId         UNIQUEIDENTIFIER  FK → Tasks.Id,
  DynamicFieldId UNIQUEIDENTIFIER  FK → DynamicFieldDefinitions.Id,
  FieldValue     NVARCHAR(MAX)     NULL,         -- Giá trị lưu dưới dạng chuỗi (JSON array đối với MultiSelect)
  PRIMARY KEY (TaskId, DynamicFieldId)
)
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

#### Bảng `Units`
```sql
Units (
  Id          UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  Code        NVARCHAR(20)      NOT NULL UNIQUE,
  Name        NVARCHAR(100)     NOT NULL,
  IsActive    BIT               NOT NULL DEFAULT 1,
  CreatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `ProductCategories`
```sql
ProductCategories (
  Id           UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ParentId     UNIQUEIDENTIFIER  NULL FK → ProductCategories.Id,
  Code         NVARCHAR(50)      NOT NULL UNIQUE,
  Name         NVARCHAR(200)     NOT NULL,
  Description  NVARCHAR(1000)    NULL,
  IsActive     BIT               NOT NULL DEFAULT 1,
  DisplayOrder INT               NOT NULL DEFAULT 0,
  CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
CREATE INDEX IX_ProductCategories_ParentId ON ProductCategories(ParentId);
-- CHECK: Không cho ParentId = Id (chống tự tham chiếu)
ALTER TABLE ProductCategories ADD CONSTRAINT CK_Category_NotSelfParent CHECK (ParentId <> Id);
```

#### Bảng `Origins`
```sql
Origins (
  Id        UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  Code      NVARCHAR(20)      NOT NULL UNIQUE,
  Name      NVARCHAR(200)     NOT NULL,
  IsActive  BIT               NOT NULL DEFAULT 1,
  CreatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `Suppliers`
```sql
Suppliers (
  Id            UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  Code          NVARCHAR(50)      NOT NULL UNIQUE,
  Name          NVARCHAR(300)     NOT NULL,
  Phone         NVARCHAR(20)      NULL,
  Email         NVARCHAR(256)     NULL,
  Address       NVARCHAR(500)     NULL,
  TaxCode       NVARCHAR(20)      NULL,
  ContactPerson NVARCHAR(200)     NULL,
  IsActive      BIT               NOT NULL DEFAULT 1,
  CreatedAt     DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt     DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `ProductLabels`
```sql
ProductLabels (
  Id        UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  Code      NVARCHAR(50)      NOT NULL UNIQUE,
  Name      NVARCHAR(100)     NOT NULL,
  Color     NVARCHAR(7)       NULL,       -- Hex color, ví dụ: #FF5733
  IsActive  BIT               NOT NULL DEFAULT 1,
  CreatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `Products`
```sql
Products (
  Id            UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProductCode   NVARCHAR(50)      NOT NULL UNIQUE,
  Name          NVARCHAR(300)     NOT NULL,
  CategoryId    UNIQUEIDENTIFIER  NULL FK → ProductCategories.Id,
  BaseUnitId    UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,
  DefaultPrice  DECIMAL(18,4)     NOT NULL DEFAULT 0,
  OriginId      UNIQUEIDENTIFIER  NULL FK → Origins.Id,
  Status        NVARCHAR(20)      NOT NULL DEFAULT 'Active',
               -- Active | Inactive | Discontinued
  Description   NVARCHAR(MAX)     NULL,       -- HTML sanitized hoặc JSON editor content
  IsDeleted     BIT               NOT NULL DEFAULT 0,
  CreatedById   UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  UpdatedById   UNIQUEIDENTIFIER  NULL FK → Users.Id,
  CreatedAt     DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt     DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  RowVersion    ROWVERSION        NOT NULL
)

-- Indexes
CREATE UNIQUE INDEX IX_Products_ProductCode ON Products(ProductCode) WHERE IsDeleted = 0;
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId) WHERE IsDeleted = 0;
CREATE INDEX IX_Products_Status ON Products(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Products_Name ON Products(Name) WHERE IsDeleted = 0;
CREATE INDEX IX_Products_OriginId ON Products(OriginId) WHERE IsDeleted = 0;

-- Constraint: DefaultPrice >= 0
ALTER TABLE Products ADD CONSTRAINT CK_Products_DefaultPrice CHECK (DefaultPrice >= 0);
```

#### Bảng `ProductImages`
```sql
ProductImages (
  Id            UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProductId     UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  FileName      NVARCHAR(260)     NOT NULL,
  StorageKey    NVARCHAR(512)     NOT NULL,
  Url           NVARCHAR(1000)    NOT NULL,
  IsPrimary     BIT               NOT NULL DEFAULT 0,
  DisplayOrder  INT               NOT NULL DEFAULT 0,
  CreatedAt     DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
CREATE INDEX IX_ProductImages_ProductId ON ProductImages(ProductId);
```

#### Bảng `ProductProductLabels` (Many-to-Many)
```sql
ProductProductLabels (
  ProductId UNIQUEIDENTIFIER  FK → Products.Id,
  LabelId   UNIQUEIDENTIFIER  FK → ProductLabels.Id,
  PRIMARY KEY (ProductId, LabelId)
)
```

#### Bảng `ProductUnitConversions`
```sql
ProductUnitConversions (
  Id              UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProductId       UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  FromUnitId      UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,
  ToBaseUnitId    UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,  -- = Product.BaseUnitId
  ConversionRate  DECIMAL(18,6)     NOT NULL,                -- 1 FromUnit = ConversionRate BaseUnit
  CreatedAt       DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt       DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UNIQUE (ProductId, FromUnitId)
)

-- Constraint: ConversionRate > 0
ALTER TABLE ProductUnitConversions ADD CONSTRAINT CK_UnitConversion_Rate CHECK (ConversionRate > 0);
```

#### Bảng `ProductSuppliers` (Many-to-Many)
```sql
ProductSuppliers (
  ProductId  UNIQUEIDENTIFIER  FK → Products.Id,
  SupplierId UNIQUEIDENTIFIER  FK → Suppliers.Id,
  PRIMARY KEY (ProductId, SupplierId)
)
```

#### Bảng `ProductAttributeGroups`
```sql
ProductAttributeGroups (
  Id          UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProductId   UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  Name        NVARCHAR(100)     NOT NULL,   -- Ví dụ: "Màu sắc", "Size"
  DisplayOrder INT              NOT NULL DEFAULT 0,
  CreatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UNIQUE (ProductId, Name)
)
CREATE INDEX IX_ProductAttributeGroups_ProductId ON ProductAttributeGroups(ProductId);
-- Ràng buộc tối đa 2 group/product được kiểm tra ở Application layer
```

#### Bảng `ProductAttributeValues`
```sql
ProductAttributeValues (
  Id               UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  AttributeGroupId UNIQUEIDENTIFIER  NOT NULL FK → ProductAttributeGroups.Id,
  Value            NVARCHAR(100)     NOT NULL,  -- Ví dụ: "Đỏ", "S", "M"
  DisplayOrder     INT               NOT NULL DEFAULT 0,
  CreatedAt        DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UNIQUE (AttributeGroupId, Value)
)
```

#### Bảng `ProductVariants`
```sql
ProductVariants (
  Id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ProductId         UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  SKU               NVARCHAR(50)      NOT NULL UNIQUE,
  AttributeValue1Id UNIQUEIDENTIFIER  NULL FK → ProductAttributeValues.Id,
  AttributeValue2Id UNIQUEIDENTIFIER  NULL FK → ProductAttributeValues.Id,
  Price             DECIMAL(18,4)     NULL,     -- Override giá nếu cần, NULL = dùng giá Product
  ImageUrl          NVARCHAR(1000)    NULL,     -- Override ảnh nếu cần
  IsActive          BIT               NOT NULL DEFAULT 1,
  CreatedAt         DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt         DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
CREATE UNIQUE INDEX IX_ProductVariants_SKU ON ProductVariants(SKU);
CREATE INDEX IX_ProductVariants_ProductId ON ProductVariants(ProductId);

-- Constraint: Price >= 0 nếu có
ALTER TABLE ProductVariants ADD CONSTRAINT CK_Variants_Price CHECK (Price IS NULL OR Price >= 0);
```

#### Bảng `Warehouses`
```sql
Warehouses (
  Id          UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  Code        NVARCHAR(50)      NOT NULL UNIQUE,
  Name        NVARCHAR(200)     NOT NULL,
  Address     NVARCHAR(500)     NULL,
  ManagerName NVARCHAR(200)     NULL,
  IsActive    BIT               NOT NULL DEFAULT 1,
  CreatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt   DATETIME2         NOT NULL DEFAULT GETUTCDATE()
)
```

#### Bảng `StockBalances`
```sql
StockBalances (
  Id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  WarehouseId       UNIQUEIDENTIFIER  NOT NULL FK → Warehouses.Id,
  ProductId         UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  ProductVariantId  UNIQUEIDENTIFIER  NULL FK → ProductVariants.Id,
  QuantityOnHand    DECIMAL(18,4)     NOT NULL DEFAULT 0,
  QuantityReserved  DECIMAL(18,4)     NOT NULL DEFAULT 0,
  UpdatedAt         DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UNIQUE (WarehouseId, ProductId, ProductVariantId)
)
CREATE INDEX IX_StockBalances_WarehouseId ON StockBalances(WarehouseId);
CREATE INDEX IX_StockBalances_ProductId ON StockBalances(ProductId);
CREATE INDEX IX_StockBalances_ProductVariantId ON StockBalances(ProductVariantId) WHERE ProductVariantId IS NOT NULL;
```

#### Bảng `StockMovements`
```sql
StockMovements (
  Id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  WarehouseId       UNIQUEIDENTIFIER  NOT NULL FK → Warehouses.Id,
  ProductId         UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  ProductVariantId  UNIQUEIDENTIFIER  NULL FK → ProductVariants.Id,
  MovementType      NVARCHAR(20)      NOT NULL,
                   -- Import | Export | TransferIn | TransferOut | Adjustment
  QuantityBase      DECIMAL(18,4)     NOT NULL,  -- Số lượng theo đơn vị cơ sở
  UnitId            UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,
  QuantityInput     DECIMAL(18,4)     NOT NULL,  -- Số lượng người dùng nhập
  ReferenceType     NVARCHAR(50)      NULL,      -- ImportReceipt | ExportReceipt | TransferReceipt
  ReferenceId       UNIQUEIDENTIFIER  NULL,      -- Id của phiếu liên quan
  CreatedById       UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  CreatedAt         DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  Note              NVARCHAR(500)     NULL
)
CREATE INDEX IX_StockMovements_WarehouseId ON StockMovements(WarehouseId);
CREATE INDEX IX_StockMovements_ProductId ON StockMovements(ProductId);
CREATE INDEX IX_StockMovements_MovementType ON StockMovements(MovementType);
CREATE INDEX IX_StockMovements_CreatedAt ON StockMovements(CreatedAt DESC);
CREATE INDEX IX_StockMovements_ReferenceType_ReferenceId ON StockMovements(ReferenceType, ReferenceId);
```

#### Bảng `ImportReceipts`
```sql
ImportReceipts (
  Id           UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ReceiptCode  NVARCHAR(50)      NOT NULL UNIQUE,
  WarehouseId  UNIQUEIDENTIFIER  NOT NULL FK → Warehouses.Id,
  SupplierId   UNIQUEIDENTIFIER  NULL FK → Suppliers.Id,
  ReceiptDate  DATE              NOT NULL,
  Status       NVARCHAR(20)      NOT NULL DEFAULT 'Draft',
              -- Draft | Confirmed | Cancelled
  Note         NVARCHAR(1000)    NULL,
  CreatedById  UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  RowVersion   ROWVERSION        NOT NULL
)
CREATE INDEX IX_ImportReceipts_WarehouseId ON ImportReceipts(WarehouseId);
CREATE INDEX IX_ImportReceipts_Status ON ImportReceipts(Status);
CREATE INDEX IX_ImportReceipts_ReceiptDate ON ImportReceipts(ReceiptDate);
```

#### Bảng `ImportReceiptLines`
```sql
ImportReceiptLines (
  Id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ReceiptId         UNIQUEIDENTIFIER  NOT NULL FK → ImportReceipts.Id,
  ProductId         UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  ProductVariantId  UNIQUEIDENTIFIER  NULL FK → ProductVariants.Id,
  UnitId            UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,
  Quantity          DECIMAL(18,4)     NOT NULL,
  QuantityBase      DECIMAL(18,4)     NOT NULL,
  UnitPrice         DECIMAL(18,4)     NOT NULL DEFAULT 0,
  TotalAmount       DECIMAL(18,4)     NOT NULL DEFAULT 0,
  Note              NVARCHAR(500)     NULL
)
CREATE INDEX IX_ImportReceiptLines_ReceiptId ON ImportReceiptLines(ReceiptId);

-- Constraints
ALTER TABLE ImportReceiptLines ADD CONSTRAINT CK_ImportLine_Quantity CHECK (Quantity > 0);
ALTER TABLE ImportReceiptLines ADD CONSTRAINT CK_ImportLine_QuantityBase CHECK (QuantityBase > 0);
ALTER TABLE ImportReceiptLines ADD CONSTRAINT CK_ImportLine_UnitPrice CHECK (UnitPrice >= 0);
```

#### Bảng `ExportReceipts`
```sql
ExportReceipts (
  Id           UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ReceiptCode  NVARCHAR(50)      NOT NULL UNIQUE,
  WarehouseId  UNIQUEIDENTIFIER  NOT NULL FK → Warehouses.Id,
  ExportDate   DATE              NOT NULL,
  Reason       NVARCHAR(500)     NULL,
  Status       NVARCHAR(20)      NOT NULL DEFAULT 'Draft',
              -- Draft | Confirmed | Cancelled
  Note         NVARCHAR(1000)    NULL,
  CreatedById  UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  RowVersion   ROWVERSION        NOT NULL
)
CREATE INDEX IX_ExportReceipts_WarehouseId ON ExportReceipts(WarehouseId);
CREATE INDEX IX_ExportReceipts_Status ON ExportReceipts(Status);
CREATE INDEX IX_ExportReceipts_ExportDate ON ExportReceipts(ExportDate);
```

#### Bảng `ExportReceiptLines`
```sql
ExportReceiptLines (
  Id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  ReceiptId         UNIQUEIDENTIFIER  NOT NULL FK → ExportReceipts.Id,
  ProductId         UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  ProductVariantId  UNIQUEIDENTIFIER  NULL FK → ProductVariants.Id,
  UnitId            UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,
  Quantity          DECIMAL(18,4)     NOT NULL,
  QuantityBase      DECIMAL(18,4)     NOT NULL,
  UnitPrice         DECIMAL(18,4)     NOT NULL DEFAULT 0,
  TotalAmount       DECIMAL(18,4)     NOT NULL DEFAULT 0,
  Note              NVARCHAR(500)     NULL
)
CREATE INDEX IX_ExportReceiptLines_ReceiptId ON ExportReceiptLines(ReceiptId);

-- Constraints
ALTER TABLE ExportReceiptLines ADD CONSTRAINT CK_ExportLine_Quantity CHECK (Quantity > 0);
ALTER TABLE ExportReceiptLines ADD CONSTRAINT CK_ExportLine_QuantityBase CHECK (QuantityBase > 0);
ALTER TABLE ExportReceiptLines ADD CONSTRAINT CK_ExportLine_UnitPrice CHECK (UnitPrice >= 0);
```

#### Bảng `TransferReceipts`
```sql
TransferReceipts (
  Id               UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  TransferCode     NVARCHAR(50)      NOT NULL UNIQUE,
  FromWarehouseId  UNIQUEIDENTIFIER  NOT NULL FK → Warehouses.Id,
  ToWarehouseId    UNIQUEIDENTIFIER  NOT NULL FK → Warehouses.Id,
  TransferDate     DATE              NOT NULL,
  Status           NVARCHAR(20)      NOT NULL DEFAULT 'Draft',
                  -- Draft | Confirmed | Cancelled
  Note             NVARCHAR(1000)    NULL,
  CreatedById      UNIQUEIDENTIFIER  NOT NULL FK → Users.Id,
  CreatedAt        DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  UpdatedAt        DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
  RowVersion       ROWVERSION        NOT NULL
)
CREATE INDEX IX_TransferReceipts_FromWarehouseId ON TransferReceipts(FromWarehouseId);
CREATE INDEX IX_TransferReceipts_ToWarehouseId ON TransferReceipts(ToWarehouseId);
CREATE INDEX IX_TransferReceipts_Status ON TransferReceipts(Status);

-- Constraint: FromWarehouseId != ToWarehouseId
ALTER TABLE TransferReceipts ADD CONSTRAINT CK_Transfer_DifferentWarehouses CHECK (FromWarehouseId <> ToWarehouseId);
```

#### Bảng `TransferReceiptLines`
```sql
TransferReceiptLines (
  Id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
  TransferId        UNIQUEIDENTIFIER  NOT NULL FK → TransferReceipts.Id,
  ProductId         UNIQUEIDENTIFIER  NOT NULL FK → Products.Id,
  ProductVariantId  UNIQUEIDENTIFIER  NULL FK → ProductVariants.Id,
  UnitId            UNIQUEIDENTIFIER  NOT NULL FK → Units.Id,
  Quantity          DECIMAL(18,4)     NOT NULL,
  QuantityBase      DECIMAL(18,4)     NOT NULL,
  Note              NVARCHAR(500)     NULL
)
CREATE INDEX IX_TransferReceiptLines_TransferId ON TransferReceiptLines(TransferId);

-- Constraints
ALTER TABLE TransferReceiptLines ADD CONSTRAINT CK_TransferLine_Quantity CHECK (Quantity > 0);
ALTER TABLE TransferReceiptLines ADD CONSTRAINT CK_TransferLine_QuantityBase CHECK (QuantityBase > 0);
```

---

### 3.4. API Endpoints

#### Authentication
```
POST   /api/auth/login          → Đăng nhập, trả JWT
POST   /api/auth/register       → Đăng ký người dùng mới
POST   /api/auth/logout         → Đăng xuất
POST   /api/auth/refresh-token  → Làm mới token
POST   /api/auth/change-password → Đổi mật khẩu
```

#### Users
```
GET    /api/users               → [Admin] Danh sách users (paginated)
GET    /api/users/{id}          → Chi tiết user
PUT    /api/users/{id}          → Cập nhật thông tin cá nhân
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
POST   /api/projects/{id}/members         → Thêm thành viên (bằng Email)
PUT    /api/projects/{id}/members/{uid}   → Đổi vai trò thành viên
DELETE /api/projects/{id}/members/{uid}   → Xóa thành viên
```

#### Tasks
```
GET    /api/projects/{pid}/tasks          → Danh sách task (filter + paginate)
POST   /api/projects/{pid}/tasks          → Tạo task mới
GET    /api/tasks/{id}                    → Chi tiết task
PUT    /api/tasks/{id}                    → Sửa task (kèm RowVersion, ParentTaskId và DynamicValues)
PATCH  /api/tasks/{id}/status             → Đổi trạng thái
PATCH  /api/tasks/{id}/assignee           → Đổi assignee
DELETE /api/tasks/{id}                    → Xóa mềm task (chỉ xóa khi các task con đã Done hoặc Cancelled)
GET    /api/tasks/{id}/children           → Danh sách task con
PATCH  /api/tasks/{id}/parent             → Gán task cha
PATCH  /api/tasks/{id}/remove-parent      → Gỡ liên kết task cha
GET    /api/tasks/my-tasks                → Task được giao cho tôi
```

#### Dynamic Fields
```
GET    /api/projects/{projectId}/dynamic-fields → Lấy danh sách trường động của dự án
POST   /api/projects/{projectId}/dynamic-fields → Tạo trường động mới cho dự án
PUT    /api/dynamic-fields/{fieldId}            → Cập nhật định nghĩa trường động
DELETE /api/dynamic-fields/{fieldId}            → Xóa định nghĩa trường động
GET    /api/tasks/{taskId}/dynamic-values       → Lấy giá trị trường động của task
PUT    /api/tasks/{taskId}/dynamic-values       → Cập nhật giá trị trường động của task
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

#### Reports
```
GET    /api/reports/work-summary          → Tóm tắt KPI công việc
GET    /api/reports/tasks-by-status       → Báo cáo số lượng task theo trạng thái
GET    /api/reports/tasks-by-priority     → Báo cáo số lượng task theo mức độ ưu tiên
GET    /api/reports/tasks-by-assignee     → Báo cáo số lượng task theo người xử lý
GET    /api/reports/tasks-by-project      → Báo cáo số lượng task theo dự án
GET    /api/reports/overdue-tasks         → Danh sách task quá hạn (phân trang)
GET    /api/reports/completed-tasks       → Danh sách task đã hoàn thành (phân trang)
GET    /api/reports/uncompleted-tasks     → Danh sách task chưa hoàn thành (phân trang)
POST   /api/reports/advanced              → Lọc nâng cao các task báo cáo
```

#### Audit Logs
```
GET    /api/projects/{pid}/audit-logs     → [PM/Admin] Log của project
GET    /api/tasks/{tid}/audit-logs        → [PM/Admin] Log của task
```

#### Units
```
GET    /api/units                         → Danh sách đơn vị tính
GET    /api/units/{id}                    → Chi tiết đơn vị tính
POST   /api/units                         → Tạo đơn vị tính
PUT    /api/units/{id}                    → Sửa đơn vị tính
DELETE /api/units/{id}                    → Xóa đơn vị tính (chỉ khi chưa được sử dụng)
```

#### Product Categories
```
GET    /api/product-categories            → Danh sách danh mục (hỗ trợ tree)
GET    /api/product-categories/{id}       → Chi tiết danh mục
POST   /api/product-categories            → Tạo danh mục
PUT    /api/product-categories/{id}       → Sửa danh mục
DELETE /api/product-categories/{id}       → Xóa mềm danh mục (chỉ khi không có sản phẩm)
GET    /api/product-categories/{id}/children → Lấy danh mục con
```

#### Origins
```
GET    /api/origins                       → Danh sách xuất xứ
GET    /api/origins/{id}                  → Chi tiết xuất xứ
POST   /api/origins                       → Tạo xuất xứ
PUT    /api/origins/{id}                  → Sửa xuất xứ
DELETE /api/origins/{id}                  → Xóa xuất xứ
```

#### Product Labels
```
GET    /api/product-labels                → Danh sách nhãn sản phẩm
GET    /api/product-labels/{id}           → Chi tiết nhãn
POST   /api/product-labels                → Tạo nhãn
PUT    /api/product-labels/{id}           → Sửa nhãn
DELETE /api/product-labels/{id}           → Xóa nhãn
```

#### Suppliers
```
GET    /api/suppliers                     → Danh sách nhà cung cấp (paginated)
GET    /api/suppliers/{id}                → Chi tiết nhà cung cấp
POST   /api/suppliers                     → Tạo nhà cung cấp
PUT    /api/suppliers/{id}                → Sửa nhà cung cấp
DELETE /api/suppliers/{id}                → Xóa nhà cung cấp (chỉ khi chưa gắn sản phẩm)
```

#### Products
```
GET    /api/products                      → Danh sách sản phẩm (paginated + filter)
GET    /api/products/{id}                 → Chi tiết sản phẩm
POST   /api/products                      → Tạo sản phẩm
PUT    /api/products/{id}                 → Sửa sản phẩm (kèm RowVersion)
DELETE /api/products/{id}                 → Xóa mềm sản phẩm (chỉ khi chưa có tồn kho/chứng từ)
POST   /api/products/search              → Tìm kiếm nâng cao sản phẩm
```

#### Product Images
```
GET    /api/products/{id}/images          → Danh sách ảnh sản phẩm
POST   /api/products/{id}/images          → Upload ảnh sản phẩm (multi-file)
DELETE /api/products/{id}/images/{imageId} → Xóa ảnh sản phẩm
PUT    /api/products/{id}/images/{imageId}/primary → Đặt ảnh chính
```

#### Product Unit Conversions
```
GET    /api/products/{id}/unit-conversions           → Danh sách quy đổi đơn vị
POST   /api/products/{id}/unit-conversions           → Thêm quy đổi đơn vị
PUT    /api/products/{id}/unit-conversions/{convId}  → Sửa quy đổi
DELETE /api/products/{id}/unit-conversions/{convId}  → Xóa quy đổi
```

#### Product Variants
```
GET    /api/products/{id}/variants        → Danh sách biến thể sản phẩm
POST   /api/products/{id}/variants        → Tạo biến thể (từ tổ hợp thuộc tính)
PUT    /api/products/{id}/variants/{vid}  → Sửa biến thể (SKU, giá, ảnh)
DELETE /api/products/{id}/variants/{vid}  → Xóa biến thể (chỉ khi chưa có tồn kho)
```

#### Product Attribute Groups & Values
```
GET    /api/products/{id}/attribute-groups              → Danh sách nhóm thuộc tính
POST   /api/products/{id}/attribute-groups              → Tạo nhóm thuộc tính (tối đa 2)
PUT    /api/products/{id}/attribute-groups/{gid}        → Sửa nhóm thuộc tính
DELETE /api/products/{id}/attribute-groups/{gid}        → Xóa nhóm thuộc tính
POST   /api/products/{id}/attribute-groups/{gid}/values → Thêm giá trị thuộc tính
DELETE /api/products/{id}/attribute-values/{vid}        → Xóa giá trị thuộc tính
```

#### Warehouses
```
GET    /api/warehouses                    → Danh sách kho
GET    /api/warehouses/{id}               → Chi tiết kho
POST   /api/warehouses                    → Tạo kho
PUT    /api/warehouses/{id}               → Sửa kho
DELETE /api/warehouses/{id}               → Xóa kho (chỉ khi chưa có giao dịch)
```

#### Import Receipts (Phiếu nhập kho)
```
GET    /api/inventory/import-receipts                → Danh sách phiếu nhập (paginated)
GET    /api/inventory/import-receipts/{id}            → Chi tiết phiếu nhập
POST   /api/inventory/import-receipts                → Tạo phiếu nhập (Draft)
PUT    /api/inventory/import-receipts/{id}            → Sửa phiếu nhập (chỉ Draft)
POST   /api/inventory/import-receipts/{id}/confirm   → Xác nhận phiếu nhập → tăng tồn kho
POST   /api/inventory/import-receipts/{id}/cancel    → Hủy phiếu nhập
```

#### Export Receipts (Phiếu xuất kho)
```
GET    /api/inventory/export-receipts                → Danh sách phiếu xuất (paginated)
GET    /api/inventory/export-receipts/{id}            → Chi tiết phiếu xuất
POST   /api/inventory/export-receipts                → Tạo phiếu xuất (Draft)
PUT    /api/inventory/export-receipts/{id}            → Sửa phiếu xuất (chỉ Draft)
POST   /api/inventory/export-receipts/{id}/confirm   → Xác nhận phiếu xuất → giảm tồn kho
POST   /api/inventory/export-receipts/{id}/cancel    → Hủy phiếu xuất
```

#### Transfer Receipts (Phiếu chuyển kho)
```
GET    /api/inventory/transfer-receipts              → Danh sách phiếu chuyển (paginated)
GET    /api/inventory/transfer-receipts/{id}          → Chi tiết phiếu chuyển
POST   /api/inventory/transfer-receipts              → Tạo phiếu chuyển (Draft)
PUT    /api/inventory/transfer-receipts/{id}          → Sửa phiếu chuyển (chỉ Draft)
POST   /api/inventory/transfer-receipts/{id}/confirm → Xác nhận chuyển kho → giảm kho nguồn + tăng kho đích
POST   /api/inventory/transfer-receipts/{id}/cancel  → Hủy phiếu chuyển
```

#### Stock Reports (Báo cáo tồn kho)
```
GET    /api/inventory/stock-balances                 → Tồn kho hiện tại (paginated + filter)
GET    /api/inventory/stock-movements                → Lịch sử phát sinh tồn kho (paginated + filter)
GET    /api/inventory/products/{productId}/stock      → Tồn kho theo sản phẩm (tất cả kho)
GET    /api/inventory/warehouses/{warehouseId}/stock  → Tồn kho theo kho (tất cả sản phẩm)
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
│   │   ├── AuthController.cs
│   │   ├── ProjectsController.cs
│   │   ├── TasksController.cs
│   │   ├── CommentsController.cs
│   │   ├── AttachmentsController.cs
│   │   ├── UnitsController.cs
│   │   ├── ProductCategoriesController.cs
│   │   ├── OriginsController.cs
│   │   ├── ProductLabelsController.cs
│   │   ├── SuppliersController.cs
│   │   ├── ProductsController.cs
│   │   ├── WarehousesController.cs
│   │   ├── ImportReceiptsController.cs
│   │   ├── ExportReceiptsController.cs
│   │   ├── TransferReceiptsController.cs
│   │   └── StockReportsController.cs
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
│   │   ├── Attachments/
│   │   ├── Products/              # ← MỚI
│   │   │   ├── CreateProduct/
│   │   │   ├── UpdateProduct/
│   │   │   ├── DeleteProduct/
│   │   │   ├── SearchProducts/
│   │   │   ├── ManageProductImages/
│   │   │   ├── ManageUnitConversions/
│   │   │   ├── ManageAttributeGroups/
│   │   │   └── ManageVariants/
│   │   ├── Categories/            # ← MỚI
│   │   ├── Units/                 # ← MỚI
│   │   ├── Origins/               # ← MỚI
│   │   ├── Labels/                # ← MỚI
│   │   ├── Suppliers/             # ← MỚI
│   │   ├── Warehouses/            # ← MỚI
│   │   └── Inventory/             # ← MỚI
│   │       ├── ImportReceipts/
│   │       │   ├── CreateImportReceipt/
│   │       │   ├── UpdateImportReceipt/
│   │       │   ├── ConfirmImportReceipt/
│   │       │   └── CancelImportReceipt/
│   │       ├── ExportReceipts/
│   │       │   ├── CreateExportReceipt/
│   │       │   ├── UpdateExportReceipt/
│   │       │   ├── ConfirmExportReceipt/
│   │       │   └── CancelExportReceipt/
│   │       ├── TransferReceipts/
│   │       │   ├── CreateTransferReceipt/
│   │       │   ├── UpdateTransferReceipt/
│   │       │   ├── ConfirmTransferReceipt/
│   │       │   └── CancelTransferReceipt/
│   │       └── StockReports/
│   ├── DTOs/
│   │   ├── Tasks/
│   │   ├── Products/              # ← MỚI
│   │   ├── Inventory/             # ← MỚI
│   │   └── ...
│   ├── Interfaces/
│   └── Services/
│       ├── PermissionService.cs
│       ├── AuditService.cs
│       ├── UnitConversionService.cs  # ← MỚI: quy đổi đơn vị
│       └── StockService.cs           # ← MỚI: xử lý tồn kho
│
├── TaskManagement.Domain/           # Domain Layer
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Project.cs
│   │   ├── TaskItem.cs
│   │   ├── Product.cs               # ← MỚI
│   │   ├── ProductImage.cs          # ← MỚI
│   │   ├── ProductCategory.cs       # ← MỚI
│   │   ├── ProductLabel.cs          # ← MỚI
│   │   ├── ProductUnitConversion.cs  # ← MỚI
│   │   ├── ProductAttributeGroup.cs  # ← MỚI
│   │   ├── ProductAttributeValue.cs  # ← MỚI
│   │   ├── ProductVariant.cs        # ← MỚI
│   │   ├── Supplier.cs              # ← MỚI
│   │   ├── Origin.cs                # ← MỚI
│   │   ├── Unit.cs                  # ← MỚI
│   │   ├── Warehouse.cs             # ← MỚI
│   │   ├── StockBalance.cs          # ← MỚI
│   │   ├── StockMovement.cs         # ← MỚI
│   │   ├── ImportReceipt.cs         # ← MỚI
│   │   ├── ImportReceiptLine.cs     # ← MỚI
│   │   ├── ExportReceipt.cs         # ← MỚI
│   │   ├── ExportReceiptLine.cs     # ← MỚI
│   │   ├── TransferReceipt.cs       # ← MỚI
│   │   └── TransferReceiptLine.cs   # ← MỚI
│   ├── Enums/
│   │   ├── TaskStatus.cs
│   │   ├── ProductStatus.cs         # ← MỚI
│   │   ├── ReceiptStatus.cs         # ← MỚI
│   │   └── MovementType.cs          # ← MỚI
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
│   │   └── components/   (button, modal, paginator, avatar, file-uploader, ...)
│   ├── features/
│   │   ├── auth/         (login page)
│   │   ├── dashboard/    (my tasks, overview)
│   │   ├── projects/     (list, create, detail, members)
│   │   ├── tasks/        (list, create, detail, comment, attachment)
│   │   ├── products/     (list, create/edit, detail, images, variants)       # ← MỚI
│   │   ├── categories/   (list, create/edit, tree view)                      # ← MỚI
│   │   ├── suppliers/    (list, create/edit)                                 # ← MỚI
│   │   ├── units/        (list, create/edit)                                 # ← MỚI
│   │   ├── origins/      (list, create/edit)                                 # ← MỚI
│   │   ├── labels/       (list, create/edit)                                 # ← MỚI
│   │   ├── warehouses/   (list, create/edit)                                 # ← MỚI
│   │   ├── inventory/    (import/export/transfer receipts, stock report)     # ← MỚI
│   │   │   ├── import-receipts/   (list, create/edit, confirm, cancel)
│   │   │   ├── export-receipts/   (list, create/edit, confirm, cancel)
│   │   │   ├── transfer-receipts/ (list, create/edit, confirm, cancel)
│   │   │   └── stock-report/      (stock balances, stock movements)
│   │   └── reports/      (work performance reports)
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

### 3.8. Permission Matrix — Module Sản phẩm & Tồn kho

| Hành động | Admin | Inventory Manager | Warehouse Staff | Viewer |
|---|:---:|:---:|:---:|:---:|
| **Sản phẩm** | | | | |
| Xem sản phẩm | ✅ | ✅ | ✅ | ✅ |
| Tạo sản phẩm | ✅ | ✅ | ❌ | ❌ |
| Sửa sản phẩm | ✅ | ✅ | ❌ | ❌ |
| Xóa sản phẩm | ✅ | ✅ | ❌ | ❌ |
| Upload ảnh sản phẩm | ✅ | ✅ | ❌ | ❌ |
| Quản lý biến thể | ✅ | ✅ | ❌ | ❌ |
| **Danh mục** | | | | |
| Xem danh mục | ✅ | ✅ | ✅ | ✅ |
| Tạo/sửa/xóa danh mục | ✅ | ✅ | ❌ | ❌ |
| **Đơn vị tính** | | | | |
| Xem đơn vị tính | ✅ | ✅ | ✅ | ✅ |
| Tạo/sửa/xóa đơn vị tính | ✅ | ✅ | ❌ | ❌ |
| **Nhà cung cấp** | | | | |
| Xem nhà cung cấp | ✅ | ✅ | ✅ | ✅ |
| Tạo/sửa/xóa nhà cung cấp | ✅ | ✅ | ❌ | ❌ |
| **Kho hàng** | | | | |
| Xem kho | ✅ | ✅ | ✅ | ✅ |
| Tạo/sửa/xóa kho | ✅ | ✅ | ❌ | ❌ |
| **Phiếu nhập kho** | | | | |
| Xem phiếu nhập | ✅ | ✅ | ✅ | ✅ |
| Tạo phiếu nhập (Draft) | ✅ | ✅ | ✅ | ❌ |
| Sửa phiếu nhập (Draft) | ✅ | ✅ | ✅ | ❌ |
| Xác nhận phiếu nhập | ✅ | ✅ | ❌ | ❌ |
| Hủy phiếu nhập | ✅ | ✅ | ❌ | ❌ |
| **Phiếu xuất kho** | | | | |
| Xem phiếu xuất | ✅ | ✅ | ✅ | ✅ |
| Tạo phiếu xuất (Draft) | ✅ | ✅ | ✅ | ❌ |
| Sửa phiếu xuất (Draft) | ✅ | ✅ | ✅ | ❌ |
| Xác nhận phiếu xuất | ✅ | ✅ | ❌ | ❌ |
| Hủy phiếu xuất | ✅ | ✅ | ❌ | ❌ |
| **Phiếu chuyển kho** | | | | |
| Xem phiếu chuyển | ✅ | ✅ | ✅ | ✅ |
| Tạo phiếu chuyển (Draft) | ✅ | ✅ | ✅ | ❌ |
| Sửa phiếu chuyển (Draft) | ✅ | ✅ | ✅ | ❌ |
| Xác nhận phiếu chuyển | ✅ | ✅ | ❌ | ❌ |
| Hủy phiếu chuyển | ✅ | ✅ | ❌ | ❌ |
| **Báo cáo tồn kho** | | | | |
| Xem tồn kho | ✅ | ✅ | ✅ | ✅ |
| Xem lịch sử phát sinh | ✅ | ✅ | ✅ | ✅ |
| Export báo cáo | ✅ | ✅ | ❌ | ❌ |
| **Audit Log** | | | | |
| Xem audit log module | ✅ | ✅ | ❌ | ❌ |

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
    → Phase 7 (Advanced Features: Gantt Chart, Task Hierarchy, Dynamic Fields & Work Reports)
    → Phase 8 (Product & Inventory Module)  ← MỚI
        → 8A (Domain + DB: Entities, Migrations, Seed data)
        → 8B (Product API: CRUD, Search, Images, Variants)
        → 8C (Inventory API: Warehouses, Receipts, Stock)
        → 8D (Frontend Angular: Product pages, Inventory pages, Reports)  [song song với 8B-8C]
        → 8E (Testing + QA)
```

---

### PHASE 7 — Advanced Features (Gantt Chart, Task Hierarchy, Dynamic Fields & Work Reports)

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T7.1 | Cập nhật cấu trúc DB (Migration) cho Dynamic Fields và Task hierarchy (ParentTaskId, CompletedAt) | M | P1 | Đã hoàn thành |
| T7.2 | Viết logic nghiệp vụ phát hiện chu trình khép kín (circular dependency) cho Parent-Child task | M | P1 | Đã hoàn thành |
| T7.3 | Xây dựng API và giao diện UI quản lý danh sách Dynamic Fields của Project | L | P1 | Đã hoàn thành |
| T7.4 | Tích hợp Dynamic Fields vào form tạo/sửa Task (validate, load và save giá trị động) | L | P1 | Đã hoàn thành |
| T7.5 | Bổ sung tab Gantt Chart hiển thị dòng thời gian trực quan của các Task | M | P1 | Đã hoàn thành |
| T7.6 | Thiết kế API tổng hợp báo cáo (KPIs, status, priority, assignees, overdue/completed tasks) | L | P1 | Đã hoàn thành |
| T7.7 | Xây dựng giao diện báo cáo "Work Performance & Reports" đẹp mắt, trực quan với các biểu đồ | L | P1 | Đã hoàn thành |
| T7.8 | Tích hợp bộ lọc Dynamic Fields tùy chỉnh và lọc theo khoảng thời gian vào Báo cáo | L | P1 | Đã hoàn thành |
| T7.9 | Viết integration tests cho phân cấp Task và báo cáo hiệu suất | M | P1 | Đã hoàn thành |

---

> [!TIP]
> **Điểm cần chốt trước khi bắt đầu code (UAT & Release):**
> 1. Loại file storage sẽ dùng (Local folder / Azure Blob / AWS S3)? trà lời: dùng local folder (Đã triển khai)
> 2. Kích thước file tối đa và loại file được phép? trả lời: 20mb, file dạng ảnh (Đã triển khai)
> 3. Member có được sửa task của người khác không? trả lời: ko (Đã triển khai)
> 4. Có cần Guest role trong MVP không? trà lời: ko (Đã bỏ qua trong MVP)
> 5. Phiên JWT hết hạn sau bao lâu? trà lời: 15 phút (Đã triển khai với cơ chế Access/Refresh Token)

---

### PHASE 8 — Product & Inventory Module

> [!NOTE]
> Phase 8 là module mới hoàn toàn, được thiết kế như một bounded context riêng trong hệ thống Modular Monolith. Không phá vỡ kiến trúc hiện tại.

#### 8A — Domain & Database

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T8A.1 | Tạo Entity `Unit` (Id, Code, Name, IsActive) | S | P1 | |
| T8A.2 | Tạo Entity `ProductCategory` với ParentId (hỗ trợ cha/con) | M | P1 | Cần check circular reference |
| T8A.3 | Tạo Entity `Origin` | S | P1 | |
| T8A.4 | Tạo Entity `Supplier` | S | P1 | |
| T8A.5 | Tạo Entity `ProductLabel` + `ProductProductLabels` (M2M) | S | P1 | |
| T8A.6 | Tạo Entity `Product` với RowVersion, soft delete, status | M | P1 | |
| T8A.7 | Tạo Entity `ProductImage` (metadata only) | S | P1 | |
| T8A.8 | Tạo Entity `ProductUnitConversion` với CHECK ConversionRate > 0 | M | P1 | |
| T8A.9 | Tạo Entity `ProductSuppliers` (M2M) | S | P1 | |
| T8A.10 | Tạo Entity `ProductAttributeGroup` + `ProductAttributeValue` | M | P1 | Tối đa 2 group/product |
| T8A.11 | Tạo Entity `ProductVariant` với SKU unique | M | P1 | |
| T8A.12 | Tạo Entity `Warehouse` | S | P1 | |
| T8A.13 | Tạo Entity `StockBalance` + `StockMovement` | M | P1 | |
| T8A.14 | Tạo Entity `ImportReceipt` + `ImportReceiptLine` với RowVersion | M | P1 | |
| T8A.15 | Tạo Entity `ExportReceipt` + `ExportReceiptLine` với RowVersion | M | P1 | |
| T8A.16 | Tạo Entity `TransferReceipt` + `TransferReceiptLine` với RowVersion | M | P1 | CHECK From ≠ To |
| T8A.17 | Tạo Enums: `ProductStatus`, `ReceiptStatus`, `MovementType` | S | P1 | |
| T8A.18 | Viết EF Core migrations cho toàn bộ 21 bảng mới | L | P1 | |
| T8A.19 | Tạo indexes trên các cột thường filter (ProductCode, CategoryId, Status, WarehouseId, MovementType) | S | P1 | |
| T8A.20 | Seed data: Đơn vị tính mặc định (Cái, Hộp, Thùng, Kg, G) | S | P1 | |
| T8A.21 | Seed data: Vai trò mới (Inventory Manager, Warehouse Staff, Viewer) | S | P1 | |
| T8A.22 | Viết unit test cho Domain entities (business rules: circular category, variant limits, conversion rate) | M | P2 | |

---

#### 8B — Product API (Backend)

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T8B.1 | CRUD API cho Units | S | P1 | |
| T8B.2 | CRUD API cho ProductCategories (hỗ trợ tree, check circular) | M | P1 | |
| T8B.3 | CRUD API cho Origins | S | P1 | |
| T8B.4 | CRUD API cho ProductLabels | S | P1 | |
| T8B.5 | CRUD API cho Suppliers | S | P1 | |
| T8B.6 | CRUD API cho Products (với RowVersion, soft delete) | L | P1 | |
| T8B.7 | API Upload/Xóa ảnh sản phẩm + đặt ảnh chính | M | P1 | |
| T8B.8 | API Quy đổi đơn vị (ProductUnitConversions CRUD) | M | P1 | |
| T8B.9 | API ProductAttributeGroups + Values (tối đa 2 group) | M | P1 | |
| T8B.10 | API ProductVariants (tạo từ tổ hợp thuộc tính, SKU unique) | L | P1 | |
| T8B.11 | API Search Products nâng cao (`POST /api/products/search`) | L | P1 | Projection DTO, child categories |
| T8B.12 | Implement `UnitConversionService` (quy đổi số lượng và giá) | M | P1 | |
| T8B.13 | Implement `HtmlSanitizer` cho mô tả sản phẩm (chống XSS) | M | P1 | |
| T8B.14 | Tích hợp AuditLog vào tất cả CRUD sản phẩm | M | P1 | |
| T8B.15 | Tích hợp Permission check cho module sản phẩm | M | P1 | |

---

#### 8C — Inventory API (Backend)

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T8C.1 | CRUD API cho Warehouses | S | P1 | |
| T8C.2 | Implement `StockService` (quản lý StockBalance + StockMovement) | L | P1 | Transaction required |
| T8C.3 | API Import Receipts: CRUD + Confirm + Cancel | L | P1 | Confirm → tăng tồn kho |
| T8C.4 | API Export Receipts: CRUD + Confirm + Cancel | L | P1 | Confirm → giảm tồn kho, check âm kho |
| T8C.5 | API Transfer Receipts: CRUD + Confirm + Cancel | L | P1 | From ≠ To, transaction 2 kho |
| T8C.6 | API Stock Balances (xem tồn kho hiện tại, filter, pagination) | M | P1 | |
| T8C.7 | API Stock Movements (lịch sử phát sinh, filter, pagination) | M | P1 | |
| T8C.8 | API Xem tồn kho theo sản phẩm / theo kho | M | P1 | |
| T8C.9 | Logic hủy phiếu đã Confirmed (đảo movement) | L | P1 | |
| T8C.10 | Tích hợp AuditLog vào tất cả CRUD + Confirm/Cancel phiếu kho | M | P1 | |
| T8C.11 | Tích hợp Permission check cho module kho | M | P1 | |
| T8C.12 | Cấu hình `AllowNegativeStock` (mặc định false) | S | P1 | |

---

#### 8D — Frontend Angular Material

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T8D.1 | Cập nhật sidebar menu: thêm Products, Categories, Suppliers, Units, Warehouses, Inventory, Stock Report | S | P1 | |
| T8D.2 | Trang CRUD đơn vị tính (Units) | S | P1 | mat-table + mat-dialog |
| T8D.3 | Trang CRUD danh mục sản phẩm (tree view cha/con) | M | P1 | mat-tree |
| T8D.4 | Trang CRUD xuất xứ (Origins) | S | P1 | |
| T8D.5 | Trang CRUD nhãn sản phẩm (Labels) | S | P1 | mat-chip |
| T8D.6 | Trang CRUD nhà cung cấp (Suppliers) | S | P1 | |
| T8D.7 | Trang danh sách sản phẩm (search, filter, pagination, sort) | L | P1 | mat-table + mat-paginator |
| T8D.8 | Form tạo/sửa sản phẩm (đầy đủ trường + rich text editor + upload ảnh) | L | P1 | mat-tabs, mat-card |
| T8D.9 | Component upload nhiều ảnh + chọn ảnh chính + xóa ảnh | M | P1 | |
| T8D.10 | Component quản lý quy đổi đơn vị theo sản phẩm | M | P1 | |
| T8D.11 | Component quản lý thuộc tính sản phẩm (tối đa 2 nhóm) + tạo variant | L | P1 | |
| T8D.12 | Trang CRUD kho hàng (Warehouses) | S | P1 | |
| T8D.13 | Trang danh sách phiếu nhập kho + form tạo/sửa + confirm/cancel | L | P1 | Thêm nhiều dòng sản phẩm |
| T8D.14 | Trang danh sách phiếu xuất kho + form tạo/sửa + confirm/cancel | L | P1 | |
| T8D.15 | Trang danh sách phiếu chuyển kho + form tạo/sửa + confirm/cancel | L | P1 | |
| T8D.16 | Component chọn product/variant trong dòng phiếu kho | M | P1 | mat-autocomplete |
| T8D.17 | Component tự quy đổi đơn vị + tính thành tiền khi nhập dòng | M | P1 | |
| T8D.18 | Trang báo cáo tồn kho (stock balances + filter) | M | P1 | |
| T8D.19 | Trang lịch sử phát sinh tồn kho (stock movements + filter) | M | P1 | |
| T8D.20 | Disable chỉnh sửa dòng hàng khi phiếu Confirmed | S | P1 | |
| T8D.21 | Hiển thị tồn kho tổng trong danh sách sản phẩm | S | P2 | |

---

#### 8E — Testing & QA (Product + Inventory)

| # | Task | Độ phức tạp | Ưu tiên | Ghi chú |
|---|---|:---:|:---:|---|
| T8E.1 | Unit test: Category circular reference detection | M | P1 | |
| T8E.2 | Unit test: Product variant generation (tổ hợp thuộc tính) | M | P1 | |
| T8E.3 | Unit test: UnitConversionService | M | P1 | |
| T8E.4 | Unit test: StockService (import/export/transfer/adjustment) | L | P1 | |
| T8E.5 | Integration test: CRUD Products với permission check | L | P1 | |
| T8E.6 | Integration test: Import Receipt flow (Draft → Confirm → kiểm tra tồn kho) | L | P1 | |
| T8E.7 | Integration test: Export Receipt flow (check âm kho khi AllowNegativeStock=false) | L | P1 | |
| T8E.8 | Integration test: Transfer Receipt flow (2 kho, transaction) | L | P1 | |
| T8E.9 | Integration test: Cancel confirmed receipt (đảo movement) | M | P1 | |
| T8E.10 | Integration test: Search products (child categories, filters) | M | P1 | |
| T8E.11 | Integration test: AuditLog cho module sản phẩm + kho | M | P2 | |
| T8E.12 | Thực hiện kiểm tra thủ công (UAT) các luồng chính module QLSP + Kho | L | P1 | |

---

> [!TIP]
> **Điểm cần chốt trước khi bắt đầu code (UAT & Release):**
> 1. Loại file storage sẽ dùng (Local folder / Azure Blob / AWS S3)? trà lời: dùng local folder (Đã triển khai)
> 2. Kích thước file tối đa và loại file được phép? trả lời: 20mb, file dạng ảnh (Đã triển khai)
> 3. Member có được sửa task của người khác không? trả lời: ko (Đã triển khai)
> 4. Có cần Guest role trong MVP không? trà lời: ko (Đã bỏ qua trong MVP)
> 5. Phiên JWT hết hạn sau bao lâu? trà lời: 15 phút (Đã triển khai với cơ chế Access/Refresh Token)

> [!IMPORTANT]
> **Điểm cần chốt cho module QLSP + Tồn kho (Phase 8):**
> 1. AllowNegativeStock = true hay false? Đề xuất: false (không cho âm kho)
> 2. Mã sản phẩm tự sinh theo format nào? Ví dụ: SP-000001
> 3. Mã phiếu kho tự sinh theo format nào? Ví dụ: NK-20260713-001, XK-20260713-001, CK-20260713-001
> 4. Ảnh sản phẩm lưu ở đâu? Dùng chung local folder storage hiện tại
> 5. Rich text editor dùng thư viện nào? Đề xuất: ngx-quill hoặc @ckeditor/ckeditor5-angular
> 6. Giới hạn dung lượng ảnh sản phẩm? Đề xuất: 5MB/ảnh, tối đa 10 ảnh/sản phẩm
> 7. Có cần export Excel ở phase này không? Đề xuất: chưa, để phase sau
