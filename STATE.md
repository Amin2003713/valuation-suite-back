# Valuation Suite — Session State / Continuity Tracker

> This file tracks the current state of the ongoing work so any future session can resume
> exactly where we left off. **Update this file after every meaningful change.**

## Project layout (two repos)

| Repo | Path | Stack |
|------|------|-------|
| Backend | `C:\Users\amin\Desktop\valuation-suite-back\src` | .NET Clean Architecture (Domain, Application, Persistence, RequestHandlers, ApiFramework, Web.Api), EF Core + SQL Server, MediatR, FluentValidation |
| Frontend | `C:\Users\amin\Desktop\valuation-suite` | Next.js 14 (app router), React 18, Tailwind, TypeScript, RTL Persian UI |

Note: the shell project root here is the **backend** (`src/`). Frontend files are read/written
via absolute paths `C:/Users/amin/Desktop/valuation-suite/...`.

## Current phase (session 2 — all 13 tools migration)

**User decisions (confirmed via questions):**
1. **Calculators → typed per-tool CQRS commands** (one command+handler per calculator, typed
   DTOs, dedicated domain calculation service per tool; form definitions served from backend
   so the client is pure UI).
2. **All 13 tools migrated this session** (question tools + calculators), in dependency order.
3. **Persist everything** — calculator submissions saved like attempts/results (user history).
4. **patent-search stays client-side** for now (calls Lens.org directly from browser).

**Uncle Bob / clean code rules given by user:**
- Controllers stay **thin** — they only map HTTP → MediatR `Send()` (via `ExecuteAsync`).
- All logic lives in **RequestHandlers** (application layer) + domain services.
- Frontend = pure UI: renders questions/results; **backend owns math, scoring, data**.
- Client fetches questions one-by-one per tool; forms are backend-defined.

## Tool inventory (13 + auth + payments)

| # | Tool (route) | Type | Migration strategy |
|---|--------------|------|--------------------|
| 1 | ip-assessment | questions | ✅ already seeded as `IP-ASSESS` assessment (session 1) |
| 2 | idea-assessment | questions | seed as assessment (`IDEA-ASSESS`) |
| 3 | innovation-readiness | questions | seed as assessment (`INNOV-READ`) |
| 4 | ip-audit | questions | seed as assessment (`IP-AUDIT`) |
| 5 | job-evaluation | questions | seed as assessment (`JOB-EVAL`) |
| 6 | wipo-diagnostics | questions | seed as assessment (`WIPO-DIAG`) |
| 7 | patent-valuation (IPscore) | questions | seed as assessment (`IPSCORE`) |
| 8 | startup-valuation | calculator | typed command `RunStartupValuationCommand` |
| 9 | brand-valuation | calculator | typed command `RunBrandValuationCommand` |
| 10 | trademark-valuation | calculator | typed command `RunTrademarkValuationCommand` |
| 11 | general-ip-valuation | calculator | typed command `RunGeneralIpValuationCommand` |
| 12 | pharma-ip-valuation | calculator | typed command `RunPharmaIpValuationCommand` |
| 13 | intangible-assets | calculator | typed command `RunIntangibleAssetsCommand` |
| 14 | knowhow-valuation | calculator | typed command `RunKnowhowValuationCommand` |

**Question-tool shape:** `data.ts` with `sections[] → questions[] → options[]` + score
weights + result bands. Seeds map to existing `Assessment/Step/Question/Option` entities;
results via `QuestionEngine` (per-step scores → overall + level).

**Calculator shape:** `logic.ts` with `INITIAL_*` constants (the **form definition**) +
`compute()` (the **math engine**). Seeds serve `INITIAL_*` as form definitions via
`GET /api/valuations/{tool}/form`; `compute()` ported to a per-tool domain calculator
(`Core/Domain/Valuations/Calculators/*.cs`); results persisted as `ToolSubmission`.

## Done in session 1 (details in git history / earlier STATE.md)

- ✅ Build errors fixed (Domain→Common reference, CS0400)
- ✅ IdentityUser migration: `ApplicationUser : IdentityUser`, JWT auth stack
  (`AuthController`, `TokenService`, JWT config in Program.cs/appsettings)
- ✅ Zarinpal payment gateway: `ZarinpalService`, `PaymentsController` (start/callback/mine),
  `Payment` aggregate + EF config
- ✅ `IpAssessmentSeeder` (IP-ASSESS questions moved to backend, per-option scores,
  pre-question visibility gating)
- ✅ Frontend API client (`app/lib/api/client.ts`), auth API (`auth.ts`),
  zarinpal checkout starter
- ✅ Backend solution builds clean

## Session 2 progress

- [x] Read tool sources: 6 question `data.ts` files + 7 calculator `logic.ts` files
- [ ] Backend: `ToolSubmission` entity + EF config + DI + DbContext set
- [ ] Backend: form-definition storage + `GET /api/valuations/{tool}/form`
- [ ] Backend: seeders for 6 question tools
- [ ] Backend: 7 typed calculator commands + calculators + handlers
- [ ] Backend: thin `ValuationsController`
- [ ] Build backend
- [ ] Frontend: shared assessment API + wire 6 question tools
- [ ] Frontend: wire 7 calculators to compute APIs
- [ ] Frontend typecheck
- [ ] Final STATE.md update

## Key backend conventions (do not break)

- Requests live in `Core/Application/<Area>/Commands|Queries/...`, handlers in
  `Infrastructure/RequestHandlers/...`; controller `ExecuteAsync` → MediatR.
- Response DTOs in `Core/Application/.../Responses/` (records); mappers in `Mappers/`.
- Repos: `ICommandRepository<T>/IQueryRepository<T>` + typed interfaces in
  `Application/Interfaces/Repositories.cs`; implementations under
  `Infrastructure/Persistance/Repositories/`.
- Exceptions: `Common/Exceptions/*` (`ValuationException.NotFound/Conflict/BadRequest`, …).
- Seeding: `ISeedingDbContext` interface (Application) implemented by `SeedingDbContext`
  (Persistence); registered in `Infrastructure/Persistance/DependencyInjection.cs`; run from
  `Program.cs` via `app.SeedIpAssessmentAsync()` pattern.
- GlobalUsings in each project make common namespaces implicit.
- `ValuationDbContext : IdentityDbContext<ApplicationUser>`; `AddIdentityCore` in
  Persistence DI; JWT bearer in Web.Api.

## Key frontend conventions

- API client: `app/lib/api/client.ts` (`api.get/post/put`, JWT from `vs_auth_v1`).
- Auth context: `app/lib/store/AuthContext.tsx` (user, isPro, openAuthModal, …).
- Tools live in `app/<tool>/page.tsx` (+ `logic.ts`, `types.ts`, `tabs/`).
- Tool access gating: `app/components/auth/AccessGate.tsx` (`tier: free|pro`).
- Chart components in `app/components/charts/`.

## Remaining / next steps

- [ ] Run EF migrations against real DB and smoke-test auth + payment + seed flow end to end.
- [ ] Zarinpal production merchant + webhook signature validation.
- [ ] patent-search → backend proxy (deferred per user).
- [ ] Admin endpoints, refresh tokens, rate limiting.
