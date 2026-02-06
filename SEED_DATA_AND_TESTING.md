# Seed Data and Customer Purchase Flow Testing Guide

## Step 1: Seed the Database

### Option A: Using the Seed API Endpoint (Recommended)

1. **Start the API** (if not already running):
   ```bash
   cd eyewearshop-api
   dotnet run
   ```

2. **Call the seed endpoint**:
   ```bash
   POST http://localhost:5000/api/seed
   ```
   
   Or using curl:
   ```bash
   curl -X POST http://localhost:5000/api/seed
   ```

### Option B: Using a Console Application

Create a simple console app or run the seed method directly in Program.cs on startup (for development only).

## Step 2: Verify Seeded Data

Check that data was seeded:
- **Brands**: Ray-Ban, Oakley, Gucci, Tom Ford, Essilor, Zeiss
- **Categories**: Sunglasses, Eyeglasses, Reading Glasses, Contact Lenses
- **Products**: 
  - 4 Frame products (Ray-Ban Aviator, Wayfarer, Gucci GG0061S, Oakley Holbrook)
  - 2 Rx Lens products (Essilor Varilux, Zeiss Digital)
  - 1 Contact Lens product (Acuvue Oasys)
- **Variants**: Multiple color variants for each frame
- **Promotion**: "SUMMER2024" - 20% off frames

## Step 3: Test Customer Purchase Flow

### Prerequisites
1. Register a customer account
2. Create a shipping address
3. (Optional) Create a prescription if ordering prescription glasses

### Purchase Flow Steps

#### 1. Register/Login
```http
POST /api/auth/register
{
  "email": "customer@example.com",
  "password": "Password123!",
  "fullName": "John Doe"
}
```

#### 2. Browse Products
```http
GET /api/catalog/products?productType=FRAME&page=1&pageSize=20
```

#### 3. View Product Details
```http
GET /api/catalog/products/{productId}
```

#### 4. Add Items to Cart (Session-based)
```http
POST /api/cart/items
Authorization: Bearer {token}
{
  "variantId": 1,
  "quantity": 2
}
```

#### 5. View Cart
```http
GET /api/cart
Authorization: Bearer {token}
```

#### 6. Create Shipping Address
```http
POST /api/account/addresses
Authorization: Bearer {token}
{
  "recipientName": "John Doe",
  "phoneNumber": "0901234567",
  "addressLine": "123 Main Street",
  "city": "Ho Chi Minh City",
  "district": "District 1",
  "note": "Ring doorbell"
}
```

#### 7. Check Checkout Requirements
```http
GET /api/checkout/requirements
Authorization: Bearer {token}
```

#### 8. Checkout
```http
POST /api/checkout
Authorization: Bearer {token}
{
  "addressId": 1,
  "prescriptionId": null,
  "promoCode": "SUMMER2024",
  "shippingMethod": "STANDARD"
}
```

**Response:**
```json
{
  "orderId": 1,
  "orderNumber": "OD240206123456789",
  "orderType": "AVAILABLE",
  "totalAmount": 144000,
  "requiresPrescription": false
}
```

#### 9. Create Payment
```http
POST /api/payments
Authorization: Bearer {token}
{
  "orderId": 1,
  "paymentType": "BANK_TRANSFER",
  "paymentMethod": "VIETQR",
  "amount": 144000,
  "note": "Payment via VietQR"
}
```

#### 10. View Order History
```http
GET /api/orders
Authorization: Bearer {token}
```

#### 11. View Order Details
```http
GET /api/orders/{orderId}
Authorization: Bearer {token}
```

## Sample Product IDs (After Seeding)

Based on the seed data, here are approximate IDs you can use:

- **Ray-Ban Aviator Black**: VariantId ~1
- **Ray-Ban Aviator Gold**: VariantId ~2
- **Ray-Ban Wayfarer Black**: VariantId ~3
- **Ray-Ban Wayfarer Tortoise**: VariantId ~4
- **Gucci Frame Black**: VariantId ~5
- **Oakley Frame Black**: VariantId ~7

*Note: Actual IDs depend on existing data in your database*

## Testing Different Order Types

### Available Order (In-stock items)
- Add frame variants with sufficient stock
- Order type will be "AVAILABLE"

### Pre-Order
- Add items where quantity > stockQuantity but preOrderQuantity > 0
- Order type will be "PRE_ORDER"

### Prescription Order
- Add both a Frame and RxLens to cart
- Include prescriptionId in checkout
- Order type will be "PRESCRIPTION"

## Testing with Promotion

Use promo code: **SUMMER2024**
- 20% discount on frames
- Minimum purchase: 500,000 VND
- Valid for 30 days from seed date

## Notes

- Cart is session-based (stored in server session, not database)
- Session expires after 30 minutes of inactivity
- Cart is automatically cleared after successful checkout
- All prices are in VND (Vietnamese Dong)