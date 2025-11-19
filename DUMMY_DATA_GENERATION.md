# Dummy Data Generation for Sales Insights Testing

## Overview

This feature provides automated generation of random dummy purchases for testing the AI-powered sales insights at `/Analytics/Insights`. It's designed for DEV/QA environments only and is disabled by default in production.

## Quick Start

### 1. Enable the Feature (Development Only)

The feature is already enabled in `appsettings.Development.json`:

```json
{
  "Features": {
    "EnableDummyDataGeneration": true
  }
}
```

⚠️ **Security Note**: This feature is disabled by default in production. Never enable it in `appsettings.json` (production configuration).

### 2. Generate Dummy Orders

**Endpoint**: `POST /api/admin/generate-dummy-data?count={number}`

**Parameters**:
- `count` (optional): Number of orders to generate (default: 50, max: 1000)

**Example using curl**:
```bash
# Generate 50 dummy orders (default)
curl -X POST http://localhost:5000/api/admin/generate-dummy-data

# Generate 100 dummy orders
curl -X POST http://localhost:5000/api/admin/generate-dummy-data?count=100
```

**Example using PowerShell**:
```powershell
# Generate 50 dummy orders (default)
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/admin/generate-dummy-data"

# Generate 100 dummy orders
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/admin/generate-dummy-data?count=100"
```

**Response**:
```json
{
  "message": "Successfully generated 50 dummy orders",
  "ordersCreated": 50,
  "orderLinesCreated": 142,
  "totalRevenue": 8547.23,
  "dateRange": {
    "from": "2024-12-20T14:35:33.049Z",
    "to": "2025-01-19T14:35:33.049Z"
  }
}
```

### 3. View Generated Data

Navigate to the Insights dashboard to see the generated test data:

**URL**: `https://localhost:5001/Analytics/Insights`

The dashboard will show:
- Summary cards with total orders, revenue, and average order value
- AI-generated insights based on the dummy data
- Trends and patterns
- Top products
- Daily sales charts

### 4. Cleanup Dummy Data

**Endpoint**: `DELETE /api/admin/cleanup-dummy-data`

**Example using curl**:
```bash
curl -X DELETE http://localhost:5000/api/admin/cleanup-dummy-data
```

**Example using PowerShell**:
```powershell
Invoke-RestMethod -Method Delete -Uri "http://localhost:5000/api/admin/cleanup-dummy-data"
```

**Response**:
```json
{
  "message": "Successfully deleted 50 dummy orders",
  "deletedCount": 50
}
```

## How It Works

### Data Generation

1. **Customer IDs**: Each order uses a random test customer ID with format `testuser_XXXX` (e.g., `testuser_5432`)
2. **Products**: Randomly selects 1-5 active products from the product catalog for each order
3. **Quantities**: Each product line has 1-3 items
4. **Order Dates**: Randomly distributed across the last 30 days
5. **Order Status**: Weighted towards realistic statuses (mostly "Paid" and "Shipped")
6. **Prices**: Uses actual product prices from the catalog

### Data Cleanup

The cleanup endpoint removes all orders where `CustomerId` starts with `testuser_`. This ensures:
- Only test data is removed
- Real customer data is never affected
- Complete cleanup of related OrderLine records (via EF Core cascade delete)

## Integration with Analytics

The generated dummy data:
- Creates proper `Order` and `OrderLine` records in the database
- Is immediately visible in the `/Analytics/Insights` dashboard
- Works with all existing analytics features (trends, AI insights, charts)
- Follows the same data structure as real orders

## Security & Production Deployment

### Development/QA Only

This feature is **disabled by default** and should **never be enabled in production**:

```json
// ❌ DON'T enable in appsettings.json (production)
{
  "Features": {
    "EnableDummyDataGeneration": true  // ❌ NEVER do this in production
  }
}

// ✅ DO enable only in appsettings.Development.json
{
  "Features": {
    "EnableDummyDataGeneration": true  // ✅ Safe for dev/QA
  }
}
```

### API Endpoint Security

Current implementation:
- Endpoints are only registered when the feature flag is enabled
- Feature flag defaults to `false` if not specified
- Endpoints follow the `/api/admin/*` pattern for administrative operations

**Recommended for production deployment**:
If you need to enable this in a QA/staging environment:
1. Use environment variables or Azure App Configuration
2. Add authentication/authorization to the endpoints
3. Implement IP whitelisting or network isolation
4. Add rate limiting to prevent abuse

## Use Cases

### 1. QA Testing
```bash
# Generate realistic test data
curl -X POST http://localhost:5000/api/admin/generate-dummy-data?count=200

# Run your tests
# ...

# Clean up afterwards
curl -X DELETE http://localhost:5000/api/admin/cleanup-dummy-data
```

### 2. Demo Preparation
```bash
# Generate 100 orders for a demo
curl -X POST http://localhost:5000/api/admin/generate-dummy-data?count=100

# Show the insights dashboard to stakeholders
# Navigate to /Analytics/Insights
```

### 3. Performance Testing
```bash
# Generate large dataset
curl -X POST http://localhost:5000/api/admin/generate-dummy-data?count=1000

# Test analytics performance with more data
```

## Troubleshooting

### Feature Not Available

**Problem**: Endpoints return 404 Not Found

**Solution**: 
1. Verify `appsettings.Development.json` has the feature enabled
2. Ensure you're running in Development environment
3. Check the application logs for feature registration

### No Products Available

**Problem**: Error "No active products available to generate dummy orders"

**Solution**: 
1. Run the application once to seed initial products
2. Check that products exist and are active: `SELECT * FROM Products WHERE IsActive = 1`

### Data Not Appearing in Insights

**Problem**: Generated orders don't show in the dashboard

**Solution**:
1. Refresh the Insights page
2. Check the date range filter (default is 30 days)
3. Verify orders were created: `SELECT * FROM Orders WHERE CustomerId LIKE 'testuser_%'`

## Advanced Usage

### Scripted Data Generation

Create a PowerShell script for repeatable test scenarios:

```powershell
# generate-test-data.ps1
param(
    [int]$OrderCount = 50,
    [string]$BaseUrl = "http://localhost:5000"
)

# Clean up any existing dummy data
Write-Host "Cleaning up existing dummy data..."
Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/admin/cleanup-dummy-data"

# Generate fresh dummy data
Write-Host "Generating $OrderCount dummy orders..."
$result = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/generate-dummy-data?count=$OrderCount"
Write-Host "Created: $($result.ordersCreated) orders, $($result.orderLinesCreated) order lines"
Write-Host "Total Revenue: £$($result.totalRevenue)"
```

### CI/CD Integration

Add to your test pipeline:

```yaml
# Example GitHub Actions workflow step
- name: Generate Test Data
  run: |
    curl -X POST http://localhost:5000/api/admin/generate-dummy-data?count=100
    
- name: Run Integration Tests
  run: dotnet test
  
- name: Cleanup Test Data
  run: |
    curl -X DELETE http://localhost:5000/api/admin/cleanup-dummy-data
```

## Implementation Details

### Files Added/Modified

**New Files**:
- `Services/IDummyDataService.cs` - Service interface
- `Services/DummyDataService.cs` - Service implementation
- `DUMMY_DATA_GENERATION.md` - This documentation

**Modified Files**:
- `Program.cs` - Service registration and API endpoints
- `appsettings.Development.json` - Feature flag configuration

### Dependencies

This feature uses existing infrastructure:
- Entity Framework Core for database operations
- Existing `Order`, `OrderLine`, and `Product` models
- Standard ASP.NET Core minimal API pattern
- No additional NuGet packages required

## Support

For issues or questions:
1. Check the application logs for detailed error messages
2. Review the Troubleshooting section above
3. Verify your environment configuration
4. Ensure you're running in Development environment

---

**Ready to generate test data?** Just run the application and call the API endpoints!
