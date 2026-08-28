# Payment Gateway API

A backend **payment gateway simulation** built with .NET, modeled after providers like Stripe and iyzico. It implements the *core logic* of a payment service provider — payment lifecycle, idempotency, and signed webhooks — as a portfolio project focused on clean, testable, enterprise-style backend design.

> ⚠️ **Scope:** This is a learning/portfolio project. It uses **standard test card numbers only** and never processes real card data (no PCI-DSS scope). The goal is to demonstrate payment-system *logic and patterns*, not to be a production gateway.

---

## Features

- **API key authentication** — merchants authenticate via `Authorization: Bearer sk_test_...` (custom middleware).
- **Payment lifecycle as a finite state machine** — `Pending → Authorized → Captured → Refunded`, plus `Authorized → Voided` and `Pending → Failed`. Invalid transitions are rejected with `409 Conflict`.
- **Card validation** — Luhn algorithm + test-card rules.
- **Idempotency** — an `Idempotency-Key` header prevents duplicate charges; the first response is stored and replayed for repeated requests.
- **Signed webhooks** — payment status changes are delivered to the merchant's URL via a background service, signed with **HMAC-SHA256**, with **retry + exponential backoff** on failure (outbox pattern).
- **Layered architecture** — Controller / Service / DTO / Data separation, `ServiceResult<T>` ↔ `ApiResponse<T>` pattern, automatic audit fields, merchant data isolation.
- **Unit tests** — xUnit with EF Core InMemory (Luhn, state-machine guards, idempotency).

## Tech Stack

.NET 10 · ASP.NET Core Web API · Entity Framework Core · PostgreSQL · xUnit · Scalar (OpenAPI UI) · Docker / OrbStack

---

## Payment State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Authorized: card approved
    Pending --> Failed: card declined
    Authorized --> Captured: capture
    Authorized --> Voided: void
    Captured --> Refunded: refund
    Failed --> [*]
    Voided --> [*]
    Refunded --> [*]
```

`Failed`, `Voided`, and `Refunded` are terminal states.

## API Endpoints

All endpoints are under `/v1` and require `Authorization: Bearer <apiKey>`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/v1/payments` | Create & authorize a payment (supports `Idempotency-Key`) |
| `GET` | `/v1/payments/{id}` | Get a payment (merchant-isolated) |
| `POST` | `/v1/payments/{id}/capture` | Capture an authorized payment |
| `POST` | `/v1/payments/{id}/void` | Void an authorized payment |
| `POST` | `/v1/payments/{id}/refund` | Refund a captured payment |

### Test cards

| Card number | Result |
|-------------|--------|
| `4242 4242 4242 4242` | Authorized |
| `4000 0000 0000 0002` | Failed (declined) |
| invalid Luhn | `400 Bad Request` |

### Example request

```bash
curl -X POST http://localhost:5142/v1/payments \
  -H "Authorization: Bearer sk_test_123456789" \
  -H "Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json" \
  -d '{ "amount": 100, "currency": "TRY", "cardNumber": "4242424242424242" }'
```

---

## Getting Started

### Option A — Run with Docker (recommended)

The only requirement is Docker (or [OrbStack](https://orbstack.dev)). One command builds the API image, starts PostgreSQL, applies migrations on startup, and runs everything together:

```bash
docker compose up --build
```

### Option B — Run locally (.NET SDK)

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) plus Docker for PostgreSQL.

```bash
# 1. Start PostgreSQL
docker run --name paymentgateway-db \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=paymentgateway \
  -p 5432:5432 -d postgres:17

# 2. Apply migrations
dotnet ef database update --project src/PaymentGateway.Api

# 3. Run the API
dotnet run --project src/PaymentGateway.Api --launch-profile http
```

### Once it's running

- API: `http://localhost:5142`
- Interactive API docs (Scalar): `http://localhost:5142/scalar`
- Seeded test merchant API key: `sk_test_123456789`

### Run the tests

```bash
dotnet test
```

---

## Project Structure

```
payment-gateway/
├── src/PaymentGateway.Api/
│   ├── Controllers/      # HTTP endpoints
│   ├── Services/         # Business logic (payments, idempotency, webhooks)
│   ├── Middleware/       # API key authentication
│   ├── Models/           # Entities + enums (state machine)
│   ├── DTOs/             # Request/response contracts
│   ├── Data/             # EF Core DbContext
│   └── Common/           # ServiceResult, ApiResponse, Luhn, HMAC
└── tests/PaymentGateway.Api.Tests/   # xUnit tests
```

## Roadmap

- [x] Dockerfile + docker-compose (API + Postgres)
- [ ] CI/CD pipeline (GitHub Actions: build + test)
- [ ] Cloud deployment (live URL + managed Postgres)
- [ ] Message queue for webhook delivery (RabbitMQ)
- [ ] Redis for idempotency cache + rate limiting
- [ ] Double-entry ledger & settlement reporting
