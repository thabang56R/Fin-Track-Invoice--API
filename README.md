

.![CI](https://github.com/thabang56R/Fin-Track-Invoice--API/actions/workflows/ci.yml/badge.svg)

🚀 # FinTrack – Invoice & Payment Management API



Production-style backend API built with **ASP.NET Core (.NET 8)** and **Entity Framework Core** for managing invoices, payments, refunds, and payment reversals with **JWT auth**, **role-based access**, **audit logging**, and **optimistic concurrency**.

---

## 🚀 Features

### 📄 Invoice lifecycle
- **Draft → Issued → PartiallyPaid → Paid**
- Cancel invoice (only allowed when no payments exist)
- Automatic totals:
  - Subtotal
  - VAT total
  - Total
  - Paid amount
  - Outstanding amount

### 💳 Payments
- Apply payments to **Issued** invoices
- Prevent overpayment
- Prevent duplicate payment references (per invoice)
- Status recalculated after each payment

### 🔁 Payment reversal
- Reverse a **specific payment**
- Prevent double-reversal
- Keeps financial history (reversal recorded as a negative payment)

### 💰 Refunds
- Supports partial refunds
- Prevent refund > paid amount
- Refund recorded as a negative payment

### 🧾 Audit logging
All create/update/delete operations are captured automatically:
- Entity type + entity id
- Old values (JSON)
- New values (JSON)
- Performed by user
- Timestamp

Implemented using an EF Core `SaveChangesInterceptor`.

### 🛡 Optimistic concurrency
- Uses SQL `rowversion`
- Prevents lost updates
- Returns **HTTP 409 Conflict** on concurrent modifications

---

## 🛠 Tech Stack

- **.NET 8** / ASP.NET Core Web API
- Entity Framework Core
- SQL Server (LocalDB)
- JWT Authentication + Role-based Authorization
- Swagger / OpenAPI
- Optimistic concurrency (RowVersion)
- Audit logging interceptor
- Unit tests (xUnit + FluentAssertions)

---

## 🔐 Authentication & Roles

Supported roles:
- `Admin`
- `Finance`
- `Viewer`

| Feature | Admin | Finance | Viewer |
|--------|:-----:|:------:|:------:|
| Create invoice | ✅ | ✅ | ❌ |
| Issue invoice | ✅ | ✅ | ❌ |
| Apply payment | ✅ | ✅ | ❌ |
| Refund | ✅ | ✅ | ❌ |
| Reverse payment | ✅ | ✅ | ❌ |
| View invoices | ✅ | ✅ | ✅ |

Swagger includes an **Authorize** button for testing secured endpoints.

---

## ⚙️ Configuration

Example `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Sql": "Server=(localdb)\\MSSQLLocalDB;Database=FinTrackDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "FinTrack",
    "Audience": "FinTrack",
    "Key": "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_32_CHARS_MINIMUM"
  }
}
