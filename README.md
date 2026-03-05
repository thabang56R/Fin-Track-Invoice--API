
.![CI](https://github.com/thabang56R/Fin-Track-Invoice--API/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Docker](https://img.shields.io/badge/Docker-Ready-blue)
![OpenAI](https://img.shields.io/badge/OpenAI-Embeddings_%2B_AI-black)
![License](https://img.shields.io/badge/License-MIT-green)

## 🚀  FinTrack – Invoice & Payment Management API

---
## 📌 Project Overview

FinTrack Invoice API is an enterprise-style financial tracking REST API designed to manage invoicing workflows, payment tracking, and financial records through a structured and secure backend system.

The API provides endpoints for creating, updating, and managing invoices while ensuring data integrity and security through modern backend engineering practices.

This project demonstrates how backend systems used in financial applications are built using scalable architecture, proper authentication mechanisms, and reliable database management.

Key backend engineering concepts implemented in this project include:

- RESTful API architecture
- JWT-based authentication and authorization
- Entity Framework Core for data persistence
- Optimistic concurrency control to prevent conflicting updates
- Audit logging for tracking system changes
- Automated testing using xUnit
- Interactive API documentation using Swagger/OpenAPI

The system is designed to mimic real-world financial services APIs used by accounting systems, SaaS platforms, and enterprise invoicing tools.

---

## 🌟 Vision

The long-term vision for FinTrack is to evolve into a full financial management backend platform capable of supporting modern invoicing and financial workflows for small businesses and SaaS applications.

The goal of FinTrack is to demonstrate how to build secure, scalable, and maintainable backend services aligned with real-world financial system architecture. 

---

## Future improvements planned for the project include:

- Customer and vendor management

- Invoice line items with tax and discount calculations

- Multi-currency support

- Payment gateway integration (Stripe / PayFast)

- Invoice PDF generation

- Email notifications for invoice reminders

- Financial reporting dashboards (revenue, outstanding invoices)

- Background jobs for scheduled invoice reminders

- Role-based access control for multi-user environments

- Cloud deployment with CI/CD pipelines 

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

---

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

## 🧩  System Architecture Focus

The FinTrack API follows a layered backend architecture designed for maintainability and scalability.

Client  Application 

(Web / Mobile / Frontend)


⬇


ASP.NET Core Web API  

(Controllers, Business Logic)


⬇

Entity Framework Core  

(Data Access Layer)


⬇

SQL Server Database  

(Persistent Storage)


⬇

Authentication Layer  

(JWT Security)


⬇

Monitoring & Documentation  

(Swagger / OpenAPI)

---

## 📡 Example API Request

Create Invoice

POST /api/invoices

Request Body:

{

  "customerName": "Acme 
  Corporation",
  
  "amount": 2500,
  
  "dueDate": "2026-04-15",
  
  "status": "Pending"
  
}


Response:


{

  "id": 12,
  
  "customerName": "Acme Corporation",
  
  "amount": 2500,
  
  "status": "Pending",
  
  "createdAt": "2026-03-01T10:25:00Z"
  
}

---

## 📚 API Documentation

Once the API is running, interactive documentation is available via Swagger:


http://localhost:5000/swagger


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

## 👨‍💻 Author

**Thabang Rakeng**  
Full-Stack Developer | AI-Focused Backend Engineer 
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





---

   
 
