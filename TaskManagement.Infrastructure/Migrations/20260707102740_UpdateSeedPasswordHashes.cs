using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedPasswordHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                column: "PasswordHash",
                value: "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222"),
                column: "PasswordHash",
                value: "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333"),
                column: "PasswordHash",
                value: "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"),
                column: "PasswordHash",
                value: "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                column: "PasswordHash",
                value: "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2a98e29a-2454-4fbb-91bc-341aefba6222"),
                column: "PasswordHash",
                value: "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("3f78e7aa-2e45-424a-81a1-f3b17789a333"),
                column: "PasswordHash",
                value: "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"),
                column: "PasswordHash",
                value: "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.");
        }
    }
}
