using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eyewearshop_data.Migrations
{
    /// <inheritdoc />
    public partial class OrderPrescriptionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_prescriptions",
                columns: table => new
                {
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    saved_prescription_id = table.Column<long>(type: "bigint", nullable: true),
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_prescriptions", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_prescriptions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_prescriptions_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_prescriptions_customer_id",
                table: "order_prescriptions",
                column: "customer_id");

            // Migrate existing orders.prescription_id into order_prescriptions as a snapshot
            migrationBuilder.Sql(@"
INSERT INTO order_prescriptions (
    order_id,
    customer_id,
    saved_prescription_id,
    right_sphere,
    right_cylinder,
    right_axis,
    right_add,
    right_pd,
    left_sphere,
    left_cylinder,
    left_axis,
    left_add,
    left_pd,
    notes,
    prescription_date,
    prescribed_by,
    created_at
)
SELECT
    o.order_id,
    o.customer_id,
    o.prescription_id,
    p.right_sphere,
    p.right_cylinder,
    p.right_axis,
    p.right_add,
    p.right_pd,
    p.left_sphere,
    p.left_cylinder,
    p.left_axis,
    p.left_add,
    p.left_pd,
    p.notes,
    p.prescription_date,
    p.prescribed_by,
    o.created_at
FROM orders o
JOIN prescriptions p ON p.prescription_id = o.prescription_id
WHERE o.prescription_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM order_prescriptions op WHERE op.order_id = o.order_id);
");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_prescriptions_prescription_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_prescription_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "prescription_id",
                table: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_prescriptions");

            migrationBuilder.AddColumn<long>(
                name: "prescription_id",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_prescription_id",
                table: "orders",
                column: "prescription_id");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_prescriptions_prescription_id",
                table: "orders",
                column: "prescription_id",
                principalTable: "prescriptions",
                principalColumn: "prescription_id");
        }
    }
}
