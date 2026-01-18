# HTTP Syntax Highlighting Test

This document tests the HTTP syntax highlighting feature in MarkMyWord.

## Basic HTTP Request

```http
GET /api/users HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
Accept: application/json
```

## HTTP POST Request with JSON Body

```http
POST /api/users HTTP/1.1
Host: api.example.com
Content-Type: application/json
Authorization: Bearer token123

{
  "name": "Alice Johnson",
  "email": "alice@example.com",
  "age": 30,
  "active": true
}
```

## HTTP Response with JSON Body

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-cache
X-Request-Id: abc-123-def

{
  "id": 42,
  "status": "success",
  "message": "User created successfully",
  "data": {
    "userId": 1001,
    "username": "alice_j",
    "created": true
  }
}
```

## HTTP Error Response

```http
HTTP/1.1 404 Not Found
Content-Type: application/json
Date: Mon, 15 Jan 2026 10:30:00 GMT

{
  "error": "ResourceNotFound",
  "message": "The requested user was not found",
  "code": 404
}
```

## HTTP PUT Request

```http
PUT /api/users/42 HTTP/1.1
Host: api.example.com
Content-Type: application/json

{
  "name": "Alice Smith",
  "email": "alice.smith@example.com"
}
```

## HTTP DELETE Request

```http
DELETE /api/users/42 HTTP/1.1
Host: api.example.com
Authorization: Bearer token123
```

## HTTP Response with Plain Text

```http
HTTP/1.1 200 OK
Content-Type: text/plain

Success! User has been deleted.
```

## Expected Rendering

When converted to Word, the following should be colored:

- **HTTP Methods** (GET, POST, PUT, DELETE): Blue (keywords)
- **URLs**: Orange (strings)
- **HTTP Version** (HTTP/1.1): Cyan (types)
- **Status Codes** (200, 404): Green (numbers)
- **Header Names**: Light blue (properties)
- **Header Values**: Dark gray (default)
- **JSON Property Names**: Light blue (properties)
- **JSON String Values**: Orange (strings)
- **JSON Numbers**: Green (numbers)
- **JSON Keywords** (true, false, null): Blue (keywords)
