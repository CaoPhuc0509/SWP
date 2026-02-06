# Database Refactoring Summary: Eyewear Product Specifications

## Overview
This document summarizes the database refactoring for eyewear product specifications to better support different product types with their unique attributes and variations.

## Product Types
The system now supports 5 product types:
1. **SUNGLASSES** - Sunglasses with frame and lens specifications
2. **FRAME** - Eyeglass frames (for prescription lenses)
3. **RX_LENS** - Prescription lenses
4. **CONTACT_LENS** - Contact lenses
5. **OTHER** - Accessories and other products

## Key Design Decisions

### 1. Product vs Variant Level Specifications

#### **Sunglasses**
- **Product Level**: Frame dimensions (A, B, DBL, TempleLength), frame material, rim type, shape, weight, lens material, lens type, UV protection, general tint color
- **Variant Level**: Specific color variations

#### **Frames**
- **Product Level**: Frame dimensions (A, B, DBL, TempleLength, LensWidth), frame material, rim type, shape, weight, hinge type, nose pads
- **Variant Level**: Color variations

#### **RxLens (Prescription Lenses)**
- **Product Level**: 
  - Design type (Single Vision, Progressive, Bifocal, etc.)
  - Material (CR-39, Polycarbonate, High Index, etc.)
  - Prescription ranges (Min/Max Sphere, Cylinder, Axis, Add)
  - Coating options (Anti-reflective, Blue light filter, UV protection, Scratch resistant)
  - Lens features
- **Variant Level**: 
  - Refractive index (1.50, 1.59, 1.67, 1.74, etc.) - This affects price and thickness
  - Color (usually "Clear" but can have tints)

**Rationale**: Prescription ranges define what prescriptions the lens can accommodate (product-level), while refractive index is a customer choice that affects price and appearance (variant-level).

#### **Contact Lenses**
- **Product Level**: 
  - Base curve and diameter (standard sizes for the lens model)
  - Prescription ranges (Min/Max Sphere, Cylinder, Axis)
  - Lens type (Daily, Monthly, Extended Wear)
  - Material (Silicone Hydrogel, Hydrogel)
  - Water content, oxygen permeability
  - Replacement schedule
  - Toric/Multifocal flags
- **Variant Level**: 
  - Base curve and diameter (if the product offers multiple sizes)
  - Color (for colored contact lenses)

**Rationale**: Base curve and diameter typically define the lens model, but some products offer multiple sizes as variants. Prescription ranges are product-level as they define what the lens can accommodate.

### 2. Database Schema Changes

#### New Entity: `SunglassesSpec`
- Stores sunglasses-specific specifications
- Includes both frame and lens attributes
- One-to-one relationship with Product

#### Updated Entity: `FrameSpec`
- Added `TempleLength` and `LensWidth` for complete frame dimensions
- Added `HingeType` and `HasNosePads` for additional frame details
- Sizes (A, B, DBL) remain at product level

#### Updated Entity: `RxLensSpec`
- Added `MinAxis`, `MaxAxis`, `MinAdd`, `MaxAdd` for complete prescription ranges
- Added coating flags: `HasAntiReflective`, `HasBlueLightFilter`, `HasUVProtection`, `HasScratchResistant`
- Prescription ranges remain at product level

#### Updated Entity: `ContactLensSpec`
- Added `MinAxis`, `MaxAxis` for complete prescription ranges
- Added `LensType`, `Material`, `WaterContent`, `OxygenPermeability`
- Added `ReplacementSchedule`, `IsToric`, `IsMultifocal`
- Base curve and diameter remain at product level (can be overridden in variants)

#### Updated Entity: `ProductVariant`
- Added `RefractiveIndex` (for RxLens variants)
- Added `BaseCurve` and `Diameter` (for Contact Lens variants that differ from product default)
- Added `VariantSku` for variant-specific SKU tracking
- Color remains for frames, sunglasses, and colored contact lenses

## Migration Notes

### Breaking Changes
1. Existing sunglasses products need to be migrated from `FrameSpec` to `SunglassesSpec`
2. RxLens variants should have `RefractiveIndex` populated
3. Contact lens specs now include additional fields that may need default values

### Data Migration Strategy
1. Identify products with `ProductType = "FRAME"` that are actually sunglasses
2. Create `SunglassesSpec` entries for sunglasses products
3. Remove `FrameSpec` entries for sunglasses products
4. Update RxLens variants to include refractive index values
5. Update ContactLens specs with new fields

## API Changes

### CatalogController Updates
- Added support for filtering sunglasses by frame dimensions
- Added `SunglassesSpec` to product response
- Added `RefractiveIndex` to variant response for RxLens
- Added `BaseCurve` and `Diameter` to variant response for Contact Lenses
- Enhanced `RxLensSpec` and `ContactLensSpec` responses with new fields

## Benefits of This Structure

1. **Clear Separation**: Product-level specs define what the product can accommodate, variant-level specs define customer choices
2. **Flexible Pricing**: Different refractive indices can have different prices
3. **Better Filtering**: Can filter products by their capabilities (prescription ranges, sizes)
4. **Scalability**: Easy to add new product types or specifications
5. **Industry Standard**: Aligns with how eyewear products are typically structured in e-commerce

## Example Usage

### Creating a Frame Product
```csharp
var frame = new Product {
    ProductType = ProductTypes.Frame,
    // ... other fields
};

var frameSpec = new FrameSpec {
    ProductId = frame.ProductId,
    A = 52m, B = 56m, Dbl = 20m, // Product-level dimensions
    // ... other specs
};

var variant1 = new ProductVariant {
    ProductId = frame.ProductId,
    Color = "Black", // Variant-level color
    Price = 2000000m
};
```

### Creating an RxLens Product
```csharp
var lens = new Product {
    ProductType = ProductTypes.RxLens,
    // ... other fields
};

var lensSpec = new RxLensSpec {
    ProductId = lens.ProductId,
    MinSphere = -10m, MaxSphere = 6m, // Product-level ranges
    // ... other specs
};

var variant150 = new ProductVariant {
    ProductId = lens.ProductId,
    RefractiveIndex = 1.50m, // Variant-level refractive index
    Price = 2000000m
};

var variant167 = new ProductVariant {
    ProductId = lens.ProductId,
    RefractiveIndex = 1.67m, // Higher index = thinner lens = higher price
    Price = 3000000m
};
```

## Next Steps

1. Run the migration to apply schema changes
2. Update seed data to reflect new structure
3. Test API endpoints with new product structure
4. Update frontend to display new specification fields
5. Consider adding validation rules for prescription ranges
