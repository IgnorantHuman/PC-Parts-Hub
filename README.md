# PC Parts Hub

PC Parts Hub is an online marketplace for buying and selling computer components. It was developed using ASP.NET Core Razor Pages as part of the UCCA3224 Web Application and Technologies assignment.

The platform provides different functions for Buyers, Sellers and Administrators. It supports the complete marketplace process, including product submission, administrative approval, product browsing, shopping cart management, checkout, payment simulation, order tracking and product reviews.

## Live Website

[Open PC Parts Hub](https://pcpartshub-ucca3224-2026-cja4hdebd9ezc9au.southeastasia-01.azurewebsites.net/)

## Main Features

### Buyer

- Register and log in to an account
- Browse administrator-approved PC components
- Search and filter products
- View product details, images, prices and stock
- View promotional discounts
- Add and remove wishlist items
- Add products to the shopping cart
- Update cart quantities dynamically
- Complete checkout and simulated payment
- View order history and order details
- Submit product ratings and reviews
- Receive new-product notifications
- Communicate through SignalR live chat
- Convert currencies using current exchange rates

### Seller

- Access a role-based Seller Dashboard
- Create product listings
- Upload multiple product images
- Edit or delete owned products
- Manage product prices and stock
- Create time-based promotional discounts
- Monitor Pending, Active and Rejected products
- View administrator rejection reasons
- Resubmit corrected products for review
- View inventory and sales charts

### Administrator

- Access protected administrative pages
- Review pending product listings
- Approve suitable products
- Reject products with a reason
- Recheck and reject previously approved products
- Maintain product-quality control
- Review seller promotion information
- Communicate with Buyers and Sellers through live chat

## Product Moderation Workflow

1. A Seller creates or edits a product.
2. The product status becomes `Pending`.
3. An Administrator reviews the product.
4. The Administrator approves or rejects the product.
5. An approved product becomes `Active` and appears in the public catalogue.
6. A rejected product displays the rejection reason to its Seller.
7. An edited product returns to `Pending` for another review.
8. An Administrator can recheck and reject an approved product when necessary.

## Technologies Used

- ASP.NET Core Razor Pages
- C#
- Entity Framework Core
- Microsoft SQL Server
- Azure SQL Database
- ASP.NET Core Identity
- LINQ
- Bootstrap
- JavaScript
- jQuery
- Chart.js
- SignalR
- Frankfurter Currency API
- PayPal Sandbox/Payment Simulation
- Microsoft Azure App Service
- Visual Studio

## System Architecture

The application uses the Razor Pages request-and-response pattern:

1. The browser sends a request to a Razor Page.
2. The PageModel processes the request and validates the input.
3. ASP.NET Core Identity checks authentication and role permissions.
4. Entity Framework Core uses LINQ to access the SQL Server database.
5. The Razor Page displays the processed result to the user.

External communication is separated into service classes, including currency conversion and payment-related services.

## Database

The main entities include:

- `ApplicationUser`
- `Product`
- `ProductImage`
- `CartItem`
- `Order`
- `OrderItem`
- `Review`
- `AdminAction`
- `Notification`

Entity Framework Core migrations are used to create and update the database schema.

## External API Consumption

PC Parts Hub consumes the Frankfurter API to provide currency conversion.

The currency converter:

- Retrieves current exchange rates
- Supports different currency pairs
- Handles invalid or unavailable responses
- Uses `HttpClient` for external communication
- Displays a controlled error message if the service is unavailable

## Published API

The application publishes an endpoint that returns the number of approved products:

```text
/api/products/active-count
