# Auth Service API Documentation

## 📋 Overview

| Property | Value |
|----------|-------|
| **Base URL (Direct)** | `http://localhost:5000` |
| **Base URL (Gateway)** | `http://localhost:5010/api/auth` |
| **Port** | 5000 |
| **JWT Secret** | `ducanhdeptrai123_ducanhdeptrai123` |

---

## 🔓 Public APIs (No Authentication Required)

### 1. Register

```
POST /auth/register
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
    "email": "test@example.com",
    "password": "Password123!",
    "fullName": "Test User"
}
```

**Success Response (200):**
```json
{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@example.com",
    "fullName": "Test User"
}
```

**Error Response (400):**
```json
{
    "message": "Email already exists"
}
```

---

### 2. Login

```
POST /auth/login
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
    "email": "test@example.com",
    "password": "Password123!"
}
```

**Success Response (200):**
```json
{
    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refresh_token": "abc123def456..."
}
```

**Error Response (401):**
```
Unauthorized (empty body)
```

---

### 3. Refresh Token

```
POST /auth/refresh
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
    "refreshToken": "your-refresh-token-here"
}
```

**Success Response (200):**
```json
{
    "access_token": "new-access-token...",
    "refresh_token": "new-refresh-token..."
}
```

---

### 4. Logout

```
POST /auth/logout
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
    "refreshToken": "your-refresh-token-here"
}
```

**Success Response (200):**
```json
{
    "message": "Logged out successfully"
}
```

---

### 5. Send Verification Email

```
POST /auth/send-verification-email
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
    "email": "test@example.com"
}
```

**Success Response (200):**
```json
{
    "message": "Verification email sent successfully"
}
```

---

### 6. Verify Email

```
POST /auth/verify-email
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
    "token": "verification-token-from-email"
}
```

**Success Response (200):**
```json
{
    "message": "Email verified successfully. Your account has been activated."
}
```

---

## 🔐 Protected APIs (Requires Authentication)

> **Note:** All protected APIs require the `Authorization` header with a valid JWT token.

```
Authorization: Bearer <access_token>
```

---

### 7. Get Current User

```
GET /auth/me
```

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@example.com",
    "fullName": "Test User",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z"
}
```

---

### 8. Get User Permissions

```
GET /auth/permissions
```

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@example.com",
    "roles": [
        {
            "id": "role-id-1",
            "name": "Admin",
            "description": "Administrator role"
        }
    ]
}
```

---

### 9. Get All Roles

```
GET /roles
```

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
[
    {
        "id": "role-id-1",
        "name": "Admin",
        "description": "Administrator role"
    },
    {
        "id": "role-id-2",
        "name": "Seller",
        "description": "Seller role"
    }
]
```

---

## 👑 Admin Only APIs

> **Note:** These APIs require the user to have the `Admin` role.

---

### 10. Create Role

```
POST /roles
```

**Headers:**
```
Content-Type: application/json
Authorization: Bearer <admin_access_token>
```

**Request Body:**
```json
{
    "name": "Manager",
    "description": "Manager role with limited admin access"
}
```

**Success Response (201):**
```json
{
    "id": "new-role-id",
    "name": "Manager",
    "description": "Manager role with limited admin access"
}
```

---

### 11. Update Role

```
PUT /roles/{id}
```

**Headers:**
```
Content-Type: application/json
Authorization: Bearer <admin_access_token>
```

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | GUID | Role ID |

**Request Body:**
```json
{
    "name": "Senior Manager",
    "description": "Senior Manager role"
}
```

**Success Response (200):**
```json
{
    "id": "role-id",
    "name": "Senior Manager",
    "description": "Senior Manager role"
}
```

---

### 12. Delete Role

```
DELETE /roles/{id}
```

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | GUID | Role ID |

**Success Response (200):**
```json
{
    "message": "Role deleted successfully"
}
```

---

### 13. Assign Role to User

```
POST /user-roles
```

**Headers:**
```
Content-Type: application/json
Authorization: Bearer <admin_access_token>
```

**Request Body:**
```json
{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "roleId": "role-id-here"
}
```

**Success Response (201):**
```json
{
    "id": "user-role-id",
    "userId": "user-id",
    "roleId": "role-id"
}
```

---

### 14. Remove Role from User

```
DELETE /user-roles/{userId}/{roleId}
```

**Headers:**
```
Authorization: Bearer <admin_access_token>
```

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| userId | GUID | User ID |
| roleId | GUID | Role ID |

**Success Response (200):**
```json
{
    "message": "Role removed from user"
}
```

---

## 📊 API Summary

| Method | Endpoint | Auth | Admin | Description |
|--------|----------|:----:|:-----:|-------------|
| POST | `/auth/register` | ❌ | ❌ | Register new user |
| POST | `/auth/login` | ❌ | ❌ | Login and get tokens |
| POST | `/auth/refresh` | ❌ | ❌ | Refresh access token |
| POST | `/auth/logout` | ❌ | ❌ | Logout |
| POST | `/auth/send-verification-email` | ❌ | ❌ | Send email verification |
| POST | `/auth/verify-email` | ❌ | ❌ | Verify email |
| GET | `/auth/me` | ✅ | ❌ | Get current user |
| GET | `/auth/permissions` | ✅ | ❌ | Get user roles |
| GET | `/roles` | ✅ | ❌ | List all roles |
| POST | `/roles` | ✅ | ✅ | Create role |
| PUT | `/roles/{id}` | ✅ | ✅ | Update role |
| DELETE | `/roles/{id}` | ✅ | ✅ | Delete role |
| POST | `/user-roles` | ✅ | ✅ | Assign role |
| DELETE | `/user-roles/{userId}/{roleId}` | ✅ | ✅ | Remove role |

---

## 🧪 Quick Test with cURL

### Register
```bash
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!","fullName":"Test User"}'
```

### Login
```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'
```

### Get Current User
```bash
curl -X GET http://localhost:5000/auth/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## 🔗 Gateway Routes

When accessing through the API Gateway (`http://localhost:5010`):

| Direct URL | Gateway URL |
|------------|-------------|
| `http://localhost:5000/auth/register` | `http://localhost:5010/api/auth/register` |
| `http://localhost:5000/auth/login` | `http://localhost:5010/api/auth/login` |
| `http://localhost:5000/auth/me` | `http://localhost:5010/api/auth/me` |
| `http://localhost:5000/roles` | `http://localhost:5010/api/roles/*` |

