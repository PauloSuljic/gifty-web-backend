using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gifty.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderToWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Wishlists",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Wishlists");
        }
    }
}
