using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Librarium.Migrations
{
    /// <inheritdoc />
    public partial class InitiaCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    OtpCode = table.Column<string>(type: "TEXT", nullable: true),
                    OtpExpiry = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StudentEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    BookTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BorrowRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false),
                    P256dh = table.Column<string>(type: "TEXT", nullable: false),
                    Auth = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BorrowRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StudentEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AdminNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shelves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShelfCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ShelfType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    GridCol = table.Column<int>(type: "INTEGER", nullable: false),
                    GridRow = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shelves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OtpCode = table.Column<string>(type: "TEXT", nullable: true),
                    OtpExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtpAttempts = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Genre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ISBN = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Copies = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableCopies = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CoverUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ShelfId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StudentEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AdminNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingRequests_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BookingRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BorrowRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StudentEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowRecords_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BorrowRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Shelves",
                columns: new[] { "Id", "GridCol", "GridRow", "ShelfCode", "ShelfType" },
                values: new object[,]
                {
                    { 1, 3, 1, "A1", "almirah" },
                    { 2, 4, 1, "A2", "almirah" },
                    { 3, 5, 1, "A3", "almirah" },
                    { 4, 6, 1, "A4", "almirah" },
                    { 5, 7, 1, "A5", "almirah" },
                    { 6, 8, 1, "A6", "almirah" },
                    { 7, 9, 1, "A7", "almirah" },
                    { 8, 10, 1, "A8", "almirah" },
                    { 9, 3, 2, "A9", "almirah" },
                    { 10, 4, 2, "A10", "almirah" },
                    { 11, 5, 2, "A11", "almirah" },
                    { 12, 6, 2, "A12", "almirah" },
                    { 13, 7, 2, "A13", "almirah" },
                    { 14, 8, 2, "A14", "almirah" },
                    { 15, 9, 2, "A15", "almirah" },
                    { 16, 10, 2, "A16", "almirah" },
                    { 17, 3, 4, "A17", "almirah" },
                    { 18, 4, 4, "A18", "almirah" },
                    { 19, 5, 4, "A19", "almirah" },
                    { 20, 6, 4, "A20", "almirah" },
                    { 21, 7, 4, "A21", "almirah" },
                    { 22, 8, 4, "A22", "almirah" },
                    { 23, 9, 4, "A23", "almirah" },
                    { 24, 3, 5, "A24", "almirah" },
                    { 25, 4, 5, "A25", "almirah" },
                    { 26, 5, 5, "A26", "almirah" },
                    { 27, 6, 5, "A27", "almirah" },
                    { 28, 7, 5, "A28", "almirah" },
                    { 29, 8, 5, "A29", "almirah" },
                    { 30, 9, 5, "A30", "almirah" },
                    { 31, 3, 10, "A31", "almirah" },
                    { 32, 4, 10, "A32", "almirah" },
                    { 33, 5, 10, "A33", "almirah" },
                    { 34, 6, 10, "A34", "almirah" },
                    { 35, 7, 10, "A35", "almirah" },
                    { 36, 8, 10, "A36", "almirah" },
                    { 37, 1, 4, "A37", "almirah" },
                    { 38, 1, 5, "A38", "almirah" },
                    { 39, 1, 6, "A39", "almirah" },
                    { 40, 1, 7, "A40", "almirah" },
                    { 41, 1, 8, "A41", "almirah" },
                    { 42, 3, 7, "R42", "rack" },
                    { 43, 4, 7, "R43", "rack" },
                    { 44, 5, 7, "R44", "rack" },
                    { 45, 6, 7, "R45", "rack" },
                    { 46, 7, 7, "R46", "rack" },
                    { 47, 3, 8, "R47", "rack" },
                    { 48, 4, 8, "R48", "rack" },
                    { 49, 5, 8, "R49", "rack" },
                    { 50, 6, 8, "R50", "rack" },
                    { 51, 7, 8, "R51", "rack" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_BookId",
                table: "BookingRequests",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_StudentId",
                table: "BookingRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ShelfId",
                table: "Books",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_BookId",
                table: "BorrowRecords",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_StudentId",
                table: "BorrowRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StudentId",
                table: "Notifications",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "BookingRequests");

            migrationBuilder.DropTable(
                name: "BorrowRecords");

            migrationBuilder.DropTable(
                name: "Fines");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Shelves");
        }
    }
}
