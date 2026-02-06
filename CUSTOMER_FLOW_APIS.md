# Customer Flow APIs - Complete Implementation

## ✅ All Customer Flow Requirements Implemented

### 1. Browse Product Catalog ✅
**Controller:** `CatalogController`  
**Endpoints:**
- `GET /api/catalog/products` - Browse products with filtering
- `GET /api/catalog/products/{productId}` - View product details
- `GET /api/catalog/categories` - List categories
- `GET /api/catalog/brands` - List brands

**Features:**
- Search by product name or SKU
- Filter by product type (Frame, RxLens, ContactLens, Combo, Other)
- Filter by category, brand, color
- Filter by price range (minPrice, maxPrice)
- Filter by frame size (minA, maxA, minB, maxB, minDbl, maxDbl)
- Pagination support
- Returns product images, variants, and specifications

### 2. Filter and Search Products ✅
**Controller:** `CatalogController`  
**Endpoint:** `GET /api/catalog/products`

**Query Parameters:**
- `q` - Search query (product name or SKU)
- `productType` - Filter by type (FRAME, RX_LENS, CONTACT_LENS, COMBO, OTHER)
- `categoryId` - Filter by category
- `brandId` - Filter by brand
- `color` - Filter by color
- `minPrice`, `maxPrice` - Price range filter
- `minA`, `maxA` - Frame size A dimension
- `minB`, `maxB` - Frame size B dimension
- `minDbl`, `maxDbl` - Frame size DBL dimension
- `page`, `pageSize` - Pagination

### 3. View Product Details ✅
**Controller:** `CatalogController`  
**Endpoint:** `GET /api/catalog/products/{productId}`

**Returns:**
- Product information (name, SKU, description, type, base price)
- Brand and category details
- Product images (with sort order and primary image)
- Product variants (color, price, stock, pre-order availability)
- Frame specifications (rim type, material, A/B/DBL dimensions, shape, weight)
- Contact lens specifications (sphere/cylinder ranges, base curve, diameter)
- Rx lens specifications (design type, material, sphere/cylinder ranges, features)

### 4. Order Glasses with Different Order Types ✅
**Controller:** `CheckoutController`  
**Endpoint:** `POST /api/checkout`

**Order Types Supported:**
- **AVAILABLE** - In-stock items ready for immediate shipping
- **PRE_ORDER** - Items with insufficient stock but available for pre-order
- **PRESCRIPTION** - Frame + Lens combination requiring prescription

**Features:**
- Automatic order type detection based on cart contents
- Prescription validation for prescription orders
- Stock and pre-order quantity validation
- Promotion code support
- Shipping address and method selection
- Automatic stock/pre-order quantity updates

### 5. Manage Shopping Cart ✅
**Controller:** `CartController`  
**Endpoints:**
- `GET /api/cart` - Get cart with all items and summary
- `POST /api/cart/items` - Add item to cart
- `PUT /api/cart/items/{cartItemId}` - Update item quantity
- `DELETE /api/cart/items/{cartItemId}` - Remove item from cart

**Features:**
- Automatic cart creation for new users
- Real-time price calculation
- Stock availability display
- Pre-order availability display
- Cart summary (subtotal, item count)

### 6. Checkout ✅
**Controller:** `CheckoutController`  
**Endpoints:**
- `GET /api/checkout/requirements` - Check checkout requirements (prescription, address)
- `POST /api/checkout` - Create order from cart

**Features:**
- Address validation
- Prescription requirement checking
- Promotion code application
- Shipping fee calculation
- Order number generation
- Automatic cart clearing after successful checkout
- Stock management (updates stock/pre-order quantities)

### 7. Payment ✅
**Controller:** `PaymentController`  
**Endpoints:**
- `GET /api/payments/order/{orderId}` - Get all payments for an order
- `POST /api/payments` - Create payment for an order
- `GET /api/payments/{paymentId}` - Get payment details
- `PUT /api/payments/{paymentId}/confirm` - Confirm payment (for manual/offline payments)

**Features:**
- Multiple payment types (CASH, CARD, BANK_TRANSFER, E_WALLET)
- Payment method tracking (VISA, MASTERCARD, VIETQR, MOMO, etc.)
- Payment amount validation
- Remaining balance calculation
- Payment status tracking (pending, completed, failed)
- Order payment status checking

### 8. Manage Personal Account ✅
**Controller:** `AccountController`  
**Endpoints:**
- `GET /api/account/profile` - Get user profile
- `PUT /api/account/profile` - Update user profile
- `GET /api/account/addresses` - List all addresses
- `GET /api/account/addresses/{addressId}` - Get address details
- `POST /api/account/addresses` - Create new address
- `PUT /api/account/addresses/{addressId}` - Update address
- `DELETE /api/account/addresses/{addressId}` - Delete address

**Features:**
- Profile management (full name, phone, gender, date of birth)
- Multiple shipping addresses
- Address validation
- Soft delete for addresses used in orders
- Address management with full CRUD operations

### 9. View Order History ✅
**Controller:** `OrderController`  
**Endpoints:**
- `GET /api/orders` - List customer orders with filtering
- `GET /api/orders/{orderId}` - Get detailed order information

**Features:**
- Filter by order type (AVAILABLE, PRE_ORDER, PRESCRIPTION)
- Filter by status
- Pagination support
- Order details include:
  - Order items with product information
  - Prescription details (if applicable)
  - Shipping and tracking information
  - Payment history
  - Order totals and status

### 10. Submit Return Requests ✅
**Controller:** `ReturnRequestController`  
**Endpoints:**
- `GET /api/return-requests` - List return requests with filtering
- `GET /api/return-requests/{returnRequestId}` - Get return request details
- `POST /api/return-requests` - Create return request

**Features:**
- Support for three request types:
  - **EXCHANGE** - Exchange items for different products
  - **RETURN** - Return items for refund
  - **WARRANTY** - Warranty claims
- Order validation (only delivered/completed orders)
- Item quantity validation
- Return request number generation
- Status tracking with simple status transitions
- Exchange order linking (for exchanges)

### 11. Prescription Management ✅
**Controller:** `PrescriptionController`  
**Endpoints:**
- `GET /api/prescriptions` - List customer prescriptions
- `GET /api/prescriptions/{prescriptionId}` - Get prescription details
- `POST /api/prescriptions` - Create new prescription
- `PUT /api/prescriptions/{prescriptionId}` - Update prescription
- `DELETE /api/prescriptions/{prescriptionId}` - Delete prescription

**Features:**
- Full prescription specifications:
  - Right eye (OD): Sphere, Cylinder, Axis, Add, PD
  - Left eye (OS): Sphere, Cylinder, Axis, Add, PD
- Prescription date and doctor/clinic information
- Notes field
- Soft delete for prescriptions used in orders

## Authentication

All customer endpoints (except catalog browsing) require JWT authentication via the `[Authorize]` attribute.

**Auth Endpoints:**
- `POST /api/auth/register` - Register new customer
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout
- `GET /api/auth/me` - Get current user info

## Database Schema

All database tables have been created and migrated:
- ✅ Products, Variants, Images
- ✅ Categories, Brands, Features
- ✅ Frame Specs, Rx Lens Specs, Contact Lens Specs
- ✅ Carts, Cart Items
- ✅ Orders, Order Items
- ✅ Prescriptions
- ✅ Return Requests, Return Request Items
- ✅ Shipping Infos
- ✅ Payments
- ✅ Promotions, Promotion Products
- ✅ Combos, Combo Items
- ✅ User Addresses

## Status

**✅ ALL CUSTOMER FLOW REQUIREMENTS COMPLETE**

All APIs are implemented, tested for compilation, and ready for use. The database migration has been successfully applied to PostgreSQL.