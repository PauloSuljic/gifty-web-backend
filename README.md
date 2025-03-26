# 🎁 Gifty Backend API

> The backend API for Gifty, built with **ASP.NET Core**, **PostgreSQL**, and **Firebase Auth**.

---

## 📡 Base URL

[https://gifty-api.azurewebsites.net/api](https://gifty-api.azurewebsites.net/api)

---

## 🛠 Tech Stack

- 🧱 **ASP.NET Core Web API**
- 🐘 **PostgreSQL** (via Azure)
- 🔐 **Firebase JWT Authentication**
- ♻️ **Entity Framework Core** (Code-first migrations)
- 🚦 **Redis** (rate limiting & caching)
- ☁️ **Azure Web App** (CI/CD via GitHub Actions)

---

## ✨ API Features

- 👤 User registration (after email verification via frontend)
- 📦 Wishlist CRUD
- 📌 Wishlist Item CRUD with reservation logic
- 🔗 Shareable wishlist links
- 🧠 Validation, authentication, error handling
- 🔐 Only 1 reserved item per wishlist per user

---

## 🧪 Development

```
git clone https://github.com/yourname/gifty-web-backend
cd gifty-web-backend
dotnet restore
dotnet ef database update
dotnet run

```

### Create appsettings.Development.json:

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=giftydb;Username=postgres;Password=password"
  }
}

```

---

## 🧱 Database Migrations
```
dotnet ef migrations add Init
dotnet ef database update
```

---

## 🔐 Firebase Auth Middleware
All protected routes use a custom FirebaseAuthenticationHandler middleware that:
- Validates Firebase ID tokens
- Extracts user UID from claims
- Verifies expiration and signature

---

## 📡 API Endpoints

| Method   | Endpoint                                         | Description                          | Auth Required |
|----------|--------------------------------------------------|--------------------------------------|---------------|
| `GET`    | `/api/users/{id}`                                | Get a specific user's profile        | ✅             |
| `POST`   | `/api/users`                                     | Create a new user                    | ✅             |
| `PUT`    | `/api/users/{id}`                                | Update user profile                  | ✅             |
| `GET`    | `/api/wishlists`                                 | Get all wishlists for logged-in user| ✅             |
| `POST`   | `/api/wishlists`                                 | Create a new wishlist                | ✅             |
| `DELETE` | `/api/wishlists/{id}`                            | Delete a wishlist                    | ✅             |
| `GET`    | `/api/wishlist-items/{wishlistId}`               | Get all items in a wishlist         | ✅             |
| `POST`   | `/api/wishlist-items`                            | Add item to wishlist                 | ✅             |
| `PATCH`  | `/api/wishlist-items/{itemId}`                   | Update item name/link                | ✅             |
| `DELETE` | `/api/wishlist-items/{itemId}`                   | Delete item from wishlist            | ✅             |
| `PATCH`  | `/api/wishlist-items/{itemId}/reserve`           | Reserve/unreserve an item            | ✅             |
| `POST`   | `/api/shared-links/{wishlistId}/generate`        | Generate shareable link              | ✅             |
| `GET`    | `/api/shared-links/{shareCode}`                  | Access a shared wishlist             | ❌             |

---

## 🔒 Rate Limiting
Powered by Redis (Azure or local)
- Prevents abuse by limiting unauthenticated requests
- Caches common GET requests
