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

## Current phase (session 3 — Admin area)

**User decisions (confirmed):**
1. **Roles + permissions** (Admin/Support/Analyst with granular permission claims).
2. **First admin seeded from appsettings** (AdminBootstrap section → AdminSeeder on startup).
3. **Admin UI in the same Next.js app at `/admin`** (shared API client, role-guarded).

**Admin feature scope:** customers (companies/users) list w/ search+paging, detail view
(info + payments + tool submissions), modify customer (plan, active, display name),
payments list, KPI dashboard. Permission-gated per feature.

## Session 3 plan

- [x] Backend: `AdminPermissions.cs` (Roles Admin/Support/Analyst, `Perms` constants,
      `RolePermissions` map, `PermissionPolicies` — policies named `perm:{permission}`)
- [x] Backend: `AdminModels.cs` (dashboard/customer/payment/submission records) +
      `AdminRequests.cs` (MediatR queries/commands)
- [x] Backend: `IAdminQueryRepository` (Application) → `AdminQueryRepository` (EF, Persistence,
      projections via `Db.Set<T>()` — DbContext has NO DbSet props, entities auto-discovered)
- [x] Backend: handlers in `Infrastructure/RequestHandlers/Admin/` — dashboard, customers
      (paged+search), customer detail (payments + submissions), payments (paged+filter),
      `UpdateAdminCustomerCommand` (plan via domain methods), `SetAdminRolesCommand`
- [x] Backend: `AdminBootstrapper` (creates roles; promotes `AdminBootstrap:Emails` from
      appsettings; register-then-restart for the first admin)
- [x] Backend: `AdminController` — thin, `[Authorize(Policy="perm:...")]` on every route;
      roles route requires `Roles="Admin"` too
- [x] Backend: TokenService stamps `perm:*` claims from roles; Program.cs adds policies +
      runs bootstrapper; appsettings `AdminBootstrap` section added
- [x] **Backend builds clean**
- [x] Frontend: `app/lib/api/admin.ts` (typed API client + JWT claim decoder for roles/perms)
- [x] Frontend: `/admin` layout — claim-based guard (client-side UX only; backend enforces),
      sidebar filtered by permissions, nav: dashboard / customers / payments
- [x] Frontend: `/admin` dashboard (KPIs: customers, revenue, usage; top tools table;
      recent customers)
- [x] Frontend: `/admin/customers` — search (debounced), plan/active filters, paging,
      status/plan badges
- [x] Frontend: `/admin/customers/[id]` — profile info, edit form (name/plan/expiry/active
      → `PUT customers/{id}`), role toggles (Admin-only → `PUT customers/{id}/roles`),
      payments table, tool-submission results (expandable `<details>` with raw JSON)
- [x] Frontend: `/admin/payments` — status filter, paging
- [x] Frontend typecheck passes (`npx tsc --noEmit` → 0 errors)

**Session 3 COMPLETE.**

### How to use the admin area
1. Register a normal account (e.g. `admin@yourdomain.com`) via `/api/auth/register`.
2. Put the email in `appsettings.json → AdminBootstrap:Emails`.
3. Restart the API — `AdminBootstrapper` creates roles and promotes that account to Admin.
4. Log in on the frontend with that account → navigate to `/admin`.
   (Re-login required for role claims to appear in the JWT.)

### Admin permission model (reference)
| Role | Permissions |
|------|-------------|
| Admin | all (dashboard.view, customers.read, customers.manage, payments.read, submissions.read) |
| Support | dashboard.view, customers.read, payments.read, submissions.read |
| Analyst | dashboard.view, submissions.read |

Roles → permissions are stamped as `perm` claims into the JWT at login; endpoints use
`[Authorize(Policy = "perm:...")]`. Role assignment UI is Admin-only and hides the
`SetAdminRolesCommand` path behind `Roles="Admin"`.

---

## Previous phase (session 2 — all 13 tools migration) ✅ COMPLETE

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

## Session 2 — DESIGN ACTUALLY IMPLEMENTED (differs from the original plan)

The original plan (per-tool CQRS commands + per-tool form endpoints) was **replaced by a
simpler, uniform design** after implementation began (fewer moving parts, same result):

- **One generic endpoint** instead of 13 typed commands:
  `POST /api/tools/{toolCode}/run` → `RunToolCommand` → `RunToolCommandHandler` →
  resolves `IToolRunner` by `ToolCode` → persists a submission → returns
  `{ submissionId, result }`.
- **Tool form definitions** live in the `ToolForm` entity
  (`Core/Domain/Tools/ToolForm.cs`), seeded by `ToolFormsSeeder`
  (defaults + reference data served to the client).
- **13 backend runners** (`Core/Application/Tools/Runners/*.cs`) — pure C# ports of the
  frontend `compute()` engines:

| ToolCode | Source |
|----------|--------|
| `IP-ASSESS` | session-1 seeder (`IpAssessmentSeeder`) |
| `IDEA-ASSESS` | `IdeaAssessmentRunner` |
| `INNOV-READ` | `InnovationReadinessRunner` |
| `IP-AUDIT` | `IpAuditRunner` (risk scores per record) |
| `JOB-EVAL` | `JobEvaluationRunner` |
| `WIPO-DIAG` | `WipoDiagnosticsRunner` |
| `IPSCORE` | `IpscoreRunner` (patent-valuation frontend) |
| `STARTUP-VAL` | `StartupValuationRunner` (9 methods + stage weights) |
| `BRAND-VAL` | `BrandValuationRunner` |
| `TRADEMARK-VAL` | `TrademarkValuationRunner` (6 methods + MC) |
| `PATENT-VAL` | `PatentValuationRunner` (general + pharma share the engine) |
| `PHARMA-IP` | same `PatentValuationRunner` |
| `INTANGIBLE` | `IntangibleAssetsRunner` |
| `KNOWHOW` | `KnowhowValuationRunner` |

- Results are **camelCase-serialized** to match the existing frontend tab types
  (with explicit `[JsonPropertyName]` for the few non-camelCase names the tabs expect:
  `Nd1`, `vals`, `mcResults`, `scenarioValues`, `allValues`).
- **`IToolRunner`** interface in `Core/Application/Tools/IToolRunner.cs`;
  runners registered via `ToolRunnerRegistration` in Application DI.
- Shared finance helpers: `Core/Application/Tools/Finance/` (`FinanceMath`, `WeightedRow`).
- Controller: `Web/Api/Controllers/ToolsController.cs` — thin, MediatR only.

## Session 2 progress — ALL COMPLETE ✅

- [x] Read tool sources: 6 question `data.ts` files + 7 calculator `logic.ts` files
- [x] Backend: `ToolForm` entity + EF config + DI + SeedingDbContext + `ToolFormsSeeder`
- [x] Backend: 13 `IToolRunner` implementations (pure C# math engines)
- [x] Backend: generic `RunToolCommand` + `GetToolFormQuery` handlers (CQRS, MediatR)
- [x] Backend: thin `ToolsController` (`GET /api/tools/{code}/form`, `POST /api/tools/{code}/run`)
- [x] Backend solution builds clean (0 errors)
- [x] Frontend: `app/lib/api/tools.ts` (`getToolForm`, `runTool`, `ToolRunResponse`)
- [x] Frontend: `useToolRunner` hook (question tools — step-by-step, answers→backend)
- [x] Frontend: `useBackendCompute` hook (calculators — debounced live compute on server)
- [x] Frontend: `AuthContext` wired to real backend API (`register/login/me`)
- [x] Frontend: 6 question tools rewired (`IDEA-ASSESS`, `INNOV-READ`, `JOB-EVAL`,
      `WIPO-DIAG`, `IPSCORE`, `IP-ASSESS`)
- [x] Frontend: 7 calculators rewired (`STARTUP-VAL`, `PATENT-VAL`, `PHARMA-IP`,
      `BRAND-VAL`, `INTANGIBLE`, `TRADEMARK-VAL`, `KNOWHOW`)
- [x] Frontend: `ip-audit` rewired to `IP-AUDIT` runner (dashboard KPIs + per-record
      risk scores + live modal risk preview — all computed server-side)
- [x] Frontend typecheck passes (`npx tsc --noEmit` → 0 errors)
- [x] Full solution builds (0 errors), frontend typecheck clean

**Client contract rules (verified per tool):**
- Frontend pages keep local form state; a `payload`/`S` object merges all state slices
  and is sent verbatim to the backend runner (case-insensitive binding via `ToolInput.Bind`).
- Calculators: `const { D, loading, error } = useBackendCompute(code, payload)`;
  pages render a "در حال محاسبه در سرور…" panel until the first `D` arrives.
- Question tools: `useToolRunner(code)` — fetches form/questions from backend, posts
  answers per step, receives scores/results.
- `patent-search` remains client-side (Lens.org) per user decision.

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

## EF Migrations (session 3)

- ✅ `Microsoft.EntityFrameworkCore.Design` 9.0.9 added to Persistence + Web.Api (EF tooling requirement)
- ✅ DI fix: `ValuationDbContext` registered via `AddDbContext` (Identity stores + EF tooling need it;
     before this, runtime startup would have failed)
- ✅ DI fix: `PharmaPatentValuationRunner` now shares the same singleton `PatentValuationRunner`
     instance (register concrete type + forward) — DI validation was failing before
- ✅ **`InitialCreate` migration generated**: `Infrastructure/Persistance/Migrations/20260916194536_InitialCreate.cs`
     (26 tables: AspNet* identity + Assessments, AssessmentVersions, Steps, Questions, Options,
     Answers, ScoredAnswers, AssessmentAttempts, AssessmentResults, Companies, Payments,
     ToolForms, ToolSubmissions, CalculationConfigs, Math*, ValidationRules, VisibilityConditions)

**Migration workflow (use these exact commands from `src/`):**
```bash
dotnet ef migrations add <Name> \
  --project Infrastructure/Persistance/Persistence.csproj \
  --startup-project Web/Api/Web.csproj \
  --context ValuationDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project Infrastructure/Persistance/Persistence.csproj \
  --startup-project Web/Api/Web.csproj \
  --context ValuationDbContext
```
Note: `--context ValuationDbContext` is required — the solution has 3 DbContext classes
(ValuationDbContext, WriteOnlyDbContext, ReadOnlyDbContext).

## Remaining / next steps

- [ ] `dotnet ef database update` (command above) against the dev SQL Server, then
      smoke-test auth + payment + seed + admin flow end to end.
- [ ] Smoke-test each tool page in the browser (backend running + logged in).
- [ ] Smoke-test the admin area (dashboard/customers/payments) with a seeded admin account.
- [ ] Zarinpal production merchant + webhook signature validation.
- [ ] patent-search → backend proxy (deferred per user).
- [ ] Refresh tokens, rate limiting.
