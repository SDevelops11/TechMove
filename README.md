# TechMove — Global Logistics Management System (GLMS)

[![.NET 8 CI](https://github.com/placeholder/TechMove/actions/workflows/ci.yml/badge.svg)](https://github.com/)
[![Tests](https://img.shields.io/badge/tests-40%20passed-brightgreen.svg)](TechMove.GLMS.Tests)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0%20MVC-blue.svg)](https://dotnet.microsoft.com/)

**Module:** Enterprise Application Development (EAPD7111 / PROG7311)  
**Student:** Sahir Madurai (ST10544483)  
**Assessment:** Formative 1 — Part 2 Prototype & Unit Testing  

---

## 🚚 Overview
TechMove Logistics GLMS is an enterprise-grade platform designed to coordinate global freight agreements, multimodal haulage schedules, and service invoices. This Part 2 deliverable implements the core **ASP.NET Core 8.0 MVC Monolith** prototype with Entity Framework Core, translating the three Gang of Four (GoF) design patterns established in Part 1 into working C# code.

---

## 🛠️ Key Architectural Implementations
1. **Database & EF Core Architecture**:
   - Normalized schema (`Clients`, `Contracts`, `ServiceRequests`, `ContractStatusLogs`).
   - Fluent API configuration with key constraints, unique indexes, and referential integrity (`DeleteBehavior.Restrict`).
   - Seed data included on initialization and standalone SQL script in `Database/CreateGLMSDatabase.sql`.
2. **Design Patterns (Direct Translation from Part 1)**:
   - **Factory Pattern (`Factories/`)**: Instantiates `StandardContract`, `PremiumContract`, `InternationalContract`, or `ExpressContract` with dedicated SLA rules and multipliers.
   - **Observer Pattern (`Observers/`)**: Broadcasts contract status transitions to 4 departmental listeners: *Operations*, *Finance*, *Client Services*, and *Compliance*, persisting audit trail logs.
   - **Strategy Pattern (`Strategies/`)**: Encapsulates currency conversion algorithms (`UsdToZarConverter`, `EurToZarConverter`, `GbpToZarConverter`, `ApiBasedConverter`).
3. **Workflow & LINQ Rules**:
   - Strict business enforcement: **Service Requests cannot be created against Expired or On Hold contracts**.
   - Admin search & filtering powered by LINQ expressions for date ranges, status, and client criteria.
4. **Resilient External Currency Exchange API Integration**:
   - Consumes live foreign exchange rates via `HttpClient` using `async`/`await`.
   - In-memory caching with 60-minute TTL and fallback spot rates for offline resilience.
   - Live interactive AJAX widget recalculating ZAR costs in real time.
5. **Secure PDF File Storage**:
   - UUID collision prevention (`Guid.NewGuid()`).
   - Strict triple-check validation: file extension (`.pdf`), MIME type (`application/pdf`), and magic bytes (`%PDF` binary header).
   - Working UI download stream endpoint.
6. **40-Test xUnit Suite & CI/CD**:
   - Covers Currency Math, File Validation, Workflow logic, and edge cases (zero values, negative inputs, `.exe` files, null inputs).
   - Automated via GitHub Actions (`.github/workflows/ci.yml`).

---

## 🚀 Quick Start

### Build & Run Web Application
```powershell
dotnet run --project TechMove.GLMS.Web/TechMove.GLMS.Web.csproj
```
Navigate to `https://localhost:7085` in your web browser.

### Run Automated Tests
```powershell
dotnet test TechMove.sln --logger "console;verbosity=detailed"
```

For complete marking rubric breakdown and video presentation guide, see [PART_2_DOCUMENTATION.md](file:///c:/Users/Sahir/Desktop/Stuff/_Rosebank%20College/EAPD7111/TechMove/PART_2_DOCUMENTATION.md).
