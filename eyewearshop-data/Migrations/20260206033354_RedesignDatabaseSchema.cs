using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace eyewearshop_data.Migrations
{
    /// <inheritdoc />
    public partial class RedesignDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "order_type",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AVAILABLE");
            
            // Update existing orders to have AVAILABLE type
            migrationBuilder.Sql("UPDATE orders SET order_type = 'AVAILABLE' WHERE order_type = '' OR order_type IS NULL;");

            migrationBuilder.AddColumn<long>(
                name: "promotion_id",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sub_total",
                table: "order_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "order_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "combos",
                columns: table => new
                {
                    combo_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    combo_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    combo_price = table.Column<decimal>(type: "numeric", nullable: false),
                    original_price = table.Column<decimal>(type: "numeric", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combos", x => x.combo_id);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    prescription_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    right_sphere = table.Column<decimal>(type: "numeric", nullable: true),
                    right_cylinder = table.Column<decimal>(type: "numeric", nullable: true),
                    right_axis = table.Column<decimal>(type: "numeric", nullable: true),
                    right_add = table.Column<decimal>(type: "numeric", nullable: true),
                    right_pd = table.Column<decimal>(type: "numeric", nullable: true),
                    left_sphere = table.Column<decimal>(type: "numeric", nullable: true),
                    left_cylinder = table.Column<decimal>(type: "numeric", nullable: true),
                    left_axis = table.Column<decimal>(type: "numeric", nullable: true),
                    left_add = table.Column<decimal>(type: "numeric", nullable: true),
                    left_pd = table.Column<decimal>(type: "numeric", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    prescription_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    prescribed_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescriptions", x => x.prescription_id);
                    table.ForeignKey(
                        name: "FK_prescriptions_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    promotion_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    promotion_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    promotion_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    minimum_purchase_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    maximum_usage_per_customer = table.Column<int>(type: "integer", nullable: true),
                    total_usage_limit = table.Column<int>(type: "integer", nullable: true),
                    current_usage_count = table.Column<int>(type: "integer", nullable: false),
                    promo_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.promotion_id);
                });

            migrationBuilder.CreateTable(
                name: "return_requests",
                columns: table => new
                {
                    return_request_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    request_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    request_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    staff_notes = table.Column<string>(type: "text", nullable: true),
                    exchange_order_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_return_requests", x => x.return_request_id);
                    table.ForeignKey(
                        name: "FK_return_requests_orders_exchange_order_id",
                        column: x => x.exchange_order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_return_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_return_requests_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipping_infos",
                columns: table => new
                {
                    shipping_info_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address_line = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    shipping_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimated_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_infos", x => x.shipping_info_id);
                    table.ForeignKey(
                        name: "FK_shipping_infos_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "combo_items",
                columns: table => new
                {
                    combo_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    combo_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combo_items", x => x.combo_item_id);
                    table.ForeignKey(
                        name: "FK_combo_items_combos_combo_id",
                        column: x => x.combo_id,
                        principalTable: "combos",
                        principalColumn: "combo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_combo_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_products",
                columns: table => new
                {
                    promotion_product_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    promotion_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    category_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_products", x => x.promotion_product_id);
                    table.ForeignKey(
                        name: "FK_promotion_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_promotion_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_promotion_products_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "promotion_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "return_request_items",
                columns: table => new
                {
                    return_request_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    return_request_id = table.Column<long>(type: "bigint", nullable: false),
                    order_item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_return_request_items", x => x.return_request_item_id);
                    table.ForeignKey(
                        name: "FK_return_request_items_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "order_item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_return_request_items_return_requests_return_request_id",
                        column: x => x.return_request_id,
                        principalTable: "return_requests",
                        principalColumn: "return_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_type",
                table: "orders",
                column: "order_type");

            migrationBuilder.CreateIndex(
                name: "IX_orders_promotion_id",
                table: "orders",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_items_combo_id",
                table: "combo_items",
                column: "combo_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_items_product_id",
                table: "combo_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_combos_start_date_end_date",
                table: "combos",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_customer_id",
                table: "prescriptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_products_category_id",
                table: "promotion_products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_products_product_id",
                table: "promotion_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_products_promotion_id",
                table: "promotion_products",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_promo_code",
                table: "promotions",
                column: "promo_code");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_start_date_end_date",
                table: "promotions",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_return_request_items_order_item_id",
                table: "return_request_items",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_request_items_return_request_id",
                table: "return_request_items",
                column: "return_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_requests_customer_id",
                table: "return_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_requests_exchange_order_id",
                table: "return_requests",
                column: "exchange_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_requests_order_id",
                table: "return_requests",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_requests_request_number",
                table: "return_requests",
                column: "request_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_infos_order_id",
                table: "shipping_infos",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_infos_tracking_number",
                table: "shipping_infos",
                column: "tracking_number");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_prescriptions_prescription_id",
                table: "orders",
                column: "prescription_id",
                principalTable: "prescriptions",
                principalColumn: "prescription_id");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_promotions_promotion_id",
                table: "orders",
                column: "promotion_id",
                principalTable: "promotions",
                principalColumn: "promotion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_prescriptions_prescription_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_promotions_promotion_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "combo_items");

            migrationBuilder.DropTable(
                name: "prescriptions");

            migrationBuilder.DropTable(
                name: "promotion_products");

            migrationBuilder.DropTable(
                name: "return_request_items");

            migrationBuilder.DropTable(
                name: "shipping_infos");

            migrationBuilder.DropTable(
                name: "combos");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropTable(
                name: "return_requests");

            migrationBuilder.DropIndex(
                name: "IX_orders_order_type",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_promotion_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "order_type",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "promotion_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "sub_total",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "order_items");
        }
    }
}
