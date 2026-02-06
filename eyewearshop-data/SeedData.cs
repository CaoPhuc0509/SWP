using eyewearshop_data.Entities;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_data;

public static class SeedData
{
    public static async Task SeedAsync(EyewearShopDbContext db)
    {
        if (await db.Products.AnyAsync())
        {
            return; // Data already seeded
        }

        var now = DateTime.UtcNow;

        // Seed Brands
        var rayBan = new Brand { BrandName = "Ray-Ban", Status = 1 };
        var oakley = new Brand { BrandName = "Oakley", Status = 1 };
        var gucci = new Brand { BrandName = "Gucci", Status = 1 };
        var tomFord = new Brand { BrandName = "Tom Ford", Status = 1 };
        var essilor = new Brand { BrandName = "Essilor", Status = 1 };
        var zeiss = new Brand { BrandName = "Zeiss", Status = 1 };

        db.Brands.AddRange(rayBan, oakley, gucci, tomFord, essilor, zeiss);
        await db.SaveChangesAsync();

        // Seed Categories
        var sunglasses = new Category { CategoryName = "Sunglasses", Status = 1 };
        var eyeglasses = new Category { CategoryName = "Eyeglasses", Status = 1 };
        var readingGlasses = new Category { CategoryName = "Reading Glasses", Status = 1 };
        var contactLenses = new Category { CategoryName = "Contact Lenses", Status = 1 };

        db.Categories.AddRange(sunglasses, eyeglasses, readingGlasses, contactLenses);
        await db.SaveChangesAsync();

        // Seed Features
        var antiReflective = new Feature { Name = "Anti-Reflective", Description = "Reduces glare and reflections", Status = 1 };
        var blueLight = new Feature { Name = "Blue Light Filter", Description = "Filters harmful blue light", Status = 1 };
        var photochromic = new Feature { Name = "Photochromic", Description = "Adaptive lenses that darken in sunlight", Status = 1 };
        var progressive = new Feature { Name = "Progressive", Description = "Multi-focal lenses for all distances", Status = 1 };

        db.Features.AddRange(antiReflective, blueLight, photochromic, progressive);
        await db.SaveChangesAsync();

        // Seed Sunglasses Products
        var aviator = new Product
        {
            ProductName = "Ray-Ban Aviator Classic",
            Sku = "RB-AV-001",
            Description = "Classic aviator sunglasses with timeless design",
            CategoryId = sunglasses.CategoryId,
            BrandId = rayBan.BrandId,
            ProductType = ProductTypes.Sunglasses,
            BasePrice = 150000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var wayfarer = new Product
        {
            ProductName = "Ray-Ban Wayfarer",
            Sku = "RB-WF-001",
            Description = "Iconic wayfarer sunglasses design",
            CategoryId = sunglasses.CategoryId,
            BrandId = rayBan.BrandId,
            ProductType = ProductTypes.Sunglasses,
            BasePrice = 180000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var gucciFrame = new Product
        {
            ProductName = "Gucci GG0061S",
            Sku = "GU-GG-001",
            Description = "Luxury acetate frame with Gucci logo",
            CategoryId = eyeglasses.CategoryId,
            BrandId = gucci.BrandId,
            ProductType = ProductTypes.Frame,
            BasePrice = 3500000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var oakleyFrame = new Product
        {
            ProductName = "Oakley Holbrook",
            Sku = "OK-HB-001",
            Description = "Sporty frame with durable O Matter material",
            CategoryId = eyeglasses.CategoryId,
            BrandId = oakley.BrandId,
            ProductType = ProductTypes.Frame,
            BasePrice = 2200000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.Products.AddRange(aviator, wayfarer, gucciFrame, oakleyFrame);
        await db.SaveChangesAsync();

        // Seed Frame Specs (only for actual frames, not sunglasses)
        db.FrameSpecs.AddRange(
            new FrameSpec
            {
                ProductId = gucciFrame.ProductId,
                RimType = "Full Rim",
                Material = "Acetate",
                A = 52m,
                B = 56m,
                Dbl = 20m,
                TempleLength = 150m,
                LensWidth = 140m,
                Shape = "Rectangle",
                Weight = 30m,
                HingeType = "Spring Hinge",
                HasNosePads = true,
                Status = 1
            },
            new FrameSpec
            {
                ProductId = oakleyFrame.ProductId,
                RimType = "Full Rim",
                Material = "O Matter",
                A = 59m,
                B = 58m,
                Dbl = 19m,
                TempleLength = 145m,
                LensWidth = 142m,
                Shape = "Rectangle",
                Weight = 24m,
                HingeType = "Standard",
                HasNosePads = false,
                Status = 1
            }
        );
        await db.SaveChangesAsync();

        // Seed Product Variants
        var aviatorBlack = new ProductVariant
        {
            ProductId = aviator.ProductId,
            Color = "Black",
            Price = 150000m,
            StockQuantity = 50,
            PreOrderQuantity = 20,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var aviatorGold = new ProductVariant
        {
            ProductId = aviator.ProductId,
            Color = "Gold",
            Price = 160000m,
            StockQuantity = 30,
            PreOrderQuantity = 10,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var wayfarerBlack = new ProductVariant
        {
            ProductId = wayfarer.ProductId,
            Color = "Black",
            Price = 180000m,
            StockQuantity = 40,
            PreOrderQuantity = 15,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var wayfarerTortoise = new ProductVariant
        {
            ProductId = wayfarer.ProductId,
            Color = "Tortoise",
            Price = 190000m,
            StockQuantity = 25,
            PreOrderQuantity = 10,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var gucciBlack = new ProductVariant
        {
            ProductId = gucciFrame.ProductId,
            Color = "Black",
            Price = 3500000m,
            StockQuantity = 15,
            PreOrderQuantity = 5,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var gucciBrown = new ProductVariant
        {
            ProductId = gucciFrame.ProductId,
            Color = "Brown",
            Price = 3600000m,
            StockQuantity = 10,
            PreOrderQuantity = 5,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var oakleyBlack = new ProductVariant
        {
            ProductId = oakleyFrame.ProductId,
            Color = "Black",
            Price = 2200000m,
            StockQuantity = 35,
            PreOrderQuantity = 15,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var oakleyMatte = new ProductVariant
        {
            ProductId = oakleyFrame.ProductId,
            Color = "Matte Black",
            Price = 2300000m,
            StockQuantity = 20,
            PreOrderQuantity = 10,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.ProductVariants.AddRange(
            aviatorBlack, aviatorGold, wayfarerBlack, wayfarerTortoise,
            gucciBlack, gucciBrown, oakleyBlack, oakleyMatte
        );
        await db.SaveChangesAsync();

        // Seed Product Images
        db.ProductImages.AddRange(
            new ProductImage { ProductId = aviator.ProductId, VariantId = aviatorBlack.VariantId, Url = "https://example.com/images/rb-aviator-black-1.jpg", SortOrder = 1, IsPrimary = true, Status = 1 },
            new ProductImage { ProductId = aviator.ProductId, VariantId = aviatorBlack.VariantId, Url = "https://example.com/images/rb-aviator-black-2.jpg", SortOrder = 2, IsPrimary = false, Status = 1 },
            new ProductImage { ProductId = aviator.ProductId, VariantId = aviatorGold.VariantId, Url = "https://example.com/images/rb-aviator-gold-1.jpg", SortOrder = 1, IsPrimary = true, Status = 1 },
            new ProductImage { ProductId = wayfarer.ProductId, VariantId = wayfarerBlack.VariantId, Url = "https://example.com/images/rb-wayfarer-black-1.jpg", SortOrder = 1, IsPrimary = true, Status = 1 },
            new ProductImage { ProductId = wayfarer.ProductId, VariantId = wayfarerTortoise.VariantId, Url = "https://example.com/images/rb-wayfarer-tortoise-1.jpg", SortOrder = 1, IsPrimary = true, Status = 1 },
            new ProductImage { ProductId = gucciFrame.ProductId, VariantId = gucciBlack.VariantId, Url = "https://example.com/images/gucci-black-1.jpg", SortOrder = 1, IsPrimary = true, Status = 1 },
            new ProductImage { ProductId = oakleyFrame.ProductId, VariantId = oakleyBlack.VariantId, Url = "https://example.com/images/oakley-black-1.jpg", SortOrder = 1, IsPrimary = true, Status = 1 }
        );
        await db.SaveChangesAsync();

        // Seed Rx Lens Products
        var essilorLens = new Product
        {
            ProductName = "Essilor Varilux Comfort",
            Sku = "ES-VC-001",
            Description = "Progressive lens with anti-reflective coating",
            CategoryId = eyeglasses.CategoryId,
            BrandId = essilor.BrandId,
            ProductType = ProductTypes.RxLens,
            BasePrice = 2500000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var zeissLens = new Product
        {
            ProductName = "Zeiss Digital Individual",
            Sku = "ZS-DI-001",
            Description = "Premium digital lens with blue light protection",
            CategoryId = eyeglasses.CategoryId,
            BrandId = zeiss.BrandId,
            ProductType = ProductTypes.RxLens,
            BasePrice = 3000000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.Products.AddRange(essilorLens, zeissLens);
        await db.SaveChangesAsync();

        // Seed Rx Lens Specs
        db.RxLensSpecs.AddRange(
            new RxLensSpec
            {
                ProductId = essilorLens.ProductId,
                DesignType = "Progressive",
                Material = "Polycarbonate",
                LensWidth = 70m,
                MinSphere = -10m,
                MaxSphere = 6m,
                MinCylinder = -4m,
                MaxCylinder = 4m,
                MinAxis = 0m,
                MaxAxis = 180m,
                MinAdd = 0.75m,
                MaxAdd = 3.5m,
                HasAntiReflective = true,
                HasBlueLightFilter = true,
                HasUVProtection = true,
                HasScratchResistant = true,
                Status = 1
            },
            new RxLensSpec
            {
                ProductId = zeissLens.ProductId,
                DesignType = "Single Vision",
                Material = "High Index",
                LensWidth = 75m,
                MinSphere = -12m,
                MaxSphere = 8m,
                MinCylinder = -6m,
                MaxCylinder = 6m,
                MinAxis = 0m,
                MaxAxis = 180m,
                HasAntiReflective = true,
                HasBlueLightFilter = true,
                HasUVProtection = true,
                HasScratchResistant = true,
                Status = 1
            }
        );
        await db.SaveChangesAsync();

        // Seed Rx Lens Features (many-to-many)
        db.RxLensSpecFeatures.AddRange(
            // Essilor: progressive + anti-reflective + blue light
            new RxLensSpecFeature { ProductId = essilorLens.ProductId, FeatureId = progressive.FeatureId },
            new RxLensSpecFeature { ProductId = essilorLens.ProductId, FeatureId = antiReflective.FeatureId },
            new RxLensSpecFeature { ProductId = essilorLens.ProductId, FeatureId = blueLight.FeatureId },

            // Zeiss: blue light + anti-reflective
            new RxLensSpecFeature { ProductId = zeissLens.ProductId, FeatureId = blueLight.FeatureId },
            new RxLensSpecFeature { ProductId = zeissLens.ProductId, FeatureId = antiReflective.FeatureId }
        );
        await db.SaveChangesAsync();

        // Seed Rx Lens Variants (different refractive indices)
        var essilorVariant150 = new ProductVariant
        {
            ProductId = essilorLens.ProductId,
            Color = "Clear",
            RefractiveIndex = 1.50m,
            Price = 2000000m,
            StockQuantity = 50,
            PreOrderQuantity = 25,
            VariantSku = "ES-VC-001-150",
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var essilorVariant167 = new ProductVariant
        {
            ProductId = essilorLens.ProductId,
            Color = "Clear",
            RefractiveIndex = 1.67m,
            Price = 3000000m,
            StockQuantity = 30,
            PreOrderQuantity = 15,
            VariantSku = "ES-VC-001-167",
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var zeissVariant159 = new ProductVariant
        {
            ProductId = zeissLens.ProductId,
            Color = "Clear",
            RefractiveIndex = 1.59m,
            Price = 2500000m,
            StockQuantity = 40,
            PreOrderQuantity = 20,
            VariantSku = "ZS-DI-001-159",
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        var zeissVariant174 = new ProductVariant
        {
            ProductId = zeissLens.ProductId,
            Color = "Clear",
            RefractiveIndex = 1.74m,
            Price = 4000000m,
            StockQuantity = 20,
            PreOrderQuantity = 10,
            VariantSku = "ZS-DI-001-174",
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.ProductVariants.AddRange(essilorVariant150, essilorVariant167, zeissVariant159, zeissVariant174);
        await db.SaveChangesAsync();

        // Seed Contact Lens Product
        var contactLens = new Product
        {
            ProductName = "Acuvue Oasys",
            Sku = "AC-OA-001",
            Description = "Monthly contact lenses with UV protection",
            CategoryId = contactLenses.CategoryId,
            BrandId = essilor.BrandId,
            ProductType = ProductTypes.ContactLens,
            BasePrice = 500000m,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.Products.Add(contactLens);
        await db.SaveChangesAsync();

        db.ContactLensSpecs.Add(new ContactLensSpec
        {
            ProductId = contactLens.ProductId,
            MinSphere = -12m,
            MaxSphere = 8m,
            MinCylinder = -2m,
            MaxCylinder = 2m,
            BaseCurve = 8.4m,
            Diameter = 14m,
            Status = 1
        });
        await db.SaveChangesAsync();

        var contactLensVariant = new ProductVariant
        {
            ProductId = contactLens.ProductId,
            Color = "Clear",
            Price = 500000m,
            StockQuantity = 200,
            PreOrderQuantity = 100,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.ProductVariants.Add(contactLensVariant);
        await db.SaveChangesAsync();

        // Seed a Promotion
        var promotion = new Promotion
        {
            PromotionName = "Summer Sale 2024",
            Description = "20% off on all frames",
            PromotionType = "PERCENTAGE",
            DiscountPercentage = 20m,
            StartDate = now.AddDays(-7),
            EndDate = now.AddDays(30),
            MinimumPurchaseAmount = 500000m,
            MaximumUsagePerCustomer = 1,
            TotalUsageLimit = 1000,
            CurrentUsageCount = 0,
            PromoCode = "SUMMER2024",
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        db.Promotions.Add(promotion);
        await db.SaveChangesAsync();

        // Add promotion to frames category
        db.PromotionProducts.Add(new PromotionProduct
        {
            PromotionId = promotion.PromotionId,
            CategoryId = sunglasses.CategoryId
        });
        await db.SaveChangesAsync();
    }
}