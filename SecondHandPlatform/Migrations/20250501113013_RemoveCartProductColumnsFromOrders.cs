using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCartProductColumnsFromOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_orders_cart_CartId", "orders");
            migrationBuilder.DropForeignKey("FK_orders_products_ProductId", "orders");
            migrationBuilder.DropIndex("IX_orders_CartId", "orders");
            migrationBuilder.DropIndex("IX_orders_ProductId", "orders");
            migrationBuilder.DropColumn("CartId", "orders");
            migrationBuilder.DropColumn("ProductId", "orders");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
