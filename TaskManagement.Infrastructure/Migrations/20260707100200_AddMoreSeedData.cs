using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "Email", "ExternalAuthId", "FullName", "PasswordHash", "RefreshToken", "RefreshTokenExpiryTime", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), null, new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "pm@taskmanagement.com", null, "Project Manager User", "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.", null, null, "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222"), null, new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "member@taskmanagement.com", null, "Regular Member User", "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.", null, null, "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333"), null, new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "guest@taskmanagement.com", null, "Guest User", "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.", null, null, "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "OwnerId", "Status", "UpdatedAt" },
                values: new object[] { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Phát triển nền tảng ứng dụng Quản lý công việc cốt lõi của doanh nghiệp.", "Core Platform Development", new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { 2, new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111") },
                    { 3, new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222") },
                    { 4, new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333") }
                });

            migrationBuilder.InsertData(
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId", "JoinedAt", "RoleInProject", "Status" },
                values: new object[,]
                {
                    { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "ProjectManager", "Active" },
                    { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Member", "Active" },
                    { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Guest", "Active" }
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "AssigneeId", "CreatedAt", "CreatedById", "Description", "DueDate", "IsDeleted", "Priority", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a11e11a1-1111-1111-1111-111111111111"), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), "Thiết kế thực thể ERD, các cấu hình bảo mật CORS, Headers, và Phân quyền API.", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), false, "Critical", new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), "Done", "Thiết kế cơ sở dữ liệu và bảo mật", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b22e22b2-2222-2222-2222-222222222222"), new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), "Phát triển giao diện Angular hiển thị biểu đồ danh sách công việc, phân tích trạng thái.", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), false, "High", new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), "InProgress", "Xây dựng màn hình Dashboard trực quan", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c33e33c3-3333-3333-3333-333333333333"), new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), "Cập nhật Swagger OpenAPI để tự động hóa tài liệu cho các endpoint.", new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), false, "Medium", new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), "Todo", "Viết tài liệu API & tích hợp Swagger", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "TaskComments",
                columns: new[] { "Id", "Content", "CreatedAt", "IsDeleted", "TaskId", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("a01c01a0-1111-1111-1111-111111111111"), "Hãy sử dụng các biểu đồ HSL và hiệu ứng hover mượt mà cho Dashboard nhé!", new DateTime(2026, 7, 7, 0, 5, 0, 0, DateTimeKind.Utc), false, new Guid("b22e22b2-2222-2222-2222-222222222222"), new DateTime(2026, 7, 7, 0, 5, 0, 0, DateTimeKind.Utc), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111") },
                    { new Guid("b02c02b0-2222-2222-2222-222222222222"), "Dạ, em đang phát triển giao diện theo thiết kế glassmorphic mượt mà.", new DateTime(2026, 7, 7, 0, 10, 0, 0, DateTimeKind.Utc), false, new Guid("b22e22b2-2222-2222-2222-222222222222"), new DateTime(2026, 7, 7, 0, 10, 0, 0, DateTimeKind.Utc), new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProjectMembers",
                keyColumns: new[] { "ProjectId", "UserId" },
                keyValues: new object[] { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111") });

            migrationBuilder.DeleteData(
                table: "ProjectMembers",
                keyColumns: new[] { "ProjectId", "UserId" },
                keyValues: new object[] { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222") });

            migrationBuilder.DeleteData(
                table: "ProjectMembers",
                keyColumns: new[] { "ProjectId", "UserId" },
                keyValues: new object[] { new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"), new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333") });

            migrationBuilder.DeleteData(
                table: "TaskComments",
                keyColumn: "Id",
                keyValue: new Guid("a01c01a0-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "TaskComments",
                keyColumn: "Id",
                keyValue: new Guid("b02c02b0-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("a11e11a1-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("c33e33c3-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111") });

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 3, new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222") });

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 4, new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333") });

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("b22e22b2-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("c7a52f44-8842-45e6-bd51-24ff43521234"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"));
        }
    }
}
