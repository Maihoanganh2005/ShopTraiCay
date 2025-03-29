using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FruitShop1.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedDeliveryDateToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDate",
                table: "Orders");
        }
    }
}
