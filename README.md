# TechMove — Global Logistics Management System (GLMS)

[![.NET 8 CI](https://github.com/SDevelops11/TechMove/actions/workflows/ci.yml/badge.svg)](https://github.com/SDevelops11/TechMove/actions)
[![Automated Tests](https://img.shields.io/badge/tests-62%20passed%20(100%25)-brightgreen.svg)](TechMove.GLMS.Tests)
[![ASP.NET Core Web API](https://img.shields.io/badge/ASP.NET%20Core-8.0%20Web%20API-blue.svg)](TechMove.GLMS.API)
[![ASP.NET Core MVC](https://img.shields.io/badge/Frontend-ASP.NET%20Core%208.0%20MVC-512bd4.svg)](TechMove.GLMS.Web)
[![Docker](https://img.shields.io/badge/Docker-Multi--Container%20Compose-2496ed.svg)](docker-compose.yml)

**Module:** Enterprise Application Development (EAPD7111 / PROG7311)  
**Student:** Sahir Madurai (ST10544483)  
**Assessment:** Final Portfolio of Evidence (PoE)

---

## 1. Architectural Overview

TechMove Logistics is an international freight coordinator handling complex maritime, rail, and multimodal shipping agreements. In Part 3, the monolithic application was successfully decoupled into a **Service-Oriented Architecture (SOA)**, containerized for cloud-native deployment with **Docker & Docker Compose**, and fortified with an enterprise **62-test automated unit and integration suite**.

```
+----------------------------------------------------------------------------------------------------+
|                                    TECHMOVE GLMS CLOUD-NATIVE SOA                                  |
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|   +----------------------------+     HTTP REST / JWT      +------------------------------------+   |
|   |  glms-frontend-web (MVC)   | -----------------------> |      glms-backend-api (Web API)    |   |
|   |  - Port: 5001 (or 8080)    |                          |  - Port: 5000 (or 8080)            |   |
|   |  - Zero SQL dependencies   |                          |  - Swagger / OpenAPI Enabled       |   |
|   |  - Typed HttpClients       |                          |  - GoF Factory Pattern             |   |
|   |  - Error handling layers   |                          |  - GoF Observer Pattern (4 Depts)  |   |
|   |  - Razor Views & Bootstrap |                          |  - GoF Strategy Pattern (FX API)   |   |
|   +----------------------------+                          +------------------------------------+   |
|                                                                             |                      |
|                                                                     EF Core | TCP 1433             |
|                                                                             v                      |
|                                                           +------------------------------------+   |
|                                                           |      sql-server-db (Database)      |   |
|                                                           |  - Image: mssql/server:2022-latest |   |
|                                                           |  - Volume: glms-sql-data           |   |
|                                                           |  - Healthcheck enabled             |   |
|                                                           +------------------------------------+   |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

---

### 1. Web API Construction (The Backend)
- **Dedicated Project:** `TechMove.GLMS.API` talks directly to the database; MVC controllers no longer contain database access logic.
- **REST Endpoints & Verbs:**
  - `GET /api/contracts` (with LINQ filtering: date range, status, client, keyword).
  - `GET /api/contracts/{id}` (single contract retrieval with audit logs).
  - `POST /api/contracts` (creates contract via Factory Pattern, returns `201 Created` with Location header).
  - `PATCH /api/contracts/{id}/status` (updates status and triggers 4 departmental observers, returns `200 OK`).
  - `POST /api/contracts/{id}/agreement` & `GET /api/contracts/{id}/agreement` (PDF file upload/download).
  - `GET /api/clients` & `GET /api/clients/{id}` (client data).
  - `GET /api/servicerequests` & `GET /api/servicerequests/{id}`.
  - `POST /api/servicerequests` (strictly validates contract status, applies Strategy Pattern currency conversion, returns `201 Created`).
  - `GET /api/currency/convert` & `GET /api/currency/rate` (foreign exchange conversion).
  - `POST /api/auth/login` (issues cryptographic JWT Bearer access tokens).
- **Self-Documenting API:** Swagger / OpenAPI is enabled across all environments (`http://localhost:5000/swagger`) with interactive JWT Bearer authorization support.

### 2. Architectural Integrity & GoF Patterns
- **Factory Pattern (`Factories/`):** Instantiates `StandardContract`, `PremiumContract`, `InternationalContract`, or `ExpressContract` with dedicated SLA terms and value multipliers.
- **Observer Pattern (`Observers/`):** Broadcasts contract status transitions to 4 departmental listeners (*Operations*, *Finance*, *Client Services*, *Compliance*), persisting full audit trail logs.
- **Strategy Pattern (`Strategies/`):** Encapsulates algorithmic currency conversions (`UsdToZarConverter`, `EurToZarConverter`, `GbpToZarConverter`, `ApiBasedConverter`).
- **Clean Separation of Concerns:** Repositories (`IContractRepository`, etc.) and Service Layers (`IContractService`, etc.) separate persistence logic from HTTP endpoints with extensive Dependency Injection.

### 3. Frontend Refactoring & Decoupling
- **Zero Database Dependency:** `TechMove.GLMS.Web` contains **no Entity Framework packages** and **no connection strings**.
- **Typed `HttpClient` Service Layer:** `IContractApiClient`, `IServiceRequestApiClient`, `IClientApiClient`, `ICurrencyApiClient`, and `IAuthApiClient` manage all API calls, JSON serialization, and bearer token attachment.
- **Robust Error Handling:** Seamlessly intercepts API HTTP status errors (`400 Bad Request`, `404 Not Found`, `401 Unauthorized`) and presents descriptive feedback to users via `TempData` and `ModelState`.
- **JWT Authentication:** Dedicated `AccountController` with Login UI allowing authentication via JWT tokens, with demo accounts pre-configured for evaluation.

### 4. Automated Integration Testing
- **62 Total Automated Tests Passing (100% Green):**
  - **40 Unit Tests:** Covering Factory logic, Observer notifications, Strategy math, PDF magic bytes validation, and status transitions.
  - **22 API Integration Tests:** Utilizing `WebApplicationFactory<Program>` with an in-memory database.
- **Rigorous Verifications:**
  - Verifies data integrity across write and read operations (**Create-then-Read**).
  - Validates HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found`).
  - Enforces workflow policies: **Service requests on Expired or On Hold contracts are strictly rejected with HTTP 400**.

### 5. Docker Configuration & Optimization
- **Optimized Multi-Stage Builds:** `TechMove.GLMS.API/Dockerfile` and `TechMove.GLMS.Web/Dockerfile` feature 4 distinct stages (`base`, `build`, `publish`, `final`).
- **Lean Runtime:** Uses `mcr.microsoft.com/dotnet/aspnet:8.0` for minimal image size and fast spin-up; compiles inside `mcr.microsoft.com/dotnet/sdk:8.0`.
- **Correct Port Exposition:** Internal ports standardized on `8080`.

### 6. Container Orchestration (Docker Compose)
- **3-Tier Ecosystem in `docker-compose.yml`:**
  1. `sql-server-db` (SQL Server 2022 on port `1433`) with healthcheck and persistent volume `glms-sql-data`.
  2. `glms-backend-api` (ASP.NET Core Web API on port `5000:8080`) connecting to `sql-server-db` via internal Docker DNS.
  3. `glms-frontend-web` (ASP.NET Core MVC on port `5001:8080`) connecting to `glms-backend-api` via internal Docker DNS.
- **Internal Networking:** Dedicated bridge network `glms-internal-net` ensuring container communication via service names.

### 7. Technical Reflection Repor
- Comprehensive academic report analyzing:
  - Why automated testing is critical in a CI/CD pipeline and how it prevents production defects.
  - How Docker containerization guarantees environmental parity across Dev, Test, and Prod, solving "it works on my machine".

---

## 3. Quick Start & Execution Guide

### Option A: Run via Docker Compose (Recommended for Full Ecosystem)
Ensure Docker Desktop is running, then execute:
```powershell
docker compose up --build
```
Access the running ecosystem:
- **MVC Frontend Web App:** [http://localhost:5001](http://localhost:5001)
- **Web API Swagger UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **SQL Server Database:** `localhost,1433` (`sa` / `TechMove#2026_EnterpriseSecure!`)

To stop the containers:
```powershell
docker compose down
```

---

### Option B: Run Locally via .NET 8 CLI

#### Step 1: Start Backend Web API
```powershell
dotnet run --project TechMove.GLMS.API/TechMove.GLMS.API.csproj --urls "http://localhost:5000"
```
The API Swagger documentation will be available at [http://localhost:5000/swagger](http://localhost:5000/swagger).

#### Step 2: Start MVC Frontend Web App (In a second terminal)
```powershell
dotnet run --project TechMove.GLMS.Web/TechMove.GLMS.Web.csproj --urls "http://localhost:5001"
```
Navigate to [http://localhost:5001](http://localhost:5001) in your browser.

---

### Option C: Execute Automated Test Suite (62 Tests)
Run all 62 unit and integration tests across the solution:
```powershell
dotnet test TechMove.sln --logger "console;verbosity=normal"
```

---

## 4. Evaluation & Demo Credentials

To test role-based authentication and JWT Bearer security:

| Account Role | Email / Username | Password | Access Capabilities |
| :--- | :--- | :--- | :--- |
| **Solutions Architect (Admin)** | `admin@techmove.com` | `Admin@123` | Full system access, all contract approvals & audit trails |
| **Logistics Lead (Manager)** | `manager@techmove.com` | `Manager@123` | Contract management, rate conversions, service dispatching |
| **Haulage Coordinator (Operator)** | `operator@techmove.com` | `Operator@123` | Service request tracking and operational logging |

---

## Option B: Run Locally via .NET 8 CLI

#### Step 1: Start Backend Web API
```powershell
dotnet run --project TechMove.GLMS.API/TechMove.GLMS.API.csproj --urls "http://localhost:5000"
```
The API Swagger documentation will be available at [http://localhost:5000/swagger](http://localhost:5000/swagger).

#### Step 2: Start MVC Frontend Web App (In a second terminal)
```powershell
dotnet run --project TechMove.GLMS.Web/TechMove.GLMS.Web.csproj --urls "http://localhost:5001"
```
Navigate to [http://localhost:5001](http://localhost:5001) in your browser.

---

### Option C: Execute Automated Test Suite (62 Tests)
Run all 62 unit and integration tests across the solution:
```powershell
dotnet test TechMove.sln --logger "console;verbosity=normal"
```

---

## 4. Evaluation & Demo Credentials

To test role-based authentication and JWT Bearer security:

| Account Role | Email / Username | Password | Access Capabilities |
| :--- | :--- | :--- | :--- |
| **Solutions Architect (Admin)** | `admin@techmove.com` | `Admin@123` | Full system access, all contract approvals & audit trails |
| **Logistics Lead (Manager)** | `manager@techmove.com` | `Manager@123` | Contract management, rate conversions, service dispatching |
| **Haulage Coordinator (Operator)** | `operator@techmove.com` | `Operator@123` | Service request tracking and operational logging |