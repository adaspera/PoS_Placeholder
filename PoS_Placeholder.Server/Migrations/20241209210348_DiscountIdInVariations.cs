using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoS_Placeholder.Server.Migrations
{
    /// <inheritdoc />
    public partial class DiscountIdInVariations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountId",
                table: "ProductVariations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariations_DiscountId",
                table: "ProductVariations",
                column: "DiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariations_Discounts_DiscountId",
                table: "ProductVariations",
                column: "DiscountId",
                principalTable: "Discounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariations_Discounts_DiscountId",
                table: "ProductVariations");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariations_DiscountId",
                table: "ProductVariations");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                table: "ProductVariations");
        }
    }
}
