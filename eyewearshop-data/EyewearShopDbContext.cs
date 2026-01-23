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

    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<FrameSpec> FrameSpecs => Set<FrameSpec>();
    public DbSet<ContactLensSpec> ContactLensSpecs => Set<ContactLensSpec>();
    public DbSet<RxLensSpec> RxLensSpecs => Set<RxLensSpec>();

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
            e.Property(x => x.Color).HasColumnName("color").HasMaxLength(255);
            e.Property(x => x.Price).HasColumnName("price");
            e.Property(x => x.StockQuantity).HasColumnName("stock_quantity");
            e.Property(x => x.PreOrderQuantity).HasColumnName("pre_order_quantity");
            e.Property(x => x.ExpectedDateRestock).HasColumnName("expected_date_restock");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasIndex(x => x.ProductId);

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
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.PrescriptionId).HasColumnName("prescription_id");
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
            e.HasIndex(x => x.PrescriptionId);

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
            e.Property(x => x.Quantity).HasColumnName("quantity");
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

            e.HasIndex(x => x.OrderId);

            e.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId);
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

        modelBuilder.Entity<FrameSpec>(e =>
        {
            e.ToTable("frame_specs");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.RimType).HasColumnName("rim_type").HasMaxLength(50);
            e.Property(x => x.Material).HasColumnName("material").HasMaxLength(255);
            e.Property(x => x.A).HasColumnName("a");
            e.Property(x => x.B).HasColumnName("b");
            e.Property(x => x.Dbl).HasColumnName("dbl");
            e.Property(x => x.Shape).HasColumnName("shape").HasMaxLength(50);
            e.Property(x => x.Weight).HasColumnName("weight");
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
            e.Property(x => x.MinSphere).HasColumnName("min_sphere");
            e.Property(x => x.MaxSphere).HasColumnName("max_sphere");
            e.Property(x => x.MinCylinder).HasColumnName("min_cylinder");
            e.Property(x => x.MaxCylinder).HasColumnName("max_cylinder");
            e.Property(x => x.BaseCurve).HasColumnName("base_curve");
            e.Property(x => x.Diameter).HasColumnName("diameter");
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
            e.Property(x => x.DesignType).HasColumnName("design_type").HasMaxLength(50);
            e.Property(x => x.Material).HasColumnName("material").HasMaxLength(255);
            e.Property(x => x.LensWidth).HasColumnName("lens_width");
            e.Property(x => x.MinSphere).HasColumnName("min_sphere");
            e.Property(x => x.MaxSphere).HasColumnName("max_sphere");
            e.Property(x => x.MinCylinder).HasColumnName("min_cylinder");
            e.Property(x => x.MaxCylinder).HasColumnName("max_cylinder");
            e.Property(x => x.FeatureId).HasColumnName("feature_id");
            e.Property(x => x.Status).HasColumnName("status");

            e.HasOne(x => x.Feature)
                .WithMany(f => f.RxLensSpecs)
                .HasForeignKey(x => x.FeatureId);

            e.HasOne(x => x.Product)
                .WithOne(p => p.RxLensSpec)
                .HasForeignKey<RxLensSpec>(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
