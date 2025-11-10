# Bagile Platform API Reference

**Base URL:** `https://api.bagile.co.uk`

**Version:** 1.0.0

---

## 🔐 Authentication

All API requests require an API Key in the header.

```
X-Api-Key: your_api_key_here
```

### Example Request with Authentication

```bash
curl https://api.bagile.co.uk/api/orders \
  -H "X-Api-Key: YOUR_API_KEY"
```

### Authentication Errors

| HTTP Status | Error Response |
|-------------|----------------|
| 401 | `{"Code": "AuthenticationFailed", "Message": "API key is missing. Please provide X-Api-Key header.", "Status": 401}` |
| 401 | `{"Code": "AuthenticationFailed", "Message": "Invalid API key provided.", "Status": 401}` |
| 500 | `{"Code": "ConfigurationError", "Message": "API key is not configured on server", "Status": 500}` |

---

## 📦 Orders Endpoints

### 1. List All Orders

Retrieve a paginated list of orders with optional filtering.

```http
GET /api/orders
```

#### Query Parameters

| Parameter | Type | Required | Default | Description | Example |
|-----------|------|----------|---------|-------------|---------|
| `status` | string | No | - | Filter by order status | `completed`, `pending`, `processing`, `cancelled` |
| `from` | string (ISO 8601) | No | - | Filter orders from this date | `2025-01-01` or `2025-01-01T00:00:00Z` |
| `to` | string (ISO 8601) | No | - | Filter orders until this date | `2025-12-31` or `2025-12-31T23:59:59Z` |
| `email` | string | No | - | Filter by customer email | `customer@example.com` |
| `page` | integer | No | 1 | Page number (starts at 1) | `1`, `2`, `3` |
| `pageSize` | integer | No | 20 | Items per page (max: 100) | `10`, `20`, `50`, `100` |

#### Response

**Status:** `200 OK`

```json
{
  "items": [
    {
      "id": 123,
      "externalId": "12243",
      "source": "woo",
      "type": "public",
      "status": "completed",
      "totalAmount": 2520.00,
      "orderDate": "2025-10-23T10:30:00Z",
      "customerName": "Henry Heselden",
      "customerEmail": "henry@themdu.com",
      "billingCompany": "MDU Services Ltd",
      "enrolments": [
        {
          "enrolmentId": 456,
          "studentEmail": "john@example.com",
          "studentName": "John Doe",
          "courseName": "Professional Scrum Master",
          "courseStartDate": "2025-11-08T09:00:00Z"
        }
      ]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

#### Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `items` | array | Array of order objects |
| `page` | integer | Current page number |
| `pageSize` | integer | Number of items per page |
| `totalCount` | integer | Total number of orders matching filters |
| `totalPages` | integer | Total number of pages |
| `hasNextPage` | boolean | Whether there's a next page |
| `hasPreviousPage` | boolean | Whether there's a previous page |

#### Order Object Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Internal order ID |
| `externalId` | string | Order ID from source system (WooCommerce/Xero) |
| `source` | string | Source system: `woo` or `xero` |
| `type` | string | Order type: `public` or `private` |
| `status` | string | Order status: `completed`, `pending`, `processing`, `cancelled` |
| `totalAmount` | decimal | Total order amount |
| `orderDate` | string | Order date (ISO 8601) |
| `customerName` | string | Customer full name |
| `customerEmail` | string | Customer email address |
| `billingCompany` | string | Company name (if provided) |
| `enrolments` | array | Array of course enrolments for this order |

#### Enrolment Object Fields

| Field | Type | Description |
|-------|------|-------------|
| `enrolmentId` | integer | Enrolment ID |
| `studentEmail` | string | Student email address |
| `studentName` | string | Student full name |
| `courseName` | string | Course/event name |
| `courseStartDate` | string | Course start date (ISO 8601) |

#### Example Requests

**Get all orders (first page):**
```bash
curl https://api.bagile.co.uk/api/orders \
  -H "X-Api-Key: YOUR_API_KEY"
```

**Filter by status:**
```bash
curl "https://api.bagile.co.uk/api/orders?status=completed" \
  -H "X-Api-Key: YOUR_API_KEY"
```

**Filter by date range:**
```bash
curl "https://api.bagile.co.uk/api/orders?from=2025-01-01&to=2025-12-31" \
  -H "X-Api-Key: YOUR_API_KEY"
```

**Filter by customer email:**
```bash
curl "https://api.bagile.co.uk/api/orders?email=customer@example.com" \
  -H "X-Api-Key: YOUR_API_KEY"
```

**Pagination (page 2, 50 items per page):**
```bash
curl "https://api.bagile.co.uk/api/orders?page=2&pageSize=50" \
  -H "X-Api-Key: YOUR_API_KEY"
```

**Combined filters:**
```bash
curl "https://api.bagile.co.uk/api/orders?status=completed&from=2025-10-01&to=2025-10-31&page=1&pageSize=100" \
  -H "X-Api-Key: YOUR_API_KEY"
```

---

### 2. Get Single Order by ID

Retrieve detailed information about a specific order.

```http
GET /api/orders/{id}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | integer | Yes | Internal order ID |

#### Response

**Status:** `200 OK`

```json
{
  "id": 123,
  "externalId": "12243",
  "source": "woo",
  "type": "public",
  "status": "completed",
  "totalAmount": 2520.00,
  "orderDate": "2025-10-23T10:30:00Z",
  "customerName": "Henry Heselden",
  "customerEmail": "henry@themdu.com",
  "billingCompany": "MDU Services Ltd",
  "enrolments": [
    {
      "enrolmentId": 456,
      "studentEmail": "john@example.com",
      "studentName": "John Doe",
      "courseName": "Professional Scrum Master",
      "courseStartDate": "2025-11-08T09:00:00Z"
    }
  ]
}
```

**Status:** `404 Not Found`

```json
{
  "error": "Order 999 not found"
}
```

#### Example Request

```bash
curl https://api.bagile.co.uk/api/orders/123 \
  -H "X-Api-Key: YOUR_API_KEY"
```

---

## 🔔 Webhook Endpoints

Receive real-time notifications from external systems.

### WooCommerce Webhooks

```http
POST /webhooks/woo
```

**Authentication:** HMAC-SHA256 signature (not API key)

**Headers:**
```
Content-Type: application/json
X-WC-Webhook-Signature: <base64_signature>
X-WC-Webhook-Event: order.created
```

**Setup Instructions:**
1. Go to WooCommerce → Settings → Advanced → Webhooks
2. Click "Add webhook"
3. Configure:
   - **Delivery URL:** `https://api.bagile.co.uk/webhooks/woo`
   - **Secret:** Your WooCommerce webhook secret
   - **Topic:** `Order created`, `Order updated`
4. Save

---

### Xero Webhooks

```http
POST /webhooks/xero
```

**Authentication:** HMAC-SHA256 signature (not API key)

**Headers:**
```
Content-Type: application/json
X-Xero-Signature: <base64_signature>
```

**Setup Instructions:**
1. Go to Xero → Settings → Webhooks
2. Click "Add webhook"
3. Configure:
   - **Delivery URL:** `https://api.bagile.co.uk/webhooks/xero`
   - **Signing Key:** Your Xero webhook secret
4. Save

---

## 🔗 Xero OAuth Endpoints

### Initiate Xero Connection

```http
GET /xero/connect
```

**Authentication:** None required

**Description:** Redirects to Xero login to authorize the application.

**Usage:** Open this URL in a browser to connect your Xero account.

```
https://api.bagile.co.uk/xero/connect
```

---

### OAuth Callback

```http
GET /xero/callback?code={authorization_code}
```

**Authentication:** None required

**Description:** Xero redirects here after authorization. Handles token exchange automatically.

**Note:** This endpoint is called automatically by Xero. You don't need to call it manually.

---

## ❤️ Health & Monitoring

### Health Check

```http
GET /health
```

**Authentication:** None required

**Response:**
```
Healthy
```

**Status:** `200 OK`

**Example:**
```bash
curl https://api.bagile.co.uk/health
```

---

## 📊 Interactive Documentation

**Swagger UI:** `https://api.bagile.co.uk/swagger`

The Swagger interface provides:
- Interactive API testing
- Complete request/response schemas
- Try-it-out functionality
- Authentication testing

---

## 💻 Code Examples

### C# / .NET

```csharp
using System.Net.Http;
using System.Net.Http.Json;

var client = new HttpClient
{
    BaseAddress = new Uri("https://api.bagile.co.uk")
};

// Add API key to all requests
client.DefaultRequestHeaders.Add("X-Api-Key", "YOUR_API_KEY");

// Get all orders
var orders = await client.GetFromJsonAsync<OrderResponse>("/api/orders");

// Get orders with filters
var completedOrders = await client.GetFromJsonAsync<OrderResponse>(
    "/api/orders?status=completed&pageSize=50");

// Get specific order
var order = await client.GetFromJsonAsync<OrderDetail>("/api/orders/123");
```

---

### Python

```python
import requests

BASE_URL = "https://api.bagile.co.uk"
HEADERS = {"X-Api-Key": "YOUR_API_KEY"}

# Get all orders
response = requests.get(f"{BASE_URL}/api/orders", headers=HEADERS)
orders = response.json()

# Get orders with filters
params = {
    "status": "completed",
    "from": "2025-01-01",
    "to": "2025-12-31",
    "pageSize": 50
}
response = requests.get(f"{BASE_URL}/api/orders", headers=HEADERS, params=params)
filtered_orders = response.json()

# Get specific order
response = requests.get(f"{BASE_URL}/api/orders/123", headers=HEADERS)
order = response.json()
```

---

### JavaScript / Node.js

```javascript
const BASE_URL = 'https://api.bagile.co.uk';
const API_KEY = 'YOUR_API_KEY';

const headers = {
  'X-Api-Key': API_KEY
};

// Get all orders
const response = await fetch(`${BASE_URL}/api/orders`, { headers });
const orders = await response.json();

// Get orders with filters
const params = new URLSearchParams({
  status: 'completed',
  from: '2025-01-01',
  to: '2025-12-31',
  pageSize: 50
});

const filteredResponse = await fetch(
  `${BASE_URL}/api/orders?${params}`, 
  { headers }
);
const filteredOrders = await filteredResponse.json();

// Get specific order
const orderResponse = await fetch(
  `${BASE_URL}/api/orders/123`, 
  { headers }
);
const order = await orderResponse.json();
```

---

### cURL

```bash
# Set your API key
API_KEY="YOUR_API_KEY"

# Get all orders
curl https://api.bagile.co.uk/api/orders \
  -H "X-Api-Key: $API_KEY"

# Get completed orders in date range
curl "https://api.bagile.co.uk/api/orders?status=completed&from=2025-01-01&to=2025-12-31" \
  -H "X-Api-Key: $API_KEY"

# Get specific order
curl https://api.bagile.co.uk/api/orders/123 \
  -H "X-Api-Key: $API_KEY"

# Paginated results
curl "https://api.bagile.co.uk/api/orders?page=2&pageSize=50" \
  -H "X-Api-Key: $API_KEY"
```

---

## 🔍 Common Use Cases

### Get Today's Orders

```bash
TODAY=$(date -I)
curl "https://api.bagile.co.uk/api/orders?from=$TODAY&to=$TODAY" \
  -H "X-Api-Key: YOUR_API_KEY"
```

### Get All Completed Orders This Month

```bash
MONTH_START=$(date -d "$(date +%Y-%m-01)" -I)
MONTH_END=$(date -d "$(date +%Y-%m-01) +1 month -1 day" -I)

curl "https://api.bagile.co.uk/api/orders?status=completed&from=$MONTH_START&to=$MONTH_END" \
  -H "X-Api-Key: YOUR_API_KEY"
```

### Get Orders for Specific Customer

```bash
curl "https://api.bagile.co.uk/api/orders?email=customer@example.com" \
  -H "X-Api-Key: YOUR_API_KEY"
```

### Export All Orders (Paginated)

```python
import requests

BASE_URL = "https://api.bagile.co.uk"
HEADERS = {"X-Api-Key": "YOUR_API_KEY"}

all_orders = []
page = 1

while True:
    response = requests.get(
        f"{BASE_URL}/api/orders",
        headers=HEADERS,
        params={"page": page, "pageSize": 100}
    )
    data = response.json()
    
    all_orders.extend(data["items"])
    
    if not data["hasNextPage"]:
        break
    
    page += 1

print(f"Retrieved {len(all_orders)} orders")
```

---

## ⚠️ Error Responses

All error responses follow this format:

```json
{
  "Code": "ErrorCode",
  "Message": "Human-readable error message",
  "Status": 400
}
```

### Common Errors

| Status | Code | Message | Solution |
|--------|------|---------|----------|
| 401 | `AuthenticationFailed` | API key is missing | Add `X-Api-Key` header |
| 401 | `AuthenticationFailed` | Invalid API key provided | Check your API key is correct |
| 404 | - | Order {id} not found | Verify the order ID exists |
| 500 | `ConfigurationError` | API key is not configured on server | Contact support |

---

## 📋 Quick Reference Summary

### Base URL
```
https://api.bagile.co.uk
```

### Authentication Header
```
X-Api-Key: YOUR_API_KEY
```

### Endpoints

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| GET | `/api/orders` | ✅ Yes | List all orders (paginated, filterable) |
| GET | `/api/orders/{id}` | ✅ Yes | Get single order by ID |
| POST | `/webhooks/woo` | ❌ No (signature) | WooCommerce webhook receiver |
| POST | `/webhooks/xero` | ❌ No (signature) | Xero webhook receiver |
| GET | `/xero/connect` | ❌ No | Initiate Xero OAuth |
| GET | `/xero/callback` | ❌ No | Xero OAuth callback |
| GET | `/health` | ❌ No | Health check |
| GET | `/swagger` | ❌ No | Interactive API docs |

### Available Filters (GET /api/orders)

- `status` - Order status (completed, pending, processing, cancelled)
- `from` - Start date (ISO 8601)
- `to` - End date (ISO 8601)
- `email` - Customer email
- `page` - Page number (default: 1)
- `pageSize` - Results per page (default: 20, max: 100)

---

## 🆘 Support

**Email:** support@bagile.co.uk  
**Documentation:** https://api.bagile.co.uk/swagger  
**Status:** https://status.bagile.co.uk (coming soon)

---

**Last Updated:** November 2025  
**API Version:** 1.0.0  
**Base URL:** https://api.bagile.co.uk
