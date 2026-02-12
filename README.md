# Fin-Track-Invoice--API

.

🚀 FinTrack – Invoice & Payment Management API

FinTrack is a production-style backend API built using ASP.NET Core 8 and Entity Framework Core.

It provides:

Secure invoice lifecycle management

Payment processing

Refunds & payment reversals

Optimistic concurrency handling

Automatic audit logging

This project demonstrates real-world backend engineering patterns suitable for Junior Backend and Graduate Software Developer roles.

🛠 Tech Stack

✅ .NET 8

✅ ASP.NET Core Web API

✅ Entity Framework Core

✅ SQL Server (LocalDB)

✅ JWT Authentication

✅ Role-Based Authorization

✅ Swagger / OpenAPI

✅ Optimistic Concurrency (RowVersion)

✅ Audit Logging (SaveChangesInterceptor)

🔐 Authentication & Authorization

FinTrack uses JWT Bearer Authentication.

👥 Supported Roles

Admin

Finance

Viewer

🔎 Role Capabilities
Feature	Admin	Finance	Viewer
Create Invoice	✅	✅	❌
Issue Invoice	✅	✅	❌
Apply Payment	✅	✅	❌
Refund	✅	✅	❌
Reverse Payment	✅	✅	❌
View Invoices	✅	✅	✅

📌 Swagger includes an Authorize button for testing secured endpoints.

📦 Core Features
📄 Invoice Lifecycle

Invoices transition through:

Draft

Issued

Partially Paid

Paid

Cancelled

Automatic Calculations

Subtotal

VAT Total

Total

Paid Amount

Outstanding Amount

💳 Payments

Apply payments to issued invoices

Prevent overpayment

Prevent duplicate references

Automatically update invoice status

🔁 Payment Reversal

Reverse a specific payment

Link reversal to original payment

Prevent double reversal

Preserve full financial history

💰 Refunds

Process partial refunds

Prevent refund exceeding paid amount

Refunds recorded as negative payments

Automatically recalculate invoice status

🛡 Optimistic Concurrency

Uses SQL rowversion

Prevents lost updates

Returns HTTP 409 Conflict on concurrent modifications

🧾 Audit Logging

All create, update, and delete operations are automatically logged:

Entity type

Entity ID

Old values (JSON)

New values (JSON)

Performed by user

Timestamp

Implemented using a custom EF Core SaveChangesInterceptor.

🗄 Database Configuration
Default LocalDB Configuration
"ConnectionStrings": {
  "Sql": "Server=(localdb)\\MSSQLLocalDB;Database=FinTrackDb;Trusted_Connection=True;TrustServerCertificate=True"
}

▶️ Running the Project
1️⃣ Clone the Repository
git clone https://github.com/thabang56R/Fin-Track-Invoice--API.git

2️⃣ Restore Dependencies
dotnet restore

3️⃣ Apply Migrations
dotnet ef database update `
  --project src/FinTrack.Infrastructure `
  --startup-project src/FinTrack.Api `
  --context AppDbContext

4️⃣ Run the API
dotnet run --project src/FinTrack.Api

5️⃣ Open Swagger
http://localhost:5285/swagger

🏗 Architecture
Solution Structure
FinTrack.Domain         → Entities & Enums
FinTrack.Application    → DTOs & Business Logic
FinTrack.Infrastructure → EF Core, Audit Interceptor
FinTrack.Api            → Controllers, JWT Auth, Swagger

Architecture Principles

Separation of Concerns

Clean Layered Architecture

Domain-Driven Structure

Production-style error handling

🧠 What This Project Demonstrates

🔐 Secure API design

💰 Real financial logic

🔄 Concurrency handling

🧮 EF Core precision configuration

🗄 Database migrations

👥 Role-based access control

🧱 Clean layered architecture

⚠ Production-style error handling

🔮 Possible Future Improvements

Pagination & filtering

Reporting endpoints

Docker support

CI/CD pipeline

Integration testing

Multi-tenant support



