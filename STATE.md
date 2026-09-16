# Valuation Suite — Session State / Continuity Tracker

> This file tracks the current state of the ongoing work so any future session can resume
> exactly where we left off. **Update this file after every meaningful change.**

## Project layout (two repos)

| Repo | Path | Stack |
|------|------|-------|
| Backend | `C:\Users\amin\Desktop\valuation-suite-back\src` | .NET 10 Clean Architecture (Domain, Application, Persistence, RequestHandlers, ApiFramework, Web.Api), EF Core 9 + SQL Server, MediatR, FluentValidation |
| Frontend | `C:\Users\amin\Desktop\valuation-suite` | Next.js 14 (app router), React 18, Tailwind, TypeScript, RTL Persian UI |

Note: the shell project root here is the **backend** (`src/`). Frontend files are read/written
via absolute paths `C:/Users/amin/Desktop/valuation-suite/...`.

## Client note (task origin)

Client said: IP-assessment questions are **hard-coded in the frontend** (`app/lib/ip-assessment/questions.ts`)
and do **not** need the math engine. Move them to the backend (as seed data with scoring),
and connect the frontend to the backend APIs.

## Task list (from user)

1. ✅ Fix build errors
2. ✅ Optimise the user part → use `IdentityUser` (ASP.NET Core Identity + JWT)
3. ✅ Add Zarinpal payment gateway (request → sandbox/pay redirect → callback verify → upgrade plan)
4. ✅ Move hardcoded IP-assessment questions to backend seed (with per-option scores)
5. ✅ Connect frontend to backend APIs (auth client + IP-assessment fetches + zarinpal checkout)
6. ✅ Build + verify

## Completed work

### 1. Build errors (fixed)
- `Core/Domain/Common/BaseEntity.cs` referenced `global::Common.Base.BaseEntity` but Domain.csproj
  had no reference to `Common`. → Added ProjectReference to `..\..\Common\Common.csproj`.
- Removed now-redundant EFCore package ref from Domain.csproj (Common already provides it).

### 2. IdentityUser user part (backend)
- Added packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (9.0.9) to Persistence,
  `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.0) + `Microsoft.AspNetCore.Identity.UI`? —
  (only JwtBearer + Identity.EntityFrameworkCore were needed) to Web.Api.
- New domain:
  - `Core/Domain/Users/ApplicationUser.cs` — `ApplicationUser : IdentityUser` with `DisplayName`,
    `Plan`, `PlanExpiresAt`, `CompanyId`.
  - `Core/Domain/Users/Plan.cs` — enum Free/Pro (+ helpers `IsPro`, `DisplayName`).
  - **Legacy `Core/Domain/Users/User.cs` and `Domain.Users.Plan` were removed** (Identity is now
    the single user model). `UserConfiguration.cs` removed; Identity tables mapped automatically
    via `builder.Services.AddIdentityCore<ApplicationUser>()...AddEntityFrameworkStores`.
- `ValuationDbContext` now derives `IdentityDbContext<ApplicationUser>`, sets
  `AddIdentityCore` in `Persistence/DependencyInjection.cs` (options: require unique email,
  min password len 6, no lockout in dev).
- **Auth stack (JWT)**:
  - `Web/Api/Services/TokenService.cs` — issues JWT (claims: sub=userId, email, name, plan).
  - `Web/Api/Services/CurrentUser.cs` + extension `GetUserId()` reading `sub`/NameIdentifier.
  - Controllers: `Web/Api/Controllers/AuthController.cs` (register/login/me endpoints).
  - DTOs in `Web/Api/Dto/AuthDtos.cs` (RegisterRequest, LoginRequest, AuthResponse, UserDto).
  - JWT config in `Web/Api/Program.cs` + `appsettings.json` (`Jwt:Issuer,Audience,Key,ExpiryMinutes`).
  - `[Authorize]` on Attempts/Results controllers; `[AllowAnonymous]` on health/auth.
- Note: Identity endpoints are **controller-based JWT**, not NextAuth-style cookies; frontend
  stores the JWT in localStorage.

### 3. Zarinpal payment gateway (backend)
- `Web/Api/Services/ZarinpalService.cs` — `IHttpClientFactory`-based:
  - `CreateAsync(amountToman, description, callbackUrl, email?, mobile?)` → POST
    `https://payment.zarinpal.com/pg/v4/payment/request.json` (sandbox host auto-swapped when
    `Zarinpal:Sandbox=true`), returns `authority`.
  - `VerifyAsync(amountToman, authority)` → POST `.../verify.json`, returns ok/refId.
  - DTOs + `ZarinpalOptions` (MerchantId, Sandbox, StartPayUrl).
- `ZarinpalOptions` bound from `appsettings.json` `Zarinpal` section (MerchantId placeholder,
  Sandbox=true, TomanAmount=100000 → 10,000 Toman default).
- Endpoints in `Web/Api/Controllers/PaymentsController.cs`:
  - `POST /api/payments/zarinpal/start` → creates a `Payment` row (Pending), returns StartPay URL
    + authority.
  - `GET  /api/payments/zarinpal/callback` → verifies, upgrades user plan to Pro, marks Payment
    Paid, redirects to frontend `?payment=success|failed`.
  - `GET  /api/payments/mine` → payment history (authorized).
- Domain: `Core/Domain/Payments/Payment.cs` (`PaymentAggregate`), `PaymentStatus`, EF config
  `PaymentConfiguration.cs`, registered `IPaymentCommandRepository/IPaymentQueryRepository` +
  implementations. `ValuationDbContext` gains `DbSet<Payment> Payments`.

### 4. Hardcoded questions → backend seed
- Frontend source of truth copied verbatim from `app/lib/ip-assessment/questions.ts`:
  - 10 PRE_QUESTIONS + 6 sections (trademark, confidential, designs, inventive, employment,
    website) with their DETAILED_QUESTIONS.
- Backend files:
  - `Core/Application/Assessments/Seeding/IpAssessmentSeeder.cs` — static definition of the
    assessment (Code="IP-ASSESS", name FA), steps per section, questions with options and
    **scores** (index-based `(index+1)*20` to preserve frontend scoring semantics), visibility
    rules for pre-question branching (product→inventive, materials→confidential, designs→designs,
    trademark→trademark, website→website, employees→employment).
  - Seeding is applied on startup: `Web/Api/Program.cs` → `app.SeedIpAssessmentAsync()`
    (idempotent — checks by Code; skips if exists or version already seeded).
- Frontend `questions.ts` now just types + re-export note; real data comes from API.

### 5. Frontend ↔ backend connection
- `app/lib/api/client.ts` — fetch wrapper: base URL from `NEXT_PUBLIC_API_URL` (default
  `http://localhost:5001`), attaches `Authorization: Bearer <jwt>`, JSON handling, `ApiError`.
- `app/lib/api/auth.ts` — register/login/me/logout against `/api/auth/*`, stores JWT+user in
  localStorage (`vs_auth_v1`), replaces old mock `store.ts` for auth flows.
- `app/lib/api/ipAssessment.ts` — `getIpAssessmentForClient()` (published version by code),
  `getOrCreateAttempt()`, `syncAnswers()`, `completeAttempt()`, `getResult()` — mapped to the
  shape IpAssessment.tsx already uses (sections/questions/labels kept from API payload).
- `app/lib/store/AuthContext.tsx` — rewritten to use real API (async login/register, JWT,
  `ready` gate, `refresh()`); `AuthModal` updated to await login/register and show API errors.
- `app/lib/store/store.ts` — auth functions removed; kept only session-snapshot helpers
  (still used by other tools) + `upgradeToPro` now calls zarinpal start endpoint.
- `app/components/ip-assessment/IpAssessment.tsx` — loads sections/questions from backend on
  mount (falls back to bundled copy if API down → keeps app usable), sends `syncAnswers` on
  section completion, and posts `complete` + fetches result to show API-driven result banner.
- `.env.local.example` added: `NEXT_PUBLIC_API_URL`, `ZARINPAL_MERCHANT_ID` note.

### 6. Verification
- `dotnet build valuation-suite.sln` — 0 errors, 0 warnings (except pre-existing NU1603 in tests).
- Frontend `npx tsc --noEmit` — passes (types only, no runtime run yet).

## How to run

Backend:
```bash
cd /c/Users/amin/Desktop/valuation-suite-back/src
dotnet run --project Web/Api   # https://localhost:5101 by default (see launchSettings)
```
- Update `Web/Api/appsettings.json` → `ConnectionStrings:AssessmentDb` if DB differs.
- Migrations: `dotnet ef migrations add Init -p Infrastructure/Persistance -s Web/Api`
  then `dotnet ef database update -p Infrastructure/Persistance -s Web/Api`.
  (Identity + Payments + Assessment tables all included.)

Frontend:
```bash
cd /c/Users/amin/Desktop/valuation-suite
cp .env.local.example .env.local   # set NEXT_PUBLIC_API_URL if backend not on :5101
npm run dev
```

Zarinpal: set `Zarinpal:MerchantId` in appsettings.json (or env `Zarinpal__MerchantId`).
Sandbox mode is on by default → payments use sandbox.zarinpal.com.

## Remaining / next steps (not started)

- [ ] Run EF migrations against real DB and smoke-test auth + payment + seed flow end to end.
- [ ] Zarinpal production merchant + webhook signature validation (currently callback trusts
      the verify call only).
- [ ] Replace remaining tools' hardcoded data (startup-valuation, brand-valuation, …) the same
      way as IP-assessment when client approves.
- [ ] Refresh-token / token revocation, rate limiting on auth endpoints.
- [ ] `docs/IMPLEMENTATION_PLAN.md` Phase 2 items (admin endpoints etc.) — partially done:
      auth + payments now exist; admin companies CRUD still open.
