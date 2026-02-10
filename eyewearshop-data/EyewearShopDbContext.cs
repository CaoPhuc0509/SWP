using eyewearshop_data.Entities;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_data;

public class EyewearShopDbContext : DbContext
{
    public EyewearShopDbContext(DbContextOptions<EyewearShopDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<OrderPrescription> OrderPrescriptions => Set<OrderPrescription>();

    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<RxLensSpecFeature> RxLensSpecFeatures => Set<RxLensSpecFeature>();
    public DbSet<SunglassesSpec> SunglassesSpecs => Set<SunglassesSpec>();
    public DbSet<FrameSpec> FrameSpecs => Set<FrameSpec>();
    public DbSet<RxLensSpec> RxLensSpecs => Set<RxLensSpec>();
    public DbSet<ContactLensSpec> ContactLensSpecs => Set<ContactLensSpec>();

    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnRequestItem> ReturnRequestItems => Set<ReturnRequestItem>();
    public DbSet<ShippingInfo> ShippingInfos => Set<ShippingInfo>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<Combo> Combos => Set<Combo>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            e.Property(x => x.Status).HasColumnName("status");

            e.HasData(
                new Role { RoleId = 1, RoleName = RoleNames.Customer, Description = "Customer", Status = 1 },
                new Role { RoleId = 2, RoleName = RoleNames.SalesSupport, Description = "Sales/Support Staff", Status = 1 },
                new Role { RoleId = 3, RoleName = RoleNames.Operations, Description = "Operations Staff", Status = 1 },
                new Role { RoleId = 4, RoleName = RoleNames.Manager, Description = "Manager", Status = 1 },
                new Role { RoleId = 5, RoleName = RoleNames.Admin, Description = "System Admin", Status = 1 }
            );
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255);
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
            e.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(10);
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId);

            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.RefreshTokenId);

            e.Property(x => x.RefreshTokenId).HasColumnName("refresh_token_id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.Token).HasColumnName("token").HasMaxLength(255).IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");

            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ExpiresAt });

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Brand>(e =>
        {
            e.ToTable("brands");
            e.HasKey(x => x.BrandId);
            e.Property(x => x.BrandId).HasColumnName("brand_id");
            e.Property(x => x.BrandName).HasColumnName("brand_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Status).HasColumnName("status");
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.CategoryName).HasColumnName("category_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Status).HasColumnName("status");
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(255).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.BrandId).HasColumnName("brand_id");
            e.Property(x => x.ProductType).HasColumnName("product_type").HasMaxLength(30).IsRequired();
            e.Property(x => x.BasePrice).HasColumnName("base_price");
            e.Property(x => x.Specifications).HasColumnName("specifications");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.Sku).IsUnique();

            e.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId);

            e.HasOne(x => x.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(x => x.BrandId);
        });

        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.ToTable("product_variants");
            e.HasKey(x => x.VariantId);
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
            
            // Variant-specific attributes
            e.Property(x => x.Color).HasColumnName("color").HasMaxLength(255);
            
            // For RxLens: Refractive index varies by variant
            e.Property(x => x.RefractiveIndex).HasColumnName("refractive_index");
            
            // For Contact Lenses: Base curve and diameter can vary by variant
            e.Property(x => x.BaseCurve).HasColumnName("base_curve");
            e.Property(x => x.Diameter).HasColumnName("diameter");
            
            // Pricing and inventory
            e.Property(x => x.Price).HasColumnName("price");
            e.Property(x => x.StockQuantity).HasColumnName("stock_quantity");
            e.Property(x => x.PreOrderQuantity).HasColumnName("pre_order_quantity");
            e.Property(x => x.ExpectedDateRestock).HasColumnName("expected_date_restock");
            
            // Variant SKU
            e.Property(x => x.VariantSku).HasColumnName("variant_sku").HasMaxLength(255);
            
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.VariantSku);

            e.HasOne(x => x.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_images");
            e.HasKey(x => x.ImageId);
            e.Property(x => x.ImageId).HasColumnName("image_id");
            e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.Url).HasColumnName("url").IsRequired();
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.IsPrimary).HasColumnName("is_primary");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.VariantId);

            e.HasOne(x => x.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Variant)
                .WithMany(v => v.Images)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("carts");
            e.HasKey(x => x.CartId);
            e.Property(x => x.CartId).HasColumnName("cart_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.CustomerId).IsUnique();

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("cart_items");
            e.HasKey(x => x.CartItemId);
            e.Property(x => x.CartItemId).HasColumnName("cart_item_id");
            e.Property(x => x.CartId).HasColumnName("cart_id").IsRequired();
            e.Property(x => x.VariantId).HasColumnName("variant_id").IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity");

            e.HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();

            e.HasOne(x => x.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Variant)
                .WithMany()
                .HasForeignKey(x => x.VariantId);
        });

        modelBuilder.Entity<UserAddress>(e =>
        {
            e.ToTable("user_addresses");
            e.HasKey(x => x.AddressId);
            e.Property(x => x.AddressId).HasColumnName("address_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            e.Property(x => x.RecipientName).HasColumnName("recipient_name").HasMaxLength(255);
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
            e.Property(x => x.AddressLine).HasColumnName("address_line").HasMaxLength(255);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.District).HasColumnName("district").HasMaxLength(100);
            e.Property(x => x.Note).HasColumnName("note");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.OrderId);
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            e.Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(50).IsRequired();
            e.Property(x => x.OrderType).HasColumnName("order_type").HasMaxLength(30).IsRequired();
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.PromotionId).HasColumnName("promotion_id");
            e.Property(x => x.SubTotal).HasColumnName("sub_total");
            e.Property(x => x.ShippingFee).HasColumnName("shipping_fee");
            e.Property(x => x.DiscountAmount).HasColumnName("discount_amount");
            e.Property(x => x.TotalAmount).HasColumnName("total_amount");
            e.Property(x => x.AssignedSaleStaffId).HasColumnName("assigned_sale_staff_id");
            e.Property(x => x.AssignedOpStaffId).HasColumnName("assigned_op_staff_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.PromotionId);
            e.HasIndex(x => x.OrderType);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId);

            e.HasOne(x => x.Promotion)
                .WithMany(p => p.Orders)
                .HasForeignKey(x => x.PromotionId);

            e.HasOne(x => x.ShippingInfo)
                .WithOne(s => s.Order)
                .HasForeignKey<ShippingInfo>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderPrescription>(e =>
        {
            e.ToTable("order_prescriptions");
            e.HasKey(x => x.OrderId);

            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            e.Property(x => x.SavedPrescriptionId).HasColumnName("saved_prescription_id");

            e.Property(x => x.RightSphere).HasColumnName("right_sphere");
            e.Property(x => x.RightCylinder).HasColumnName("right_cylinder");
            e.Property(x => x.RightAxis).HasColumnName("right_axis");
            e.Property(x => x.RightAdd).HasColumnName("right_add");
            e.Property(x => x.RightPD).HasColumnName("right_pd");

            e.Property(x => x.LeftSphere).HasColumnName("left_sphere");
            e.Property(x => x.LeftCylinder).HasColumnName("left_cylinder");
            e.Property(x => x.LeftAxis).HasColumnName("left_axis");
            e.Property(x => x.LeftAdd).HasColumnName("left_add");
            e.Property(x => x.LeftPD).HasColumnName("left_pd");

            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.PrescriptionDate).HasColumnName("prescription_date");
            e.Property(x => x.PrescribedBy).HasColumnName("prescribed_by").HasMaxLength(255);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => x.CustomerId);

            e.HasOne(x => x.Order)
                .WithOne(o => o.OrderPrescription)
                .HasForeignKey<OrderPrescription>(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.OrderItemId);
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id");
            e.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            e.Property(x => x.UnitPrice).HasColumnName("unit_price");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.SubTotal).HasColumnName("sub_total");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.OrderId);

            e.HasOne(x => x.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Variant)
                .WithMany()
                .HasForeignKey(x => x.VariantId);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentId).HasColumnName("payment_id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.PaymentType).HasColumnName("payment_type").HasMaxLength(50);
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Note).HasColumnName("note");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.OrderId);

            e.HasOne(x => x.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.ToTable("payment_transactions");
            e.HasKey(x => x.PaymentTransactionId);

            e.Property(x => x.PaymentTransactionId).HasColumnName("payment_transaction_id");
            e.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
            e.Property(x => x.Gateway).HasColumnName("gateway").HasMaxLength(50).IsRequired();
            e.Property(x => x.GatewayTransactionId).HasColumnName("gateway_transaction_id").HasMaxLength(100);
            e.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(100).IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10);
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.PaidAt).HasColumnName("paid_at");
            e.Property(x => x.RawRequest).HasColumnName("raw_request");
            e.Property(x => x.RawResponse).HasColumnName("raw_response");

            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.RequestId).IsUnique();
            e.HasIndex(x => x.GatewayTransactionId);

            e.HasOne(x => x.Order)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Feature>(e =>
        {
            e.ToTable("features");
            e.HasKey(x => x.FeatureId);
            e.Property(x => x.FeatureId).HasColumnName("feature_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            e.Property(x => x.Status).HasColumnName("status");
        });

        modelBuilder.Entity<SunglassesSpec>(e =>
        {
            e.ToTable("sunglasses_specs");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            
            // Frame specifications
            e.Property(x => x.RimType).HasColumnName("rim_type").HasMaxLength(50);
            e.Property(x => x.Material).HasColumnName("material").HasMaxLength(255);
            e.Property(x => x.A).HasColumnName("a");
            e.Property(x => x.B).HasColumnName("b");
            e.Property(x => x.Dbl).HasColumnName("dbl");
            e.Property(x => x.TempleLength).HasColumnName("temple_length");
            e.Property(x => x.Shape).HasColumnName("shape").HasMaxLength(50);
            e.Property(x => x.Weight).HasColumnName("weight");
            
            // Lens specifications
            e.Property(x => x.LensMaterial).HasColumnName("lens_material").HasMaxLength(255);
            e.Property(x => x.LensType).HasColumnName("lens_type").HasMaxLength(100);
            e.Property(x => x.UvProtection).HasColumnName("uv_protection");
            e.Property(x => x.TintColor).HasColumnName("tint_color").HasMaxLength(100);
            
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Product)
                .WithOne(p => p.SunglassesSpec)
                .HasForeignKey<SunglassesSpec>(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FrameSpec>(e =>
        {
            e.ToTable("frame_specs");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            
            // Frame structure
            e.Property(x => x.RimType).HasColumnName("rim_type").HasMaxLength(50);
            e.Property(x => x.Material).HasColumnName("material").HasMaxLength(255);
            e.Property(x => x.Shape).HasColumnName("shape").HasMaxLength(50);
            e.Property(x => x.Weight).HasColumnName("weight");
            
            // Frame dimensions (at product level)
            e.Property(x => x.A).HasColumnName("a");
            e.Property(x => x.B).HasColumnName("b");
            e.Property(x => x.Dbl).HasColumnName("dbl");
            e.Property(x => x.TempleLength).HasColumnName("temple_length");
            e.Property(x => x.LensWidth).HasColumnName("lens_width");
            
            // Additional specifications
            e.Property(x => x.HingeType).HasColumnName("hinge_type").HasMaxLength(50);
            e.Property(x => x.HasNosePads).HasColumnName("has_nose_pads");
            
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Product)
                .WithOne(p => p.FrameSpec)
                .HasForeignKey<FrameSpec>(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactLensSpec>(e =>
        {
            e.ToTable("contact_lens_specs");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            
            // Physical specifications (at product level)
            e.Property(x => x.BaseCurve).HasColumnName("base_curve");
            e.Property(x => x.Diameter).HasColumnName("diameter");
            
            // Prescription ranges (at product level)
            e.Property(x => x.MinSphere).HasColumnName("min_sphere");
            e.Property(x => x.MaxSphere).HasColumnName("max_sphere");
            e.Property(x => x.MinCylinder).HasColumnName("min_cylinder");
            e.Property(x => x.MaxCylinder).HasColumnName("max_cylinder");
            e.Property(x => x.MinAxis).HasColumnName("min_axis");
            e.Property(x => x.MaxAxis).HasColumnName("max_axis");
            
            // Lens type and material
            e.Property(x => x.LensType).HasColumnName("lens_type").HasMaxLength(50);
            e.Property(x => x.Material).HasColumnName("material").HasMaxLength(255);
            e.Property(x => x.WaterContent).HasColumnName("water_content");
            e.Property(x => x.OxygenPermeability).HasColumnName("oxygen_permeability");
            
            // Usage specifications
            e.Property(x => x.ReplacementSchedule).HasColumnName("replacement_schedule");
            e.Property(x => x.IsToric).HasColumnName("is_toric");
            e.Property(x => x.IsMultifocal).HasColumnName("is_multifocal");
            
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Product)
                .WithOne(p => p.ContactLensSpec)
                .HasForeignKey<ContactLensSpec>(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RxLensSpec>(e =>
        {
            e.ToTable("rx_lens_specs");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            
            // Lens design and material (at product level)
            e.Property(x => x.DesignType).HasColumnName("design_type").HasMaxLength(50);
            e.Property(x => x.Material).HasColumnName("material").HasMaxLength(255);
            e.Property(x => x.LensWidth).HasColumnName("lens_width");
            
            // Prescription ranges (at product level)
            e.Property(x => x.MinSphere).HasColumnName("min_sphere");
            e.Property(x => x.MaxSphere).HasColumnName("max_sphere");
            e.Property(x => x.MinCylinder).HasColumnName("min_cylinder");
            e.Property(x => x.MaxCylinder).HasColumnName("max_cylinder");
            e.Property(x => x.MinAxis).HasColumnName("min_axis");
            e.Property(x => x.MaxAxis).HasColumnName("max_axis");
            e.Property(x => x.MinAdd).HasColumnName("min_add");
            e.Property(x => x.MaxAdd).HasColumnName("max_add");
            
            // Coating options
            e.Property(x => x.HasAntiReflective).HasColumnName("has_anti_reflective");
            e.Property(x => x.HasBlueLightFilter).HasColumnName("has_blue_light_filter");
            e.Property(x => x.HasUVProtection).HasColumnName("has_uv_protection");
            e.Property(x => x.HasScratchResistant).HasColumnName("has_scratch_resistant");
            
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Product)
                .WithOne(p => p.RxLensSpec)
                .HasForeignKey<RxLensSpec>(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RxLensSpecFeature>(e =>
        {
            e.ToTable("rx_lens_spec_features");
            e.HasKey(x => new { x.ProductId, x.FeatureId });

            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.FeatureId).HasColumnName("feature_id");

            e.HasIndex(x => x.FeatureId);

            e.HasOne(x => x.RxLensSpec)
                .WithMany(s => s.RxLensSpecFeatures)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Feature)
                .WithMany(f => f.RxLensSpecFeatures)
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Prescription>(e =>
        {
            e.ToTable("prescriptions");
            e.HasKey(x => x.PrescriptionId);
            e.Property(x => x.PrescriptionId).HasColumnName("prescription_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            
            e.Property(x => x.RightSphere).HasColumnName("right_sphere");
            e.Property(x => x.RightCylinder).HasColumnName("right_cylinder");
            e.Property(x => x.RightAxis).HasColumnName("right_axis");
            e.Property(x => x.RightAdd).HasColumnName("right_add");
            e.Property(x => x.RightPD).HasColumnName("right_pd");
            
            e.Property(x => x.LeftSphere).HasColumnName("left_sphere");
            e.Property(x => x.LeftCylinder).HasColumnName("left_cylinder");
            e.Property(x => x.LeftAxis).HasColumnName("left_axis");
            e.Property(x => x.LeftAdd).HasColumnName("left_add");
            e.Property(x => x.LeftPD).HasColumnName("left_pd");
            
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.PrescriptionDate).HasColumnName("prescription_date");
            e.Property(x => x.PrescribedBy).HasColumnName("prescribed_by").HasMaxLength(255);
            
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.CustomerId);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReturnRequest>(e =>
        {
            e.ToTable("return_requests");
            e.HasKey(x => x.ReturnRequestId);
            e.Property(x => x.ReturnRequestId).HasColumnName("return_request_id");
            e.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
            e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            
            e.Property(x => x.RequestType).HasColumnName("request_type").HasMaxLength(30).IsRequired();
            e.Property(x => x.RequestNumber).HasColumnName("request_number").HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasColumnName("status");
            
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.StaffNotes).HasColumnName("staff_notes");
            
            e.Property(x => x.ExchangeOrderId).HasColumnName("exchange_order_id");
            
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.ExchangeOrderId);

            e.HasOne(x => x.Order)
                .WithMany(o => o.ReturnRequests)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ExchangeOrder)
                .WithMany(o => o.ExchangeOrders)
                .HasForeignKey(x => x.ExchangeOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReturnRequestItem>(e =>
        {
            e.ToTable("return_request_items");
            e.HasKey(x => x.ReturnRequestItemId);
            e.Property(x => x.ReturnRequestItemId).HasColumnName("return_request_item_id");
            e.Property(x => x.ReturnRequestId).HasColumnName("return_request_id").IsRequired();
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id").IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity");

            e.HasIndex(x => x.ReturnRequestId);
            e.HasIndex(x => x.OrderItemId);

            e.HasOne(x => x.ReturnRequest)
                .WithMany(r => r.Items)
                .HasForeignKey(x => x.ReturnRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.OrderItem)
                .WithMany()
                .HasForeignKey(x => x.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShippingInfo>(e =>
        {
            e.ToTable("shipping_infos");
            e.HasKey(x => x.ShippingInfoId);
            e.Property(x => x.ShippingInfoId).HasColumnName("shipping_info_id");
            e.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
            
            e.Property(x => x.RecipientName).HasColumnName("recipient_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
            e.Property(x => x.AddressLine).HasColumnName("address_line").HasMaxLength(255).IsRequired();
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.District).HasColumnName("district").HasMaxLength(100);
            e.Property(x => x.Ward).HasColumnName("ward").HasMaxLength(100);
            e.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            e.Property(x => x.Note).HasColumnName("note");
            
            e.Property(x => x.ShippingMethod).HasColumnName("shipping_method").HasMaxLength(50);
            e.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
            e.Property(x => x.Carrier).HasColumnName("carrier").HasMaxLength(100);
            e.Property(x => x.ShippedAt).HasColumnName("shipped_at");
            e.Property(x => x.EstimatedDeliveryDate).HasColumnName("estimated_delivery_date");
            e.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
            
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.OrderId).IsUnique();
            e.HasIndex(x => x.TrackingNumber);
        });

        modelBuilder.Entity<Promotion>(e =>
        {
            e.ToTable("promotions");
            e.HasKey(x => x.PromotionId);
            e.Property(x => x.PromotionId).HasColumnName("promotion_id");
            e.Property(x => x.PromotionName).HasColumnName("promotion_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            
            e.Property(x => x.PromotionType).HasColumnName("promotion_type").HasMaxLength(50).IsRequired();
            e.Property(x => x.DiscountPercentage).HasColumnName("discount_percentage");
            e.Property(x => x.DiscountAmount).HasColumnName("discount_amount");
            
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            
            e.Property(x => x.MinimumPurchaseAmount).HasColumnName("minimum_purchase_amount");
            e.Property(x => x.MaximumUsagePerCustomer).HasColumnName("maximum_usage_per_customer");
            e.Property(x => x.TotalUsageLimit).HasColumnName("total_usage_limit");
            e.Property(x => x.CurrentUsageCount).HasColumnName("current_usage_count");
            
            e.Property(x => x.PromoCode).HasColumnName("promo_code").HasMaxLength(50);
            
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.PromoCode);
            e.HasIndex(x => new { x.StartDate, x.EndDate });
        });

        modelBuilder.Entity<PromotionProduct>(e =>
        {
            e.ToTable("promotion_products");
            e.HasKey(x => x.PromotionProductId);
            e.Property(x => x.PromotionProductId).HasColumnName("promotion_product_id");
            e.Property(x => x.PromotionId).HasColumnName("promotion_id").IsRequired();
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");

            e.HasIndex(x => x.PromotionId);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.CategoryId);

            e.HasOne(x => x.Promotion)
                .WithMany(p => p.Products)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Combo>(e =>
        {
            e.ToTable("combos");
            e.HasKey(x => x.ComboId);
            e.Property(x => x.ComboId).HasColumnName("combo_id");
            e.Property(x => x.ComboName).HasColumnName("combo_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            
            e.Property(x => x.ComboPrice).HasColumnName("combo_price");
            e.Property(x => x.OriginalPrice).HasColumnName("original_price");
            
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => new { x.StartDate, x.EndDate });
        });

        modelBuilder.Entity<ComboItem>(e =>
        {
            e.ToTable("combo_items");
            e.HasKey(x => x.ComboItemId);
            e.Property(x => x.ComboItemId).HasColumnName("combo_item_id");
            e.Property(x => x.ComboId).HasColumnName("combo_id").IsRequired();
            e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.IsRequired).HasColumnName("is_required");

            e.HasIndex(x => x.ComboId);
            e.HasIndex(x => x.ProductId);

            e.HasOne(x => x.Combo)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.ComboId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
