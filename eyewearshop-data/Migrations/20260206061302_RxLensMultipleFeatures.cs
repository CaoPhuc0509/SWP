using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eyewearshop_data.Migrations
{
    /// <inheritdoc />
    public partial class RxLensMultipleFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rx_lens_specs_features_feature_id",
                table: "rx_lens_specs");

            migrationBuilder.DropIndex(
                name: "IX_rx_lens_specs_feature_id",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "feature_id",
                table: "rx_lens_specs");

            migrationBuilder.CreateTable(
                name: "rx_lens_spec_features",
                columns: table => new
                {
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    feature_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rx_lens_spec_features", x => new { x.product_id, x.feature_id });
                    table.ForeignKey(
                        name: "FK_rx_lens_spec_features_features_feature_id",
                        column: x => x.feature_id,
                        principalTable: "features",
                        principalColumn: "feature_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rx_lens_spec_features_rx_lens_specs_product_id",
                        column: x => x.product_id,
                        principalTable: "rx_lens_specs",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rx_lens_spec_features_feature_id",
                table: "rx_lens_spec_features",
                column: "feature_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rx_lens_spec_features");

            migrationBuilder.AddColumn<long>(
                name: "feature_id",
                table: "rx_lens_specs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_rx_lens_specs_feature_id",
                table: "rx_lens_specs",
                column: "feature_id");

            migrationBuilder.AddForeignKey(
                name: "FK_rx_lens_specs_features_feature_id",
                table: "rx_lens_specs",
                column: "feature_id",
                principalTable: "features",
                principalColumn: "feature_id");
        }
    }
}
