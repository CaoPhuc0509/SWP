using eyewearshop_api.Services;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/manager/products")]
[Authorize(Roles = "Manager")]
public class ProductManagerController : ControllerBase
{
    private readonly EyewearShopDbContext _db;
    private readonly IR2StorageService _r2;

    public ProductManagerController(EyewearShopDbContext db, IR2StorageService r2)
    {
        _db = db;
        _r2 = r2;
    }

    /// <summary>
    /// get products list
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetProducts(CancellationToken ct = default)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Sku,
                p.ProductType,
                p.BasePrice,
                VariantCount = p.Variants.Count,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(products);
    }

    /// <summary>
    /// Get product detail
    /// </summary>
    [HttpGet("{productId}")]
    public async Task<ActionResult> GetProductDetail(long productId, CancellationToken ct)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound();

        object? spec = null;

        if (product.ProductType == ProductTypes.RxLens)
        {
            spec = await _db.RxLensSpecs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        }
        else if (product.ProductType == ProductTypes.ContactLens)
        {
            spec = await _db.ContactLensSpecs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        }
        else if (product.ProductType == ProductTypes.Frame)
        {
            spec = await _db.FrameSpecs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        }
        else if (product.ProductType == ProductTypes.Combo || product.ProductType == ProductTypes.Sunglasses)
        {
            var rxSpec = await _db.RxLensSpecs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId, ct);

            var frameSpec = await _db.FrameSpecs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId, ct);

            spec = new
            {
                RxLensSpec = rxSpec,
                FrameSpec = frameSpec
            };
        }

        var result = new
        {
            product.ProductId,
            product.ProductName,
            product.Sku,
            product.Description,
            product.ProductType,
            product.BasePrice,
            product.Specifications,

            Category = product.Category != null
                ? new { product.Category.CategoryId, product.Category.CategoryName }
                : null,

            Brand = product.Brand != null
                ? new { product.Brand.BrandId, product.Brand.BrandName }
                : null,

            Variants = product.Variants.Select(v => new
            {
                v.VariantId,
                v.Color,
                v.Price,
                v.StockQuantity,
                v.PreOrderQuantity,
                v.Status
            }),

            Images = product.Images.Select(i => new
            {
                i.ImageId,
                i.IsPrimary
            }),

            Spec = spec,
            product.Status,
            product.CreatedAt,
            product.UpdatedAt
        };

        return Ok(result);
    }

    /// <summary>
    /// Create a new product (combo and sunglasses)
    /// </summary>
    [HttpPost("combo")]
    public async Task<ActionResult> CreateComboProduct([FromBody] CreateComboRequest request, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // validate brand
            var brandExists = await _db.Brands
                .AnyAsync(x => x.BrandId == request.BrandId, ct);

            if (!brandExists)
                return BadRequest("Brand not found");

            // validate category
            var categoryExists = await _db.Categories
                .AnyAsync(x => x.CategoryId == request.CategoryId, ct);

            if (!categoryExists)
                return BadRequest("Category not found");

            // validate product type
            if (request.ProductType != ProductTypes.Combo &&
                request.ProductType != ProductTypes.Sunglasses)
            {
                return BadRequest("ProductType must be COMBO or SUNGLASSES");
            }

            var product = new eyewearshop_data.Entities.Product
            {
                ProductName = request.ProductName,
                Sku = request.Sku,
                Description = request.Description,
                ProductType = request.ProductType,
                BasePrice = request.BasePrice,
                CategoryId = request.CategoryId,
                BrandId = request.BrandId,
                Specifications = request.Specifications,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = 1
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);

            var rxLensSpec = new RxLensSpec
            {
                ProductId = product.ProductId,
                DesignType = request.DesignType,
                Material = request.rxLensMaterial,
                LensWidth = request.LensWidth,
                MinSphere = request.MinSphere,
                MaxSphere = request.MaxSphere,
                MinCylinder = request.MinCylinder,
                MaxCylinder = request.MaxCylinder,
                MinAxis = request.MinAxis,
                MaxAxis = request.MaxAxis,
                MinAdd = request.MinAdd,
                MaxAdd = request.MaxAdd,
                HasAntiReflective = request.HasAntiReflective,
                HasBlueLightFilter = request.HasBlueLightFilter,
                HasUVProtection = request.HasUVProtection,
                HasScratchResistant = request.HasScratchResistant,
                Status = 1
            };

            var frameSpec = new FrameSpec
            {
                ProductId = product.ProductId,
                RimType = request.RimType,
                Material = request.FrameMaterial,
                Shape = request.Shape,
                Weight = request.Weight,
                A = request.A,
                B = request.B,
                Dbl = request.Dbl,
                TempleLength = request.TempleLength,
                LensWidth = request.FrameLensWidth,
                HingeType = request.HingeType,
                HasNosePads = request.HasNosePads,
                Status = 1
            };

            _db.RxLensSpecs.Add(rxLensSpec);
            _db.FrameSpecs.Add(frameSpec);

            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return Ok(new
            {
                productId = product.ProductId,
                message = "is created"
            });
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            return StatusCode(500, new
            {
                isCreated = false
            });
        }
    }

    /// <summary>
    /// Create a new product (rxlens)
    /// </summary>
    [HttpPost("rxlens")]
    public async Task<ActionResult> CreateRxLens([FromBody] CreateRxLensRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            ProductName = request.ProductName,
            Sku = request.Sku,
            Description = request.Description,
            ProductType = ProductTypes.RxLens,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Specifications = request.Specifications,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        var rxLensSpec = new RxLensSpec
        {
            ProductId = product.ProductId,
            DesignType = request.DesignType,
            Material = request.Material,
            LensWidth = request.LensWidth,
            MinSphere = request.MinSphere,
            MaxSphere = request.MaxSphere,
            MinCylinder = request.MinCylinder,
            MaxCylinder = request.MaxCylinder,
            MinAxis = request.MinAxis,
            MaxAxis = request.MaxAxis,
            MinAdd = request.MinAdd,
            MaxAdd = request.MaxAdd,
            HasAntiReflective = request.HasAntiReflective,
            HasBlueLightFilter = request.HasBlueLightFilter,
            HasUVProtection = request.HasUVProtection,
            HasScratchResistant = request.HasScratchResistant,
            Status = 1
        };

        _db.RxLensSpecs.Add(rxLensSpec);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
    nameof(GetProductDetail),
    new { productId = product.ProductId },
    new { productId = product.ProductId });
    }
    /// <summary>
    /// Create a new product (contactlens)
    /// </summary>

    [HttpPost("contactlens")]
    public async Task<ActionResult> CreateContactLens([FromBody] CreateContactLensRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            ProductName = request.ProductName,
            Sku = request.Sku,
            Description = request.Description,
            ProductType = ProductTypes.ContactLens,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Specifications = request.Specifications,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        var contactLensSpec = new ContactLensSpec
        {
            ProductId = product.ProductId,
            BaseCurve = request.BaseCurve,
            Diameter = request.Diameter,
            MinSphere = request.MinSphere,
            MaxSphere = request.MaxSphere,
            MinCylinder = request.MinCylinder,
            MaxCylinder = request.MaxCylinder,
            MinAxis = request.MinAxis,
            MaxAxis = request.MaxAxis,
            LensType = request.LensType,
            Material = request.Material,
            WaterContent = request.WaterContent,
            OxygenPermeability = request.OxygenPermeability,
            ReplacementSchedule = request.ReplacementSchedule,
            IsToric = request.IsToric,
            IsMultifocal = request.IsMultifocal,
            Status = 1
        };

        _db.ContactLensSpecs.Add(contactLensSpec);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetProductDetail), new { productId = product.ProductId }, new { productId = product.ProductId });
    }

    /// <summary>
    /// Create a new product (frame)
    /// </summary>
    [HttpPost("frame")]
    public async Task<ActionResult> CreateFrame([FromBody] CreateFrameRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            ProductName = request.ProductName,
            Sku = request.Sku,
            Description = request.Description,
            ProductType = ProductTypes.Frame,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Specifications = request.Specifications,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        var frameSpec = new FrameSpec
        {
            ProductId = product.ProductId,
            RimType = request.RimType,
            Material = request.Material,
            Shape = request.Shape,
            Weight = request.Weight,
            A = request.A,
            B = request.B,
            Dbl = request.Dbl,
            TempleLength = request.TempleLength,
            LensWidth = request.LensWidth,
            HingeType = request.HingeType,
            HasNosePads = request.HasNosePads,
            Status = 1
        };

        _db.FrameSpecs.Add(frameSpec);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetProductDetail), new { productId = product.ProductId }, new { productId = product.ProductId });
    }




    /// <summary>
    /// update product info (combo)
    /// </summary>
    [HttpPut("combo/{productId}")]
    public async Task<ActionResult> UpdateComboProduct(long productId, [FromBody] UpdateComboRequest request, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.RxLensSpec)
            .Include(p => p.FrameSpec)
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        if (product.ProductType != ProductTypes.Combo && product.ProductType != ProductTypes.Sunglasses)
            return Conflict("Product type mismatch. This product is not Combo or Sunglasses.");

        // Update common product fields
        product.ProductName = request.ProductName ?? product.ProductName;
        product.Sku = request.Sku ?? product.Sku;
        product.Description = request.Description ?? product.Description;
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.BrandId = request.BrandId ?? product.BrandId;
        product.Specifications = request.Specifications ?? product.Specifications;

        // Update RxLensSpec fields
        if (product.RxLensSpec != null)
        {
            product.RxLensSpec.DesignType = request.DesignType ?? product.RxLensSpec.DesignType;
            product.RxLensSpec.Material = request.rxLensMaterial ?? product.RxLensSpec.Material;
            product.RxLensSpec.LensWidth = request.LensWidth ?? product.RxLensSpec.LensWidth;
            product.RxLensSpec.MinSphere = request.MinSphere ?? product.RxLensSpec.MinSphere;
            product.RxLensSpec.MaxSphere = request.MaxSphere ?? product.RxLensSpec.MaxSphere;
            product.RxLensSpec.MinCylinder = request.MinCylinder ?? product.RxLensSpec.MinCylinder;
            product.RxLensSpec.MaxCylinder = request.MaxCylinder ?? product.RxLensSpec.MaxCylinder;
            product.RxLensSpec.MinAxis = request.MinAxis ?? product.RxLensSpec.MinAxis;
            product.RxLensSpec.MaxAxis = request.MaxAxis ?? product.RxLensSpec.MaxAxis;
            product.RxLensSpec.MinAdd = request.MinAdd ?? product.RxLensSpec.MinAdd;
            product.RxLensSpec.MaxAdd = request.MaxAdd ?? product.RxLensSpec.MaxAdd;
            product.RxLensSpec.HasAntiReflective = request.HasAntiReflective ?? product.RxLensSpec.HasAntiReflective;
            product.RxLensSpec.HasBlueLightFilter = request.HasBlueLightFilter ?? product.RxLensSpec.HasBlueLightFilter;
            product.RxLensSpec.HasUVProtection = request.HasUVProtection ?? product.RxLensSpec.HasUVProtection;
            product.RxLensSpec.HasScratchResistant = request.HasScratchResistant ?? product.RxLensSpec.HasScratchResistant;
        }
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (product.FrameSpec != null)
        {
            // Update FrameSpec fields
            product.FrameSpec.RimType = request.RimType ?? product.FrameSpec.RimType;
            product.FrameSpec.Material = request.FrameMaterial ?? product.FrameSpec.Material;
            product.FrameSpec.Shape = request.Shape ?? product.FrameSpec.Shape;
            product.FrameSpec.Weight = request.Weight ?? product.FrameSpec.Weight;
            product.FrameSpec.A = request.A ?? product.FrameSpec.A;
            product.FrameSpec.B = request.B ?? product.FrameSpec.B;
            product.FrameSpec.Dbl = request.Dbl ?? product.FrameSpec.Dbl;
            product.FrameSpec.TempleLength = request.TempleLength ?? product.FrameSpec.TempleLength;
            product.FrameSpec.LensWidth = request.FrameLensWidth ?? product.FrameSpec.LensWidth;
            product.FrameSpec.HingeType = request.HingeType ?? product.FrameSpec.HingeType;
            product.FrameSpec.HasNosePads = request.HasNosePads ?? product.FrameSpec.HasNosePads;
        }
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { productId = product.ProductId, message = "is updated" });
    }

    /// <summary>
    /// update product info (rxlens)
    /// </summary>
    [HttpPut("lens/{productId}")]
    public async Task<ActionResult> UpdateRxLens(long productId, [FromBody] UpdateLensRequest request, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.RxLensSpec)
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        if (product.ProductType != ProductTypes.RxLens)
            return Conflict("Product type mismatch. This product is not RX Lens.");

        if (product.RxLensSpec == null)
            return Conflict("RX Lens specification not found");

        product.ProductName = request.ProductName ?? product.ProductName;
        product.Sku = request.Sku ?? product.Sku;
        product.Description = request.Description ?? product.Description;
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.BrandId = request.BrandId ?? product.BrandId;

        product.RxLensSpec.DesignType = request.DesignType ?? product.RxLensSpec.DesignType;
        product.RxLensSpec.Material = request.Material ?? product.RxLensSpec.Material;
        product.RxLensSpec.LensWidth = request.LensWidth ?? product.RxLensSpec.LensWidth;
        product.RxLensSpec.MinSphere = request.MinSphere ?? product.RxLensSpec.MinSphere;
        product.RxLensSpec.MaxSphere = request.MaxSphere ?? product.RxLensSpec.MaxSphere;
        product.RxLensSpec.MinCylinder = request.MinCylinder ?? product.RxLensSpec.MinCylinder;
        product.RxLensSpec.MaxCylinder = request.MaxCylinder ?? product.RxLensSpec.MaxCylinder;
        product.RxLensSpec.MinAxis = request.MinAxis ?? product.RxLensSpec.MinAxis;
        product.RxLensSpec.MaxAxis = request.MaxAxis ?? product.RxLensSpec.MaxAxis;
        product.RxLensSpec.MinAdd = request.MinAdd ?? product.RxLensSpec.MinAdd;
        product.RxLensSpec.MaxAdd = request.MaxAdd ?? product.RxLensSpec.MaxAdd;
        product.RxLensSpec.HasAntiReflective = request.HasAntiReflective ?? product.RxLensSpec.HasAntiReflective;
        product.RxLensSpec.HasBlueLightFilter = request.HasBlueLightFilter ?? product.RxLensSpec.HasBlueLightFilter;
        product.RxLensSpec.HasUVProtection = request.HasUVProtection ?? product.RxLensSpec.HasUVProtection;
        product.RxLensSpec.HasScratchResistant = request.HasScratchResistant ?? product.RxLensSpec.HasScratchResistant;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { productId = product.ProductId, message = "is updated" });
    }

    /// <summary>
    /// update product info (contact lens)
    /// </summary>
    [HttpPut("contact-lens/{productId}")]
    public async Task<ActionResult> UpdateContactLens(long productId, [FromBody] UpdateContactLensRequest request, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.ContactLensSpec)
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        if (product.ProductType != ProductTypes.ContactLens)
            return Conflict("Product type mismatch. This product is not Contact Lens.");

        if (product.ContactLensSpec == null)
            return Conflict("Contact Lens specification not found");

        product.ProductName = request.ProductName ?? product.ProductName;
        product.Sku = request.Sku ?? product.Sku;
        product.Description = request.Description ?? product.Description;
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.BrandId = request.BrandId ?? product.BrandId;

        product.ContactLensSpec.Material = request.Material ?? product.ContactLensSpec.Material;
        product.ContactLensSpec.WaterContent = request.WaterContent ?? product.ContactLensSpec.WaterContent;
        product.ContactLensSpec.BaseCurve = request.BaseCurve ?? product.ContactLensSpec.BaseCurve;
        product.ContactLensSpec.Diameter = request.Diameter ?? product.ContactLensSpec.Diameter;
        product.ContactLensSpec.MinSphere = request.MinSphere ?? product.ContactLensSpec.MinSphere;
        product.ContactLensSpec.MaxSphere = request.MaxSphere ?? product.ContactLensSpec.MaxSphere;
        product.ContactLensSpec.MinCylinder = request.MinCylinder ?? product.ContactLensSpec.MinCylinder;
        product.ContactLensSpec.MaxCylinder = request.MaxCylinder ?? product.ContactLensSpec.MaxCylinder;
        product.ContactLensSpec.MinAxis = request.MinAxis ?? product.ContactLensSpec.MinAxis;
        product.ContactLensSpec.MaxAxis = request.MaxAxis ?? product.ContactLensSpec.MaxAxis;
        product.ContactLensSpec.LensType = request.LensType ?? product.ContactLensSpec.LensType;
        product.ContactLensSpec.OxygenPermeability = request.OxygenPermeability ?? product.ContactLensSpec.OxygenPermeability;
        product.ContactLensSpec.ReplacementSchedule = request.ReplacementSchedule ?? product.ContactLensSpec.ReplacementSchedule;
        product.ContactLensSpec.IsToric = request.IsToric ?? product.ContactLensSpec.IsToric;
        product.ContactLensSpec.IsMultifocal = request.IsMultifocal ?? product.ContactLensSpec.IsMultifocal;

        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { productId = product.ProductId, message = "is updated" });
    }

    /// <summary>
    /// update product info (frame)
    /// </summary>
    [HttpPut("frame/{productId}")]
    public async Task<ActionResult> UpdateFrame(long productId, [FromBody] UpdateFrameRequest request, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.FrameSpec)
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        if (product.ProductType != ProductTypes.Frame)
            return Conflict("Product type mismatch. This product is not Frame.");

        if (product.FrameSpec == null)
            return Conflict("Frame specification not found");

        product.ProductName = request.ProductName ?? product.ProductName;
        product.Sku = request.Sku ?? product.Sku;
        product.Description = request.Description ?? product.Description;
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.BrandId = request.BrandId ?? product.BrandId;

        product.FrameSpec.RimType = request.RimType ?? product.FrameSpec.RimType;
        product.FrameSpec.Material = request.Material ?? product.FrameSpec.Material;
        product.FrameSpec.Shape = request.Shape ?? product.FrameSpec.Shape;
        product.FrameSpec.Weight = request.Weight ?? product.FrameSpec.Weight;
        product.FrameSpec.A = request.A ?? product.FrameSpec.A;
        product.FrameSpec.B = request.B ?? product.FrameSpec.B;
        product.FrameSpec.Dbl = request.Dbl ?? product.FrameSpec.Dbl;
        product.FrameSpec.TempleLength = request.TempleLength ?? product.FrameSpec.TempleLength;
        product.FrameSpec.LensWidth = request.LensWidth ?? product.FrameSpec.LensWidth;
        product.FrameSpec.HingeType = request.HingeType ?? product.FrameSpec.HingeType;
        product.FrameSpec.HasNosePads = request.HasNosePads ?? product.FrameSpec.HasNosePads;

        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { productId = product.ProductId, message = "is updated" });
    }

    /// <summary>
    /// add variant to product (color, price, stockQuantity, preOrderQuantity)
    /// </summary>
    [HttpPost("{productId}/variants")]
    public async Task<ActionResult> AddVariant(
     [FromRoute] long productId,
     [FromBody] AddVariantRequest request,
     CancellationToken ct)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        var variant = new eyewearshop_data.Entities.ProductVariant
        {
            ProductId = productId,
            Color = request.Color,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            PreOrderQuantity = request.PreOrderQuantity,
            VariantSku = request.VariantSku,
            BaseCurve = request.BaseCurve,
            Diameter = request.Diameter,
            RefractiveIndex = request.RefractiveIndex,
            ExpectedDateRestock = request.ExpectedDateRestock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(ct);

        var response = new VariantResponse(
            variant.VariantId,
            variant.ProductId,
            variant.Color,
            variant.Price,
            variant.StockQuantity,
            variant.PreOrderQuantity,
            variant.VariantSku,
            variant.BaseCurve,
            variant.Diameter,
            variant.RefractiveIndex,
            variant.ExpectedDateRestock,
            variant.Status
        );

        return CreatedAtAction(
            nameof(GetProductDetail),
            new { productId },
            response
        );
    }

    /// <summary>
    /// update variant of product
    /// </summary>
    [HttpPut("{productId}/variants/{variantId}")]
    public async Task<ActionResult> UpdateVariant([FromRoute] long productId, [FromRoute] long variantId, [FromBody] UpdateVariantRequest request, CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.ProductId == productId, ct);

        if (variant == null)
            return NotFound();

        variant.Color = request.Color;
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.PreOrderQuantity = request.PreOrderQuantity;
        variant.VariantSku = request.VariantSku;
        variant.BaseCurve = request.BaseCurve;
        variant.Diameter = request.Diameter;
        variant.RefractiveIndex = request.RefractiveIndex;
        variant.ExpectedDateRestock = request.ExpectedDateRestock;
        variant.Status = request.Status;
        variant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(variant);
    }

    /// <summary>
    /// update status of product - delete product (status = 0)
    /// </summary>
    [HttpPut("{status}/products/{productId}")]
    public async Task<ActionResult> UpdateProductStatus([FromRoute] long productId, [FromRoute] short status, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product == null)
            return NotFound();
        if (status != 0 && status != 1)
            return BadRequest("Invalid status value. Status must be either 0 (inactive) or 1 (active).");

        product.Status = status;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { productId = product.ProductId, message = status == 0 ? "is deactivated" : "is activated" });
    }

    /// <summary>
    /// Upload an image for a product variant. Accepts multipart/form-data with field "file" (image/*, max 5 MB).
    /// Optional int query params: sortOrder (default 0), isPrimary (default false).
    /// </summary>
    [HttpPost("{productId}/variants/{variantId}/images/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadVariantImage(
        [FromRoute] long productId,
        [FromRoute] long variantId,
        IFormFile file,
        [FromQuery] int sortOrder = 0,
        [FromQuery] bool isPrimary = false,
        CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.ProductId == productId, ct);

        if (variant == null)
            return NotFound("Variant not found.");

        string url;
        try
        {
            url = await _r2.UploadAsync(file, "products", ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        // If this image is marked primary, clear existing primary flag for the same product
        if (isPrimary)
        {
            var existingPrimary = await _db.ProductImages
                .Where(i => i.ProductId == productId && i.IsPrimary)
                .ToListAsync(ct);
            foreach (var img in existingPrimary)
            {
                img.IsPrimary = false;
            }
        }

        var image = new ProductImage
        {
            ProductId = productId,
            VariantId = variantId,
            Url = url,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            Status = 1
        };

        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetProductDetail),
            new { productId },
            new
            {
                image.ImageId,
                image.ProductId,
                image.VariantId,
                image.Url,
                image.SortOrder,
                image.IsPrimary,
                image.Status
            });
    }

    /// <summary>
    /// Delete a product image by imageId. Removes both the DB record and the object from R2.
    /// </summary>
    [HttpDelete("{productId}/images/{imageId}")]
    public async Task<ActionResult> DeleteProductImage(
        [FromRoute] long productId,
        [FromRoute] long imageId,
        CancellationToken ct)
    {
        var image = await _db.ProductImages
            .FirstOrDefaultAsync(i => i.ImageId == imageId && i.ProductId == productId, ct);

        if (image == null)
            return NotFound("Image not found.");

        // Delete from R2
        var objectKey = _r2.ExtractKeyFromUrl(image.Url);
        if (objectKey != null)
            await _r2.DeleteAsync(objectKey, ct);

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}




public record CreateComboRequest(string ProductName, string Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? ProductType, string? Specifications,
    // combo-specific fields
    //rxlens fields
    string? DesignType,
    string? rxLensMaterial,
    decimal? LensWidth,
    decimal? MinSphere,
    decimal? MaxSphere,
    decimal? MinCylinder,
    decimal? MaxCylinder,
    decimal? MinAxis,
    decimal? MaxAxis,
    decimal? MinAdd,
    decimal? MaxAdd,
    bool? HasAntiReflective,
    bool? HasBlueLightFilter,
    bool? HasUVProtection,
    bool? HasScratchResistant,
    //frame fields
    string? RimType,
    string? FrameMaterial,
    string? Shape,
    decimal? Weight,
    decimal? A,
    decimal? B,
    decimal? Dbl,
    decimal? TempleLength,
    decimal? FrameLensWidth,
    string? HingeType,
    bool? HasNosePads
);
public record CreateRxLensRequest(string ProductName, string Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,
    // RxLensSpec fields
    string? DesignType,
    string? Material,
    decimal? LensWidth,
    decimal? MinSphere,
    decimal? MaxSphere,
    decimal? MinCylinder,
    decimal? MaxCylinder,
    decimal? MinAxis,
    decimal? MaxAxis,
    decimal? MinAdd,
    decimal? MaxAdd,
    bool? HasAntiReflective,
    bool? HasBlueLightFilter,
    bool? HasUVProtection,
    bool? HasScratchResistant
);
public record CreateContactLensRequest(string ProductName, string Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,

    // ContactLensSpec fields
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? MinSphere,
    decimal? MaxSphere,
    decimal? MinCylinder,
    decimal? MaxCylinder,
    decimal? MinAxis,
    decimal? MaxAxis,
    string? LensType,
    string? Material,
    int? WaterContent,
    int? OxygenPermeability,
    int? ReplacementSchedule,
    bool? IsToric,
    bool? IsMultifocal
);
public record CreateFrameRequest(string ProductName, string Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,
    // FrameSpec fields
    string? RimType,
    string? Material,
    string? Shape,
    decimal? Weight,
    decimal? A,
    decimal? B,
    decimal? Dbl,
    decimal? TempleLength,
    decimal? LensWidth,
    string? HingeType,
    bool? HasNosePads
);
public record UpdateComboRequest(string? ProductName, string? Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,
    // combo-specific fields
    //rxlens fields
    string? DesignType,
    string? rxLensMaterial,
    decimal? LensWidth,
    decimal? MinSphere,
    decimal? MaxSphere,
    decimal? MinCylinder,
    decimal? MaxCylinder,
    decimal? MinAxis,
    decimal? MaxAxis,
    decimal? MinAdd,
    decimal? MaxAdd,
    bool? HasAntiReflective,
    bool? HasBlueLightFilter,
    bool? HasUVProtection,
    bool? HasScratchResistant,
    //frame fields
    string? RimType,
    string? FrameMaterial,
    string? Shape,
    decimal? Weight,
    decimal? A,
    decimal? B,
    decimal? Dbl,
    decimal? TempleLength,
    decimal? FrameLensWidth,
    string? HingeType,
    bool? HasNosePads
);
public record UpdateLensRequest(string? ProductName, string? Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,
    // RxLensSpec fields
    string? DesignType,
    string? Material,
    decimal? LensWidth,
    decimal? MinSphere,
    decimal? MaxSphere,
    decimal? MinCylinder,
    decimal? MaxCylinder,
    decimal? MinAxis,
    decimal? MaxAxis,
    decimal? MinAdd,
    decimal? MaxAdd,
    bool? HasAntiReflective,
    bool? HasBlueLightFilter,
    bool? HasUVProtection,
    bool? HasScratchResistant
);
public record UpdateContactLensRequest(string? ProductName, string? Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,

    // ContactLensSpec fields
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? MinSphere,
    decimal? MaxSphere,
    decimal? MinCylinder,
    decimal? MaxCylinder,
    decimal? MinAxis,
    decimal? MaxAxis,
    string? LensType,
    string? Material,
    int? WaterContent,
    int? OxygenPermeability,
    int? ReplacementSchedule,
    bool? IsToric,
    bool? IsMultifocal
);
public record UpdateFrameRequest(string? ProductName, string? Sku, string? Description, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications,
    // FrameSpec fields
    string? RimType,
    string? Material,
    string? Shape,
    decimal? Weight,
    decimal? A,
    decimal? B,
    decimal? Dbl,
    decimal? TempleLength,
    decimal? LensWidth,
    string? HingeType,
    bool? HasNosePads
);
public record AddVariantRequest(
    string? Color,
    decimal Price,
    int StockQuantity,
    int PreOrderQuantity,
    string VariantSku,
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? RefractiveIndex,
    DateTime? ExpectedDateRestock
);
public record UpdateVariantRequest(
    string? Color,
    decimal Price,
    int StockQuantity,
    int PreOrderQuantity,
    string VariantSku,
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? RefractiveIndex,
    DateTime? ExpectedDateRestock,
    short Status
);
public record VariantResponse(
    long VariantId,
    long ProductId,
    string? Color,
    decimal Price,
    int StockQuantity,
    int PreOrderQuantity,
    string VariantSku,
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? RefractiveIndex,
    DateTime? ExpectedDateRestock,
    short Status
);