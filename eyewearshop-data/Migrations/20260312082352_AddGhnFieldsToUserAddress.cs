using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eyewearshop_data.Migrations
{
    /// <inheritdoc />
    public partial class AddGhnFieldsToUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GhnDistrictId",
                table: "user_addresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnWardCode",
                table: "user_addresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "user_addresses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhnDistrictId",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "GhnWardCode",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "user_addresses");
        }
    }
}
