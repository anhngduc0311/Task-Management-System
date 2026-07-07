# TÀI LIỆU THIẾT KẾ THỐNG NHẤT  
## Hệ thống Quản lý Công việc / Task Management System

**Phiên bản:** 1.0  
**Vai trò tài liệu:** Single Source of Truth  
**Mục tiêu:** Thống nhất phạm vi, kiến trúc, luồng dữ liệu, giới hạn kỹ thuật và các quyết định nền tảng cho quá trình thiết kế, phát triển, kiểm thử và vận hành hệ thống.

---

# 1. Tổng quan

## 1.1. Mục tiêu hệ thống

Hệ thống được xây dựng nhằm hỗ trợ quản lý công việc theo nhóm, cho phép người dùng tạo project, tạo task, phân công người phụ trách, theo dõi trạng thái, bình luận, đính kèm file và ghi nhận lịch sử thay đổi.

Mục tiêu chính của hệ thống là:

- Giảm phụ thuộc vào Excel hoặc công cụ quản lý thủ công.
- Chuẩn hóa quy trình tạo, giao, theo dõi và hoàn tất công việc.
- Tăng tính minh bạch thông qua phân quyền, lịch sử thay đổi và kiểm soát truy cập.
- Đảm bảo hệ thống có thể mở rộng sau giai đoạn MVP mà không phải viết lại kiến trúc lõi.

## 1.2. Phạm vi MVP

Phiên bản MVP tập trung vào các chức năng cốt lõi:

- Đăng nhập, xác thực người dùng.
- Quản lý người dùng cơ bản.
- Quản lý project.
- Quản lý task.
- Giao task cho thành viên.
- Cập nhật trạng thái task.
- Bình luận trong task.
- Đính kèm file ở mức cơ bản.
- Phân quyền theo vai trò.
- Ghi nhận lịch sử thay đổi quan trọng.
- Tìm kiếm / lọc task cơ bản.
- Dashboard hoặc danh sách task theo project, assignee, status.

## 1.3. Ngoài phạm vi MVP

Các chức năng sau không thuộc phạm vi MVP (ngoại trừ Biểu đồ Gantt đã được triển khai bổ sung ở Phase 7):

- AI gợi ý / chấm điểm task.
- Time tracking phức tạp.
- Workflow automation nâng cao.
- Push notification đa nền tảng.
- Mobile app native.
- Microservices.
- Search engine riêng như Elasticsearch.
- Real-time toàn hệ thống bằng SignalR ở giai đoạn đầu.
- Multi-tenant SaaS phức tạp nếu chưa xác nhận mô hình kinh doanh.

## 1.4. Định hướng sản phẩm

Hệ thống được thiết kế theo hướng **MVP trước, mở rộng sau**. Không nhồi quá nhiều tính năng ngay từ đầu để tránh rủi ro feature creep.

Ưu tiên của MVP:

1. Dễ dùng.
2. Ổn định.
3. Bảo mật quyền truy cập.
4. Dữ liệu rõ ràng, dễ truy vết.
5. Có nền tảng mở rộng cho các giai đoạn sau.

---

# 2. Các quyết định kiến trúc đã khóa

## 2.1. Kiểu kiến trúc tổng thể

Hệ thống sử dụng mô hình:

**Frontend SPA + Backend REST API + SQL Database + File Storage riêng**

Kiến trúc triển khai ban đầu là:

**Modular Monolith**

Không sử dụng microservices trong MVP.

Lý do:

- Giảm độ phức tạp vận hành.
- Dễ phát triển và debug.
- Phù hợp với team nhỏ hoặc giai đoạn sản phẩm ban đầu.
- Vẫn có thể tách module thành service riêng trong tương lai nếu cần.

## 2.2. Frontend

Frontend sử dụng:

- Angular.
- RxJS.
- Angular Material hoặc PrimeNG.

Frontend chịu trách nhiệm:

- Hiển thị giao diện.
- Quản lý state phía client.
- Gọi API backend.
- Xử lý form, validate cơ bản.
- Hiển thị danh sách task, project, comment, attachment.
- Không quyết định quyền bảo mật cuối cùng.

Lưu ý quan trọng:

Frontend có thể ẩn hoặc hiện nút theo quyền, nhưng **mọi kiểm tra quyền bắt buộc phải được thực hiện lại ở Backend**.

## 2.3. Backend

Backend sử dụng:

- .NET Web API.
- Entity Framework Core.
- RESTful API.
- Clean Architecture hoặc N-Tier Architecture.

Backend chịu trách nhiệm:

- Xác thực.
- Phân quyền.
- Xử lý nghiệp vụ.
- Kiểm tra quyền truy cập tài nguyên.
- Ghi audit log.
- Quản lý transaction.
- Giao tiếp với database.
- Giao tiếp với file storage.
- Cung cấp API cho frontend.

## 2.4. Database

Database sử dụng:

- SQL Server hoặc Azure SQL.

Database lưu trữ:

- User.
- Role.
- Project.
- Project Member.
- Task.
- Comment.
- Attachment metadata.
- Audit log.
- Notification metadata nếu có.

Không lưu trực tiếp file nhị phân lớn vào database.

Database chỉ lưu:

- Tên file.
- Loại file.
- Kích thước.
- Đường dẫn hoặc storage key.
- Người upload.
- Thời gian upload.
- Quyền truy cập liên quan.

## 2.5. File Storage

File attachment được lưu bên ngoài database.

Các lựa chọn phù hợp:

- Azure Blob Storage.
- AWS S3.
- Thư mục riêng trên server trong môi trường nhỏ.
- Storage service tương đương.

Nguyên tắc:

- Không lưu file trực tiếp trong SQL Server.
- Không để file public nếu chứa dữ liệu nội bộ.
- Backend phải kiểm tra quyền trước khi cho tải file.
- Có giới hạn loại file và kích thước file.
- Nên dùng private storage và signed URL nếu dữ liệu nhạy cảm.

## 2.6. Real-time

Real-time bằng SignalR **không phải bắt buộc trong MVP**.

Giai đoạn MVP ưu tiên:

- REST API.
- Refresh thủ công.
- Polling nhẹ nếu cần.

SignalR được đưa vào Phase 2 nếu có nhu cầu rõ ràng như:

- Board cập nhật trực tiếp.
- Comment real-time.
- Notification real-time.
- Nhiều người cùng thao tác trên cùng task.
- Trải nghiệm cộng tác thời gian thực là giá trị cốt lõi.

Lý do chưa đưa SignalR vào MVP:

- Tăng độ phức tạp vận hành.
- Cần xử lý connection, reconnect, scale-out, permission thay đổi theo thời gian thực.
- Có thể làm chậm tiến độ MVP nếu chưa thật sự cần.

---

# 3. Kiến trúc hệ thống

## 3.1. Sơ đồ kiến trúc logic

```text
[User Browser]
      |
      v
[Angular SPA]
      |
      | HTTPS / REST API
      v
[.NET Web API]
      |
      | Business Logic
      v
[Application Services]
      |
      +--------------------+
      |                    |
      v                    v
[SQL Server / Azure SQL]  [File Storage]
      |
      v
[Audit Logs / App Data]
```

## 3.2. Các layer chính

### 3.2.1. Presentation Layer

Thành phần:

- Angular Components.
- Pages.
- Forms.
- UI Components.
- Route Guards.
- Client-side state management.

Nhiệm vụ:

- Hiển thị dữ liệu.
- Nhận input từ người dùng.
- Gọi API.
- Hiển thị lỗi.
- Điều hướng giao diện.
- Kiểm tra quyền ở mức UI để cải thiện trải nghiệm.

Giới hạn:

- Không chứa business logic quan trọng.
- Không được tin tưởng dữ liệu từ client.
- Không quyết định quyền cuối cùng.

### 3.2.2. API Layer

Thành phần:

- Controllers.
- Request DTOs.
- Response DTOs.
- API filters.
- Authentication middleware.
- Authorization middleware.

Nhiệm vụ:

- Nhận request từ frontend.
- Validate input.
- Gọi application service.
- Trả response chuẩn hóa.
- Xử lý lỗi ở mức API.

### 3.2.3. Application Layer

Thành phần:

- Use cases.
- Services.
- Command handlers.
- Query handlers.
- Permission services.
- Validation services.

Nhiệm vụ:

- Chứa logic nghiệp vụ chính.
- Kiểm tra quyền theo tài nguyên.
- Điều phối thao tác với database và storage.
- Ghi audit log.
- Quản lý transaction nghiệp vụ.

Ví dụ use case:

- Register (Đăng ký).
- Login / RefreshToken (Xác thực).
- CreateTask / UpdateTask / DeleteTask.
- SetParentTask / RemoveParentTask (Quan hệ Task Cha - Con).
- CreateDynamicField / UpdateDynamicField / DeleteDynamicField (Trường động).
- UpdateTaskDynamicValues (Cập nhật giá trị trường động).
- GetWorkSummaryReport / GetStatusReport / GetPriorityReport / GetAssigneeReport (Báo cáo hiệu suất).
- UpdateTaskStatus.
- AssignTask.
- AddComment.
- UploadAttachment.
- AddProjectMember (bằng Email).
- ChangeUserRole.

### 3.2.4. Domain Layer

Thành phần:

- Entity.
- Value Object.
- Domain rules.
- Enum.
- Business constraints.

Các entity chính:

- User (Bổ sung RefreshToken, RefreshTokenExpiryTime).
- Role.
- Project.
- ProjectMember.
- Task (Bổ sung CompletedAt, ParentTaskId).
- DynamicFieldDefinition (Định nghĩa trường dữ liệu động).
- TaskDynamicFieldValue (Giá trị trường dữ liệu động của task).
- TaskComment.
- TaskAttachment.
- AuditLog.
- Notification.

Domain Layer không phụ thuộc vào database, framework hoặc UI.

### 3.2.5. Infrastructure Layer

Thành phần:

- Entity Framework Core.
- SQL Server implementation.
- File storage implementation.
- Email service nếu có.
- Logging provider.
- External service integrations.

Nhiệm vụ:

- Truy xuất dữ liệu.
- Lưu file.
- Gửi email.
- Ghi log hệ thống.
- Kết nối với dịch vụ bên ngoài.

---

# 4. Mô hình dữ liệu cốt lõi

## 4.1. User

Đại diện cho người dùng hệ thống.

Thông tin chính:

- Id.
- FullName.
- Email.
- PasswordHash hoặc ExternalAuthId.
- Status.
- CreatedAt.
- UpdatedAt.

Lưu ý:

Không lưu mật khẩu plain text. Mật khẩu phải được hash bằng cơ chế an toàn.

## 4.2. Role

Đại diện cho vai trò hệ thống.

Vai trò MVP đề xuất:

- Admin.
- Project Manager.
- Member.
- Guest.

Tuy nhiên, hệ thống cần thiết kế để có thể mở rộng quyền chi tiết hơn theo project hoặc task.

## 4.3. Project

Đại diện cho một không gian quản lý công việc.

Thông tin chính:

- Id.
- Name.
- Description.
- OwnerId.
- Status.
- CreatedAt.
- UpdatedAt.

## 4.4. ProjectMember

Đại diện cho quan hệ giữa user và project.

Thông tin chính:

- ProjectId.
- UserId.
- RoleInProject.
- JoinedAt.
- Status.

Mục tiêu:

Cho phép một user có vai trò khác nhau ở các project khác nhau.

## 4.5. Task

Đại diện cho một công việc.

Thông tin chính:

- Id.
- ProjectId.
- Title.
- Description.
- Status.
- Priority.
- AssigneeId.
- CreatedById.
- DueDate.
- CompletedAt (Ngày hoàn thành thực tế).
- ParentTaskId (Task cha của task hiện tại, cho phép phân cấp công việc).
- CreatedAt.
- UpdatedAt.
- RowVersion hoặc concurrency token (Hỗ trợ optimistic concurrency).

Lưu ý quan trọng về ràng buộc nghiệp vụ:
- Không được phép tạo vòng lặp đệ quy trong quan hệ cha-con (circular dependency).
- Không được phép xóa Task cha nếu còn chứa các Task con chưa hoàn thành (`Todo`/`InProgress`/`InReview`). Khi xóa mềm Task cha thành công, liên kết `ParentTaskId` của các task con sẽ tự động gỡ bỏ (sét thành null).

Lưu ý:

Task nên có concurrency token để tránh ghi đè dữ liệu khi nhiều người cùng chỉnh sửa.

## 4.6. TaskComment

Đại diện cho bình luận trong task.

Thông tin chính:

- Id.
- TaskId.
- UserId.
- Content.
- CreatedAt.
- UpdatedAt.
- IsDeleted.

Lưu ý:

Nếu cho phép rich text hoặc markdown, cần sanitize dữ liệu trước khi hiển thị.

## 4.7. TaskAttachment

Đại diện cho metadata của file đính kèm.

Thông tin chính:

- Id.
- TaskId.
- UploadedById.
- FileName.
- StorageKey.
- ContentType.
- FileSize.
- CreatedAt.
- IsDeleted.

File thật không nằm trong database.

## 4.8. AuditLog

Đại diện cho lịch sử thay đổi.

Thông tin chính:

- Id.
- EntityType.
- EntityId.
- Action.
- ChangedById.
- ChangedAt.
- OldValue.
- NewValue.
- IpAddress.
- UserAgent.

Hệ thống ghi nhận log tự động cho các hành động:

- Tạo task.
- Sửa task (bao gồm thay đổi giá trị trường động).
- Đổi trạng thái.
- Đổi assignee.
- Đổi deadline.
- Thay đổi quan hệ Task cha-con (`TaskParentChanged`, `TaskParentRemoved`).
- Xóa task (`TaskDeleted`).
- Thêm / xóa thành viên project.
- Thay đổi quyền.
- Tạo / sửa / xóa trường dữ liệu động (`DynamicFieldCreated`, `DynamicFieldUpdated`, `DynamicFieldDeleted`).

## 4.9. DynamicFieldDefinition (Bổ sung mới)

Định nghĩa trường dữ liệu động tùy chỉnh cho mỗi dự án.

Thông tin chính:
- Id.
- ProjectId.
- FieldName (Tên hiển thị hiển thị trên giao diện).
- FieldKey (Khóa định danh trường để lưu trữ/truy vấn).
- FieldType (Text, Number, Date, Boolean, Select, MultiSelect).
- IsRequired (Đánh dấu bắt buộc nhập).
- Options (Các tùy chọn có sẵn đối với dạng Select/MultiSelect dưới dạng JSON Array).
- DefaultValue (Giá trị mặc định).
- DisplayOrder (Thứ tự hiển thị trên form).
- IsActive (Trạng thái hoạt động).

## 4.10. TaskDynamicFieldValue (Bổ sung mới)

Lưu trữ giá trị thực tế của các trường động được nhập liệu cho mỗi task.

Thông tin chính:
- TaskId.
- DynamicFieldId.
- FieldValue (Giá trị dạng chuỗi hoặc JSON đối với MultiSelect).

---

# 5. Phân quyền và bảo mật

## 5.1. Nguyên tắc phân quyền

Hệ thống sử dụng RBAC ở mức nền tảng, kết hợp kiểm tra quyền theo tài nguyên.

Không chỉ kiểm tra:

```text
User có role gì?
```

Mà còn phải kiểm tra:

```text
User có quyền thao tác trên project/task cụ thể này không?
```

Ví dụ:

- User là Member trong Project A không có nghĩa là được xem Project B.
- User là Project Manager của Project A không có quyền quản lý Project C.
- Guest có thể xem task nhưng không được chỉnh sửa.
- Assignee có thể đổi trạng thái task nếu chính sách cho phép.
- Admin hệ thống có quyền cao nhất nhưng vẫn cần audit.

## 5.2. Quyền đề xuất cho MVP

| Hành động | Admin | Project Manager | Member | Guest |
|---|---:|---:|---:|---:|
| Tạo project | Có | Có thể | Không | Không |
| Sửa project | Có | Có | Không | Không |
| Xóa project | Có | Có thể giới hạn | Không | Không |
| Thêm thành viên | Có | Có | Không | Không |
| Xem task | Có | Có | Có | Có thể |
| Tạo task | Có | Có | Có | Không |
| Sửa task | Có | Có | Có giới hạn | Không |
| Xóa task | Có | Có | Không | Không |
| Comment | Có | Có | Có | Có thể |
| Upload file | Có | Có | Có thể | Không |
| Xem audit log | Có | Có thể | Không | Không |

Bảng quyền này là mặc định ban đầu và có thể được điều chỉnh theo yêu cầu nghiệp vụ.

## 5.3. Các nguyên tắc bảo mật bắt buộc

- Không lưu password plain text.
- Không tin dữ liệu từ frontend.
- API phải kiểm tra quyền ở từng hành động quan trọng.
- Validate input ở backend.
- Sanitize nội dung comment/description nếu có rich text.
- Không expose file public nếu file chứa dữ liệu nội bộ.
- Không trả dữ liệu nhạy cảm không cần thiết trong API.
- Không cho phép user truy cập project/task không thuộc quyền của họ.
- Ghi audit log cho các thao tác quan trọng.
- Có rate limit cho API nhạy cảm như login, upload file, comment.

---

# 6. Luồng dữ liệu

## 6.1. Luồng đăng nhập

```text
User nhập thông tin đăng nhập
        |
        v
Angular gửi request login
        |
        v
Backend xác thực thông tin
        |
        v
Backend tạo token/session
        |
        v
Frontend lưu token theo cơ chế an toàn
        |
        v
User truy cập hệ thống
```

Backend chịu trách nhiệm xác thực và trả về thông tin người dùng phù hợp.

Frontend chỉ dùng thông tin này để điều hướng và hiển thị UI.

## 6.2. Luồng tạo task

```text
User bấm tạo task
        |
        v
Frontend hiển thị form
        |
        v
User nhập title, description, assignee, due date
        |
        v
Frontend gửi request CreateTask
        |
        v
Backend validate dữ liệu
        |
        v
Backend kiểm tra quyền user trong project
        |
        v
Backend tạo task trong database
        |
        v
Backend ghi audit log
        |
        v
Backend trả task mới về frontend
        |
        v
Frontend cập nhật danh sách task
```

Điểm kiểm soát bắt buộc:

- Project có tồn tại không?
- User có quyền tạo task trong project không?
- Assignee có thuộc project không?
- Due date có hợp lệ không?
- Title có vượt giới hạn độ dài không?

## 6.3. Luồng cập nhật task

```text
User mở task
        |
        v
Frontend tải dữ liệu task
        |
        v
User chỉnh sửa thông tin
        |
        v
Frontend gửi request UpdateTask kèm RowVersion
        |
        v
Backend kiểm tra quyền
        |
        v
Backend kiểm tra concurrency
        |
        v
Backend cập nhật task
        |
        v
Backend ghi audit log
        |
        v
Backend trả dữ liệu mới nhất
        |
        v
Frontend cập nhật UI
```

Nếu có xung đột dữ liệu:

```text
User A và User B cùng mở task
User A lưu trước
User B lưu sau với RowVersion cũ
Backend phát hiện xung đột
Backend trả lỗi conflict
Frontend yêu cầu User B tải lại dữ liệu mới nhất
```

Nguyên tắc:

Không được âm thầm ghi đè dữ liệu của người lưu trước.

## 6.4. Luồng comment task

```text
User nhập comment
        |
        v
Frontend gửi AddComment
        |
        v
Backend kiểm tra user có quyền xem/comment task
        |
        v
Backend validate nội dung
        |
        v
Backend lưu comment
        |
        v
Backend ghi audit log nếu cần
        |
        v
Backend trả comment mới
        |
        v
Frontend hiển thị comment
```

Nếu comment hỗ trợ markdown hoặc rich text, phải sanitize trước khi render.

## 6.5. Luồng upload file

```text
User chọn file
        |
        v
Frontend kiểm tra sơ bộ loại file và kích thước
        |
        v
Frontend gửi file lên backend hoặc nhận upload URL
        |
        v
Backend kiểm tra quyền upload
        |
        v
Backend kiểm tra loại file, kích thước
        |
        v
Backend lưu file vào storage
        |
        v
Backend lưu metadata vào database
        |
        v
Backend ghi audit log
        |
        v
Frontend hiển thị file trong task
```

Nguyên tắc:

- Frontend chỉ kiểm tra sơ bộ.
- Backend phải kiểm tra lại.
- File thật nằm ở storage.
- Database chỉ lưu metadata.
- Tải file phải đi qua kiểm tra quyền.

## 6.6. Luồng tải file

```text
User bấm tải file
        |
        v
Frontend gửi request DownloadAttachment
        |
        v
Backend kiểm tra user có quyền xem task/file
        |
        v
Backend tạo stream hoặc signed URL
        |
        v
User tải file
```

Không cho phép truy cập trực tiếp file nếu chưa kiểm tra quyền.

## 6.7. Luồng audit log

```text
User thực hiện hành động quan trọng
        |
        v
Backend xử lý nghiệp vụ
        |
        v
Backend xác định dữ liệu trước và sau thay đổi
        |
        v
Backend ghi audit log
        |
        v
Audit log được lưu trong database
```

Audit log nên được ghi ở Application Layer hoặc thông qua cơ chế thống nhất, tránh mỗi controller tự ghi rời rạc.

---

# 7. Giới hạn kỹ thuật

## 7.1. Giới hạn về kiến trúc

- MVP không dùng microservices.
- MVP không yêu cầu real-time toàn hệ thống.
- Không sử dụng SignalR nếu chưa có nhu cầu rõ ràng.
- Không thiết kế quá phức tạp trước khi có user feedback.
- Không đưa AI, time tracking nâng cao vào MVP (Biểu đồ Gantt đã được hỗ trợ ở Phase 7).
- Không lưu file lớn trực tiếp trong database (sử dụng thư mục file trên server).

## 7.2. Giới hạn về hiệu năng

Hệ thống cần tránh:

- N+1 Query khi load danh sách task.
- Load toàn bộ task không phân trang.
- Include quá nhiều entity không cần thiết.
- Trả response quá lớn.
- Tìm kiếm bằng query không có index.
- Audit log làm chậm thao tác chính.
- Upload file lớn không giới hạn.

Nguyên tắc kỹ thuật:

- Danh sách task phải phân trang.
- Query phải dùng projection/select DTO phù hợp.
- Các field thường lọc như ProjectId, AssigneeId, Status, DueDate nên có index.
- Không trả comment/attachment đầy đủ trong mọi API list task nếu không cần.
- Dùng `.Select()` thay vì `.Include()` tràn lan khi chỉ cần dữ liệu hiển thị.
- Theo dõi slow query trong môi trường staging/production.

## 7.3. Giới hạn về dữ liệu

Các giới hạn đề xuất cho MVP:

- Title task: tối đa 200 ký tự.
- Description: giới hạn độ dài theo cấu hình.
- Comment: giới hạn độ dài theo cấu hình.
- File upload: giới hạn dung lượng theo cấu hình.
- Số lượng attachment mỗi task: giới hạn theo cấu hình.
- Số lượng project/member/task cần được kiểm soát bằng pagination và index.

Các giới hạn cụ thể cần được chốt trước khi triển khai production.

## 7.4. Giới hạn về bảo mật

- Không có quyền thì không được truy cập dữ liệu, kể cả khi biết ID.
- Không expose ID nhạy cảm nếu không cần.
- Không tin tưởng role gửi từ frontend.
- Không cho phép upload file nguy hiểm.
- Không render HTML từ user nếu chưa sanitize.
- Không lưu secret trong source code.
- Không bỏ qua audit cho các hành động quan trọng.
- Không cho phép admin xóa audit log tùy tiện nếu audit được dùng để truy trách nhiệm.

## 7.5. Giới hạn về vận hành

Hệ thống MVP cần tối thiểu có:

- Logging lỗi backend.
- Logging request quan trọng.
- Health check API.
- Backup database.
- Backup file storage.
- Quy trình restore dữ liệu.
- Cấu hình môi trường tách biệt: Development, Staging, Production.
- CI/CD cơ bản hoặc checklist deploy rõ ràng.
- Không chạy migration production một cách tự động thiếu kiểm soát.

## 7.6. Giới hạn về mở rộng

Hệ thống được thiết kế để mở rộng sau MVP, nhưng không tối ưu quá sớm.

Có thể mở rộng trong tương lai:

- SignalR cho real-time.
- Notification nâng cao.
- Email digest.
- Search engine riêng.
- Mobile app.
- Multi-tenant SaaS.
- Workflow automation.
- Reporting/dashboard nâng cao.
- Integration với calendar, Slack, Teams hoặc GitHub.

Tuy nhiên, các phần này không nên làm trong MVP nếu chưa có nhu cầu nghiệp vụ rõ ràng.

---

# 8. Rủi ro chính và biện pháp kiểm soát

## 8.1. Rủi ro Feature Creep

Mô tả:

Dự án bị kéo dài vì thêm quá nhiều chức năng trước khi có bản dùng được.

Biện pháp:

- Khóa scope MVP.
- Tách rõ Phase 1, Phase 2, Phase 3.
- Mọi tính năng mới phải có lý do nghiệp vụ rõ ràng.
- Ưu tiên feedback thực tế từ người dùng.

## 8.2. Rủi ro phân quyền sai

Mô tả:

Người dùng xem hoặc sửa được dữ liệu không thuộc quyền.

Biện pháp:

- Kiểm tra quyền ở backend.
- Thiết kế ProjectMember rõ ràng.
- Viết test cho các case truy cập trái phép.
- Audit log các thao tác quan trọng.

## 8.3. Rủi ro mất dữ liệu

Mô tả:

Lỗi server, lỗi deploy hoặc lỗi migration làm mất dữ liệu.

Biện pháp:

- Backup DB định kỳ.
- Backup file storage.
- Test restore.
- Backup trước khi migration.
- Không deploy migration nguy hiểm nếu chưa review.

## 8.4. Rủi ro hiệu năng

Mô tả:

Danh sách task load chậm, query nhiều, database nghẽn.

Biện pháp:

- Pagination.
- Index.
- Projection DTO.
- Theo dõi slow query.
- Không Include dữ liệu không cần thiết.
- Không load attachment/comment đầy đủ trong list view.

## 8.5. Rủi ro file attachment

Mô tả:

File quá lớn, file độc hại, file public sai quyền, storage phình nhanh.

Biện pháp:

- Giới hạn kích thước.
- Giới hạn loại file.
- Lưu file ngoài DB.
- Kiểm tra quyền download.
- Dùng private storage.
- Có chính sách xóa/retention.

## 8.6. Rủi ro audit log phình to

Mô tả:

Audit log tăng nhanh làm nặng database.

Biện pháp:

- Chỉ ghi các event quan trọng trong MVP.
- Có index phù hợp.
- Có chính sách archive sau này.
- Không lưu dữ liệu thừa trong audit.

---

# 9. Deployment và môi trường

## 9.1. Môi trường đề xuất

Tối thiểu cần có:

- Development.
- Staging.
- Production.

Development dùng cho lập trình.

Staging dùng để test gần giống production.

Production dùng cho người dùng thật.

## 9.2. Docker

Docker được sử dụng để đóng gói ứng dụng, giúp giảm lỗi khác biệt môi trường.

Các thành phần có thể containerize:

- Frontend.
- Backend API.
- Background worker nếu có.

Database production có thể dùng managed service thay vì tự chạy container để giảm rủi ro vận hành.

## 9.3. CI/CD

CI/CD tối thiểu nên có:

- Build frontend.
- Build backend.
- Run test.
- Check lint nếu có.
- Deploy staging.
- Deploy production sau khi duyệt.

Không nên deploy trực tiếp lên production mà bỏ qua staging.

---

# 10. Các câu hỏi đã được thống nhất và quyết định

Các câu hỏi kiến trúc dưới đây đã được thảo luận và chốt phương án triển khai thực tế:

## 10.1. Sản phẩm

1. **Hệ thống hướng tới đối tượng nào?** -> Hệ thống quản lý nội bộ dành cho các nhóm từ 5-50 người.
2. **Loại hình giao diện?** -> Định hướng Jira-lite (đầy đủ tính năng quản lý công việc và báo cáo chi tiết).
3. **Có cần task cha/con trong MVP không?** -> Có. Đã hỗ trợ phân cấp Task Cha - Con (Parent-Child Hierarchy) đệ quy ngăn ngừa vòng lặp.
4. **Có cần nhiều assignee cho một task không?** -> Không, mỗi task chỉ thuộc về một Assignee duy nhất tại một thời điểm để nâng cao trách nhiệm.
5. **Có cần Guest trong MVP không?** -> Không. Hiện tại chỉ hỗ trợ vai trò hệ thống và vai trò trong dự án (Project Manager, Member).

## 10.2. Phân quyền

1. **Member có được sửa task của người khác không?** -> Không. Member chỉ được sửa mô tả và trạng thái các task do chính họ đảm nhận hoặc tạo ra.
2. **Ai được quyền chỉnh sửa toàn bộ thông tin task?** -> PM và Admin dự án.
3. **Ai được xóa task?** -> PM và Admin có quyền xóa mềm task (với điều kiện các task con của nó đã hoàn thành hoặc hủy).

## 10.3. File đính kèm

1. **Nơi lưu trữ file đính kèm?** -> Sử dụng lưu trữ cục bộ (Local server folder) trong thư mục cấu hình `uploads`.
2. **File tối đa bao nhiêu MB?** -> Tối đa 20 MB/file.
3. **Cho phép loại file nào?** -> Chỉ cho phép các định dạng tệp hình ảnh (.jpg, .jpeg, .png, .gif) để đảm bảo an toàn hệ thống.

## 10.4. Xác thực & Bảo mật

1. **Phiên đăng nhập (Session)?** -> Xác thực qua JWT Token với thời gian hết hạn Access Token là 15 phút.
2. **Cơ chế làm mới phiên?** -> Sử dụng Refresh Token được lưu trữ trong DB và gửi/nhận an toàn phía client.
3. **Audit Log:** Lưu chi tiết các giá trị cũ (OldValue) và mới (NewValue) dưới dạng JSON cùng địa chỉ IP và User Agent của người thao tác.

## 10.5. Real-time

1. **Có bắt buộc real-time không?** -> Không. MVP sử dụng cơ chế thủ công (manual refresh) và polling nhẹ trên giao diện.

---

# 11. Kết luận kiến trúc

Hệ thống sẽ được xây dựng theo hướng:

```text
Angular SPA
    +
.NET Web API
    +
Modular Monolith
    +
SQL Server / Azure SQL
    +
External File Storage
    +
RBAC + Resource-based Permission
    +
Audit Log
    +
Docker + CI/CD cơ bản
```

MVP không ưu tiên microservices, AI hoặc real-time phức tạp. Biểu đồ Gantt và Báo cáo hiệu suất nâng cao (Work Performance & Reports) đã được đưa vào triển khai ở Phase 7.

Trọng tâm kiến trúc là:

- Dễ phát triển.
- Dễ bảo trì.
- Kiểm soát quyền tốt.
- Có audit rõ ràng.
- Tránh phình database vì file.
- Tránh feature creep.
- Có nền tảng mở rộng sang real-time, notification, hoặc SaaS trong tương lai.

Tài liệu này là cơ sở thống nhất cho các bước tiếp theo: thiết kế database, thiết kế API, chia module backend, thiết kế UI flow và lập kế hoạch triển khai MVP.
