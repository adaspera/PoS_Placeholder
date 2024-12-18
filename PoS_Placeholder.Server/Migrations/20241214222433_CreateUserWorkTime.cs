using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoS_Placeholder.Server.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserWorkTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserWorkTimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BreakStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    BreakEnd = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWorkTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWorkTimes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkTimes_UserId",
                table: "UserWorkTimes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserWorkTimes");
        }
    }
}
