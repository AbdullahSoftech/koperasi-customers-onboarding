# FinTech Onboarding API

A clean, production-structured ASP.NET Core Web API implementing a full customer onboarding system for a cooperative (Koperasi) platform. The API covers new customer registration, existing customer migration, and multi-factor login — built using Clean Architecture, CQRS, and Entity Framework Core.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Database Setup](#database-setup)
- [Running the API](#running-the-api)
- [OTP Behavior (Mocked)](#otp-behavior-mocked)
- [OtpPurpose Enum Reference](#otppurpose-enum-reference)
- [API Flows & Testing Guide](#api-flows--testing-guide)
  - [Flow 1: New Customer Registration](#flow-1-new-customer-registration)
  - [Flow 2: Migrate Existing Customer](#flow-2-migrate-existing-customer)
  - [Flow 3: Customer Login](#flow-3-customer-login)
  - [Supporting Endpoints](#supporting-endpoints)
- [Error Codes Reference](#error-codes-reference)
- [API Response Format](#api-response-format)

---

## Architecture Overview

The solution follows **Clean Architecture** with a strict dependency rule — outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────┐
│                    API                      │  ← Controllers, Middleware, Swagger
├─────────────────────────────────────────────┤
│               Application                  │  ← CQRS Commands/Queries, Validators, DTOs
├─────────────────────────────────────────────┤
│               Infrastructure               │  ← EF Core, DbContext, Migrations
├─────────────────────────────────────────────┤
│                  Domain                    │  ← Entities, Enums (no dependencies)
├─────────────────────────────────────────────┤
│                  Shared                    │  ← ApiResponse wrapper, Constants
└─────────────────────────────────────────────┘
```

**Patterns used:**
- **CQRS** — Commands and Queries separated via MediatR
- **Validation Pipeline** — FluentValidation auto-runs before every handler via MediatR behavior
- **Repository abstraction** — `IAppDbContext` interface decouples Application from Infrastructure
- **Audit logging** — Every critical action is recorded in the `AuditLogs` table

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Database | SQL Server (LocalDB / SQL Express) |
| Mediator | MediatR |
| Validation | FluentValidation |
| Password Hashing | BCrypt.Net |
| API Docs | Swagger / Swashbuckle |

---

## Project Structure

```
Koperasi/
├── API/
│   ├── Controllers/
│   │   ├── AuthController.cs        # Login: initiate + complete (PIN verify)
│   │   ├── CustomersController.cs   # Registration, consent, PIN setup, biometric
│   │   ├── OtpController.cs         # ALL OTP operations (phone + email, all flows)
│   │   └── ContentController.cs     # Privacy policy content
│   ├── Middlewares/
│   │   └── ExceptionMiddleware.cs   # Global exception & validation error handling
│   └── Program.cs
│
├── Application/
│   ├── Commands/
│   │   ├── Auth/                    # InitiateLoginCommand, CompleteLoginCommand
│   │   ├── Customers/               # RegisterCustomerCommand, SetupPinCommand, ...
│   │   └── Otp/                     # SendOtpCommand, VerifyOtpCommand,
│   │                                #   SendEmailOtpCommand, VerifyEmailOtpCommand
│   ├── Queries/Customers/           # GetCustomerQuery
│   ├── Validators/                  # FluentValidation rules per command
│   ├── DTOs/                        # Request/Response models
│   ├── Behaviors/                   # MediatR validation pipeline behavior
│   └── Interfaces/                  # IAppDbContext abstraction
│
├── Domain/
│   ├── Entities/                    # Customer, OtpRequest, CustomerAuth, LoginSession...
│   └── Enums/                       # CustomerStatus, CustomerType, OtpPurpose
│
├── Infrastructure/
│   ├── Data/AppDbContext.cs         # DbContext with all entity configurations
│   └── Migrations/                  # EF Core migration files
│
└── Shared/
    ├── Wrappers/ApiResponse.cs      # Unified API response envelope
    └── Constants/AppConstants.cs
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express or LocalDB)
- Visual Studio 2022 v17.10+ or VS Code with C# extension

---

## Getting Started

### 1. Clone / Open the project

Open `Koperasi.slnx` in Visual Studio 2022.

### 2. Configure the connection string

Edit `API/appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=FinTechOnboardingDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Common values for `YOUR_SERVER`:
- Local SQL Express: `localhost\\SQLEXPRESS`
- LocalDB: `(localdb)\\MSSQLLocalDB`

---

## Database Setup

Run EF Core migrations to create the database and all tables:

```bash
# From the solution root
dotnet ef database update --project Infrastructure --startup-project API
```

Or in Visual Studio — open **Package Manager Console**, set Default Project to `Infrastructure`, then run:

```
Update-Database
```

This creates the following tables:

| Table | Purpose |
|---|---|
| `Customers` | Core customer profile (FullName, PhoneNumber, Email, NationalId) |
| `OtpRequests` | OTP records with purpose, expiry, attempt count, verification status |
| `CustomerAuths` | Hashed 6-digit PIN and biometric token |
| `PrivacyConsents` | Consent records with policy version and IP address |
| `MigrationRecords` | Tracks customers migrated from the old system |
| `LoginSessions` | Multi-step login session state (phone + email OTP flags) |
| `AuditLogs` | Immutable action history |

---

## Running the API

Press **F5** in Visual Studio or run:

```bash
dotnet run --project API
```

Swagger UI opens automatically at the root URL:

```
https://localhost:{PORT}/
```

All endpoints are documented and testable directly from Swagger.

---

## OTP Behavior (Mocked)

No SMS or email gateway is integrated. OTP codes are:

- Generated as a random **4-digit code** server-side
- **Logged to the application console output** — check the terminal/Output window for the code:
  ```
  [MOCK SMS]   OTP for +60123456789 (Registration): 7342 — expires at 2026-04-19 12:05:00
  [MOCK EMAIL] OTP for ali@example.com (RegistrationEmail): 5819 — expires at 2026-04-19 12:05:00
  ```
- Valid for **5 minutes** from generation
- Invalidated after successful verification — cannot be reused
- Maximum **3 verification attempts** before the OTP is locked

> In production these codes would be delivered via SMS/email and never exposed in logs.

---

## OtpPurpose Enum Reference

The `purpose` field in OTP requests is an integer corresponding to:

| Value | Name | Used in |
|---|---|---|
| `1` | Migration | `POST /api/otp/send` — migration phone OTP |
| `2` | Login | `POST /api/otp/send` — login phone OTP |
| `3` | EmailVerification | `POST /api/otp/email/send` — login email OTP |
| `4` | RegistrationEmail | `POST /api/otp/email/send` — registration email OTP |

> `0` (Registration) is **blocked** on `POST /api/otp/send`. The registration phone OTP is issued automatically when you call `POST /api/customers/register`.

---

## API Flows & Testing Guide

> **Tip:** Copy the IDs returned in each step — you will need them in the next step.

---

### Flow 1: New Customer Registration

**Full flow:** Provide details → Verify mobile OTP → Verify email OTP → Accept privacy policy → Set PIN → Confirm PIN → Enable biometric (optional)

---

#### Step 1 — Initiate Registration

Provide the customer's details. The API creates a pending account and **automatically sends an OTP to the mobile number**.

```http
POST /api/customers/register
Content-Type: application/json

{
  "fullName": "Ali Hassan",
  "icNumber": "900515145678",
  "mobileNumber": "+60123456789",
  "emailAddress": "ali@example.com"
}
```

**Success response `data`:**
```json
{
  "customerId": "aaaaaaaa-...",
  "otpRequestId": "bbbbbbbb-...",
  "expiresAt": "2026-04-19T12:05:00Z"
}
```

> Save `customerId` and `otpRequestId`. Check the console for the OTP code.

---

#### Step 2 — Verify Mobile OTP

```http
POST /api/otp/verify
Content-Type: application/json

{
  "otpRequestId": "<otpRequestId from step 1>",
  "phoneNumber": "+60123456789",
  "otpCode": "<4-digit code from console>"
}
```

**Success response:**
```json
{ "success": true, "message": "OTP verified successfully." }
```

---

#### Step 3 — Send Email OTP

```http
POST /api/otp/email/send
Content-Type: application/json

{
  "customerId": "<customerId from step 1>",
  "purpose": 4
}
```

> `purpose: 4` = RegistrationEmail

**Success response `data`:**
```json
{
  "otpRequestId": "cccccccc-...",
  "expiresAt": "2026-04-19T12:10:00Z"
}
```

> Save this new `otpRequestId`. Check the console for the email OTP code.

---

#### Step 4 — Verify Email OTP

```http
POST /api/otp/email/verify
Content-Type: application/json

{
  "customerId": "<customerId>",
  "otpRequestId": "<otpRequestId from step 3>",
  "otpCode": "<4-digit code from console>"
}
```

---

#### Step 5 — Get Privacy Policy (display to user)

```http
GET /api/content/privacy-policy
```

**Response `data`:**
```json
{
  "version": "v1.0",
  "title": "Privacy Policy",
  "effectiveDate": "2025-01-01",
  "content": "..."
}
```

---

#### Step 6 — Accept Privacy Policy

```http
POST /api/customers/{customerId}/consent
Content-Type: application/json

{
  "policyVersion": "v1.0",
  "isAccepted": true
}
```

---

#### Step 7 — Set Up 6-Digit PIN

```http
POST /api/customers/{customerId}/auth/setup-pin
Content-Type: application/json

{
  "pin": "123456"
}
```

> PIN must be exactly **6 digits**, numeric only.

---

#### Step 8 — Confirm PIN

Re-enter the same PIN to confirm it was typed correctly.

```http
POST /api/customers/{customerId}/auth/confirm-pin
Content-Type: application/json

{
  "pin": "123456"
}
```

---

#### Step 9 — Enable Biometric (Optional)

If the user skips this, the account is still fully active. The client can call this later.

```http
PUT /api/customers/{customerId}/auth/biometric
Content-Type: application/json

{
  "biometricToken": "device-biometric-token-xyz"
}
```

**Registration is now complete. The account status changes from `Pending` to `Active` after PIN setup.**

---

### Flow 2: Migrate Existing Customer

For customers already in the old system who need to be migrated to this platform.

---

#### Step 1 — Send Migration OTP

```http
POST /api/otp/send
Content-Type: application/json

{
  "phoneNumber": "+60129876543",
  "purpose": 1
}
```

> `purpose: 1` = Migration. The phone number must already exist in the system.

**Success response `data`:**
```json
{
  "otpRequestId": "dddddddd-...",
  "expiresAt": "2026-04-19T12:05:00Z"
}
```

---

#### Step 2 — Verify Migration OTP

```http
POST /api/otp/verify
Content-Type: application/json

{
  "otpRequestId": "<otpRequestId from step 1>",
  "phoneNumber": "+60129876543",
  "otpCode": "<4-digit code from console>"
}
```

---

#### Step 3 — Migrate Customer

```http
POST /api/customers/migrate
Content-Type: application/json

{
  "otpRequestId": "<otpRequestId from step 1>",
  "phoneNumber": "+60129876543",
  "oldSystemRef": "OLD-REF-001"
}
```

> `oldSystemRef` is optional — use it to link to the previous system's identifier.

**Success response `data`:** returns the customer profile with `id` (customerId).

---

#### Steps 4–8 — Same as Registration Flow

After migration, complete the same remaining steps:

| Step | Endpoint |
|---|---|
| Accept privacy policy | `POST /api/customers/{id}/consent` |
| Set up 6-digit PIN | `POST /api/customers/{id}/auth/setup-pin` |
| Confirm PIN | `POST /api/customers/{id}/auth/confirm-pin` |
| Enable biometric (optional) | `PUT /api/customers/{id}/auth/biometric` |

---

### Flow 3: Customer Login

**Full flow:** Enter IC number → Verify phone OTP → Verify email OTP → Enter PIN → Logged in

> If the IC number is not found the API returns `NOT_REGISTERED`. If the account exists but registration was never completed, it returns `REGISTRATION_INCOMPLETE`.

---

#### Step 1 — Initiate Login

```http
POST /api/auth/login/initiate
Content-Type: application/json

{
  "icNumber": "900515145678"
}
```

**Success response `data`:**
```json
{
  "customerId": "aaaaaaaa-...",
  "phoneNumber": "+60*****789",
  "maskedEmail": "al******@example.com",
  "loginSessionId": "eeeeeeee-..."
}
```

> Save `customerId` and `loginSessionId` — both are required throughout the login flow.

---

#### Step 2 — Send Phone OTP

```http
POST /api/otp/send
Content-Type: application/json

{
  "phoneNumber": "+60123456789",
  "purpose": 2
}
```

> `purpose: 2` = Login

**Success response `data`:**
```json
{
  "otpRequestId": "ffffffff-...",
  "expiresAt": "2026-04-19T12:05:00Z"
}
```

---

#### Step 3 — Verify Phone OTP

Pass `loginSessionId` here so the session tracks that phone OTP is done.

```http
POST /api/otp/verify
Content-Type: application/json

{
  "otpRequestId": "<otpRequestId from step 2>",
  "phoneNumber": "+60123456789",
  "otpCode": "<4-digit code from console>",
  "loginSessionId": "<loginSessionId from step 1>"
}
```

---

#### Step 4 — Send Email OTP

```http
POST /api/otp/email/send
Content-Type: application/json

{
  "customerId": "<customerId from step 1>",
  "purpose": 3,
  "loginSessionId": "<loginSessionId from step 1>"
}
```

> `purpose: 3` = EmailVerification (login flow)

**Success response `data`:**
```json
{
  "otpRequestId": "gggggggg-...",
  "expiresAt": "2026-04-19T12:10:00Z"
}
```

---

#### Step 5 — Verify Email OTP

```http
POST /api/otp/email/verify
Content-Type: application/json

{
  "customerId": "<customerId>",
  "otpRequestId": "<otpRequestId from step 4>",
  "otpCode": "<4-digit code from console>",
  "loginSessionId": "<loginSessionId from step 1>"
}
```

---

#### Step 6 — Complete Login (Enter PIN)

Enter the 6-digit PIN. If correct, the login session is consumed and you receive the customer profile.

```http
POST /api/auth/login/complete
Content-Type: application/json

{
  "loginSessionId": "<loginSessionId from step 1>",
  "customerId": "<customerId from step 1>",
  "pin": "123456"
}
```

**Success response `data`:**
```json
{
  "customerId": "aaaaaaaa-...",
  "fullName": "Ali Hassan",
  "phoneNumber": "+60123456789",
  "status": "Active",
  "loggedInAt": "2026-04-19T12:03:00Z"
}
```

**Login is now complete.** The session is marked consumed and cannot be reused.

---

### Supporting Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/customers/{id}` | Get customer profile by ID |
| `GET` | `/api/content/privacy-policy` | Retrieve current privacy policy text and version |

---

## Error Codes Reference

All failure responses include an error code string in the `errors` array that can be used for programmatic handling.

### OTP Errors

| Code | Meaning |
|---|---|
| `OTP_NOT_FOUND` | No OTP request found for the given ID |
| `OTP_ALREADY_USED` | OTP has already been verified |
| `OTP_EXPIRED` | OTP has passed its 5-minute expiry |
| `OTP_INVALID` | Wrong code entered (remaining attempts shown in message) |
| `OTP_MAX_ATTEMPTS` | 3 failed attempts — request a new OTP |
| `MOBILE_OTP_NOT_VERIFIED` | Email OTP requested before mobile OTP was verified |
| `PHONE_OTP_NOT_VERIFIED` | Login email OTP requested before phone OTP was verified |
| `USE_REGISTRATION_ENDPOINT` | Attempted to send Registration OTP via `/api/otp/send` |
| `INVALID_PURPOSE` | Email OTP purpose must be `3` or `4` |

### Registration Errors

| Code | Meaning |
|---|---|
| `PHONE_ALREADY_REGISTERED` | Mobile number already linked to an account |
| `IC_ALREADY_REGISTERED` | IC number already linked to an account |
| `EMAIL_ALREADY_REGISTERED` | Email address already linked to an account |
| `EMAIL_OTP_NOT_VERIFIED` | Consent attempted before email OTP was verified |
| `INVALID_PIN` | PIN is not exactly 6 numeric digits |
| `PIN_MISMATCH` | Confirm-PIN does not match the stored PIN |

### Login Errors

| Code | Meaning |
|---|---|
| `NOT_REGISTERED` | IC number not found — account does not exist |
| `REGISTRATION_INCOMPLETE` | Account exists but registration was never completed |
| `ACCOUNT_SUSPENDED` | Account is suspended — contact support |
| `SESSION_INVALID` | Login session not found, expired, or already consumed |
| `SESSION_REQUIRED` | `loginSessionId` missing for EmailVerification OTP |
| `PIN_INVALID` | Wrong PIN entered at login completion |
| `PIN_NOT_SETUP` | Account has no PIN — contact support |

### General Errors

| Code | Meaning |
|---|---|
| `CUSTOMER_NOT_FOUND` | No customer found with the given ID |
| `POLICY_NOT_ACCEPTED` | `isAccepted` must be `true` |
| `AUTH_NOT_FOUND` | No auth record found — set up PIN first |

---

## API Response Format

All endpoints return a consistent envelope:

**Success:**
```json
{
  "success": true,
  "message": "Human-readable message",
  "data": { },
  "errors": []
}
```

**Failure (business rule / validation):**
```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "errors": [
    "MobileNumber: Mobile number is required."
  ]
}
```

**HTTP status codes:**

| Code | When |
|---|---|
| `200` | Success |
| `400` | Validation error, business rule violation |
| `404` | Resource not found |
| `500` | Unhandled server error |
