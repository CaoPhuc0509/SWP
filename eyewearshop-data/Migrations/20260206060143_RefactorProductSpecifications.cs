using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eyewearshop_data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_anti_reflective",
                table: "rx_lens_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_blue_light_filter",
                table: "rx_lens_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_scratch_resistant",
                table: "rx_lens_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_uv_protection",
                table: "rx_lens_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_add",
                table: "rx_lens_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_axis",
                table: "rx_lens_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_add",
                table: "rx_lens_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_axis",
                table: "rx_lens_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_curve",
                table: "product_variants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "diameter",
                table: "product_variants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refractive_index",
                table: "product_variants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_sku",
                table: "product_variants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_nose_pads",
                table: "frame_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hinge_type",
                table: "frame_specs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lens_width",
                table: "frame_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "temple_length",
                table: "frame_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_multifocal",
                table: "contact_lens_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_toric",
                table: "contact_lens_specs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lens_type",
                table: "contact_lens_specs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "material",
                table: "contact_lens_specs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_axis",
                table: "contact_lens_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_axis",
                table: "contact_lens_specs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "oxygen_permeability",
                table: "contact_lens_specs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "replacement_schedule",
                table: "contact_lens_specs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "water_content",
                table: "contact_lens_specs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sunglasses_specs",
                columns: table => new
                {
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    rim_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    material = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    a = table.Column<decimal>(type: "numeric", nullable: true),
                    b = table.Column<decimal>(type: "numeric", nullable: true),
                    dbl = table.Column<decimal>(type: "numeric", nullable: true),
                    temple_length = table.Column<decimal>(type: "numeric", nullable: true),
                    shape = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    weight = table.Column<decimal>(type: "numeric", nullable: true),
                    lens_material = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    lens_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uv_protection = table.Column<int>(type: "integer", nullable: true),
                    tint_color = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunglasses_specs", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_sunglasses_specs_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_variant_sku",
                table: "product_variants",
                column: "variant_sku");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sunglasses_specs");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_variant_sku",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "has_anti_reflective",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "has_blue_light_filter",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "has_scratch_resistant",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "has_uv_protection",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "max_add",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "max_axis",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "min_add",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "min_axis",
                table: "rx_lens_specs");

            migrationBuilder.DropColumn(
                name: "base_curve",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "diameter",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "refractive_index",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "variant_sku",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "has_nose_pads",
                table: "frame_specs");

            migrationBuilder.DropColumn(
                name: "hinge_type",
                table: "frame_specs");

            migrationBuilder.DropColumn(
                name: "lens_width",
                table: "frame_specs");

            migrationBuilder.DropColumn(
                name: "temple_length",
                table: "frame_specs");

            migrationBuilder.DropColumn(
                name: "is_multifocal",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "is_toric",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "lens_type",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "material",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "max_axis",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "min_axis",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "oxygen_permeability",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "replacement_schedule",
                table: "contact_lens_specs");

            migrationBuilder.DropColumn(
                name: "water_content",
                table: "contact_lens_specs");
        }
    }
}
