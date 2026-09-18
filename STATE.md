# Valuation Suite — Session State / Continuity Tracker

> This file tracks the current state of the ongoing work so any future session can resume
> exactly where we left off. **Update this file after every meaningful change.**

## Current phase (session 7 — IN PROGRESS: plans page, callback page, account area, adviser everywhere)

### Session 7 scope (user request)
1. "Upgrade to Pro" must route to a **/plans** page where the user picks a plan duration and pays.
2. User can **edit profile** and see **payment history** and **tool usage history** → /account.
3. **/payment/callback?payment=success** page — Zarinpal redirect target; refreshes session.
4. Payment wired into tools: paying for a tool's advanced section unlocks it immediately.
5. **Adviser for ALL tools** (advice price on every tool) + adviser page active in admin area.
6. Test all pages, fix errors.

### Session 7 progress — BACKEND ✅ COMPLETE
- [x] `Payment.Months` (Pro subscription length) + migration `PaymentMonths` **generated + applied**.
- [x] Checkout `kind=pro` now takes `months` (1/3/6/12) with **tiered server-computed pricing**
      (base monthly × months × discount 1.0/0.9/0.8/0.65); callback extends Pro by months
      (`AddMonths`, no longer the fixed ProPlanDurationDays).
- [x] **Profile API (MediatR, thin controller)**: `GET/PUT /api/auth/profile` →
      `GetMyProfileQuery` + `UpdateMyProfileCommand` (name + phone with 09xxxxxxxxx validation),
      handlers in `Infrastructure/RequestHandlers/Users/ProfileHandlers.cs`.
- [x] **Adviser on ALL tools**: seeder `ToolFormsSeeder.Pricing` defaults advanced/advice/route
      for all 13 tools (JOB-EVAL fully free, advice-only); live DB updated via SQL — every tool
      now has AdvicePriceToman > 0.
- [x] Build clean.

### Session 7 remaining — FRONTEND ✅ COMPLETE (typecheck 0 errors)
- [x] **/plans** — Pro duration picker (1/3/6/12 months with tiered prices + discount tags),
      packages grid, per-tool unlock cards; all CTAs checkout through Zarinpal;
      unauthenticated users get the auth modal. /pricing Pro banner now links here.
- [x] **/payment/callback** — reads ?payment=success|failed&message=…, re-fetches /me so
      plan/entitlements land instantly, success → حساب من/home, failure → retry /plans.
- [x] **/account** — 3 tabs: profile (edit name + phone via PUT /api/auth/profile),
      payments history (/api/payments/mine with status/kind badges + refId), usage history
      (/api/tools/submissions with tool/score/date). Header avatar menu → حساب من +
      ارتقا به Pro (→ /plans).
- [x] **AdvancedLock** — now offers **direct checkout** for that tool's advanced sections
      (button label shows the DB price) with /plans fallback.
- [x] **Admin sidebar** — new "صف کارشناسان" entry pointing to /adviser (visible with
      submissions.read; the /adviser layout already accepts Admin + Adviser roles).
- [x] globals.css: .link-btn + neutral account-dropdown link styles.
- [x] `npx tsc --noEmit` → 0 errors.

### Session 7 remaining — E2E verify (next)
- [ ] Start API, verify: pro checkout months=6 prices with 0.8 tier, profile GET/PUT,
      pricing catalog shows advice prices on all 13 tools, callback redirect hits
      /payment/callback?payment=success.

---

## Session 6 archive (✅ COMPLETE — monetization rollout to all tools + live Zarinpal sandbox e2e)

### Fixes
- [x] **Zarinpal `metadata.mobile must be a string`** — v4 rejects nulls inside metadata;
      `CreatePaymentAsync` now builds a `Dictionary<string,string>` metadata (email/mobile
      only when present) and omits metadata entirely when both are empty.
- [x] **Zarinpal error parsing** — v4 `errors` can be object/array-of-objects/array-of-strings;
      `ZarinpalResponse<T>.Errors` is now raw `JsonElement?` + `ErrorMessage()` that extracts
      the message from any shape (was crashing the 500 handler before).
- [x] **Callback save bug (real money bug)** — the callback loaded the Payment through the
      READ repo but saved via the WRITE repo → status/RefId/PaidAt changes were silently
      lost (user got Pro but payment stayed AwaitingGateway). Now loads via
      `paymentCommands.Table` so the mutation is tracked and persisted.

### Rollout to ALL tools (frontend)
- [x] `useBackendCompute` now returns `advancedIncluded` (from run response) alongside
      `submissionId`; `useToolMeta(toolCode)` returns price/advice/route meta.
- [x] **AdvancedLock + NotesPanel wired into every calculator**: startup-valuation,
      trademark-valuation, knowhow-valuation, general-ip-valuation, pharma-ip-valuation,
      intangible-assets, brand-valuation (lock under ToolLayout top; notes in the
      final/summary tab). Question tool idea-assessment got NotesPanel in the results card.
- [x] Frontend typecheck clean.

### Live Zarinpal sandbox verification (merchant configured, Sandbox=true)
- [x] Pro checkout → authority `S0000…` + `sandbox.zarinpal.com/pg/StartPay/…` gateway URL;
      sandbox page has OK/NOK verify links → clicking OK marks session paid → callback
      → **payment status=3 Paid, refId=495529401, paidAt set, user plan=Pro isPro=true**,
      re-login token → `advancedIncluded:true` on paid tools.
- [x] Tool checkout (BRAND-VAL 150000 Toman from DB, server-computed) → paid → perpetual
      `UserToolAccess` grant → `advancedIncluded:true`, pricing catalog
      `advancedUnlocked:true`.
- [x] Package + advice checkouts return real authority/gateway URLs.

### Data seeded (dev DB)
- Prices: BRAND-VAL 150k+advice 300k; STARTUP-VAL/TRADEMARK-VAL/PATENT-VAL/PHARMA-IP 120k;
  INTANGIBLE/KNOWHOW 100k; routes filled for all 13 tools.
- Packages: «بسته استارتاپی» (STARTUP-VAL+IDEA-ASSESS+INNOV-READ, 250k, perpetual) and
  «بسته مالکیت فکری» (pick-3, 60 days, 300k).
- NOTE: `ToolFormsSeeder` only inserts missing tool codes — price updates live in the DB;
  to change prices use SQL against `ToolForms` (or extend the seeder).

---

## Project layout (two repos)

| Repo | Path | Stack |
|------|------|-------|
| Backend | `C:\Users\amin\Desktop\valuation-suite-back\src` | .NET Clean Architecture (Domain, Application, Persistence, RequestHandlers, ApiFramework, Web.Api), EF Core + SQL Server, MediatR, FluentValidation |
| Frontend | `C:\Users\amin\Desktop\valuation-suite` | Next.js 14 (app router), React 18, Tailwind, TypeScript, RTL Persian UI |

Note: the shell project root here is the **backend** (`src/`). Frontend files are read/written
via absolute paths `C:/Users/amin/Desktop/valuation-suite/...`.

## Session 5 archive (✅ COMPLETE — roles visible, company at register, freemium pricing, adviser notes, visual admin reporting)

**User directives (session 5):**
1. Every user (admin or not) must be identifiable in the admin area → roles now in auth responses + `/me` + customers table.
2. Users must register their company too → optional `companyName` at register creates a Company.
3. Admin reporting must be VISUAL (charts), not raw JSON → new analytics endpoint + chart components.
4. ALL tools free for basics; advanced results (valuable info) need payment OR packages; user can pick any tools/how many (pick-N credits).
5. Adviser role (new admin type) writes text/voice notes on results; customers buy advice per submission.
6. On login client identifies role and loads correct pages.
7. Zarinpal gateway activated for all payment kinds (server-computed amounts).
8. Step UX: clicking an option auto-advances; misc UI/UX improvements.

### Session 5 progress — BACKEND COMPLETE (builds + migration applied)

- [x] **Domain**: `UserToolAccess` (per-tool grants, expiry optional), `AccessPackage`
      (pick-N + curated bundles, duration, price), `SubmissionNote` (text + base64 voice),
      `PaymentKind` (ProPlan/ToolAdvanced/Package/Advice) + Payment fields
      (Kind/ToolCode/PackageId/SubmissionId), ToolForm fields (AdvancedPriceToman, Route,
      AdvicePriceToman), Adviser role in AdminRoles + notes.write/access.manage perms.
- [x] **EF**: configurations for the 3 new tables; migration `AccessAndNotes` generated +
      **applied to DB** (27 tables total).
- [x] **Application**: `IEntitlementService`+impl (IsPro, CanViewAdvanced, HasPaidAdvice,
      Grant, pick-credit grant/consume), pricing catalog query (tools+packages+myAccesses).
- [x] **Handlers**: `RunToolCommandHandler` now strips advanced sections (mc/tornado/
      scenarios/vals/allValues/methods keys) from results when not entitled → **basic is
      free, advanced is paid**; pricing catalog handler; admin handlers for analytics,
      grants (grant/revoke), packages upsert, adviser notes (add/list/queue), customer
      note delivery with seen-marking.
- [x] **Controllers**: AuthController returns roles + company info (register accepts
      companyName → creates Company); `/api/payments/checkout` typed endpoint (pro/tool/
      package/advice — amounts always server-computed); callback grants by kind (pro
      extends expiry, tool → perpetual grant, package → grants + pick credits, advice →
      paid payment row); AdminController +7 routes: analytics, grants CRUD, packages,
      adviser-queue, submissions/{id}/notes.
- [x] EntitlementService registered in Application DI.

### Session 5 remaining — FRONTEND ✅ COMPLETE + E2E VERIFIED

- [x] lib/api: roles/company in auth store (`AuthPayload.user.roles`, `hasRole`,
      `isAdminUser`); auth.ts returns roles+company from register/login/me; admin.ts got
      analytics/grants/notes clients + `hasNotes`/`unreadNotes` on submissions; tools.ts
      carries route/prices + `advancedIncluded` on runs; new `pricing.ts` client.
- [x] **Role-based redirect after login** — AuthContext `homeFor()`: Admin/Support/Analyst
      → `/admin`, Adviser → `/adviser`, others stay. Login → correct area automatically.
- [x] **/pricing page** — Pro banner (990000 تومان/ماه), packages grid, **pick-N custom
      bundle builder** (select exactly N tools), per-tool advanced purchase table with
      free/unlocked/price badges, "my active accesses" chips, all via Zarinpal checkout.
- [x] **AdvancedLock component** — shown on calculator pages when the server stripped
      advanced sections (detected via missing `mc`/`tornado` in result); wired into
      brand-valuation as the pattern; CTA → /pricing.
- [x] **/adviser area** (`/adviser` + AdviserLayout role guard) — queue of paid reviews
      with input/result viewers, **NoteComposer with MediaRecorder voice recording**
      (base64 webm, ≤2min) + text, submit → customer sees it. NotesPanel component for
      customers (audio player via blob URL, buy-advice CTA) wired into brand-valuation
      final tab; customer endpoint `/api/tools/my-submissions/{id}/notes` marks seen.
- [x] **Admin visual reporting** — `/admin/analytics` page: submissions/day bar,
      revenue/day bar (compact money), tool usage bar, score-bucket histogram, plan
      donut, payment-status donut — zero raw JSON. New sidebar entry "گزارش تصویری".
      **Roles badge column** added to admin customers table (Admin=purple chip,
      others light; "مشتری" when none).
- [x] **Step auto-advance UX** — idea-assessment: clicking an option scrolls the next
      unanswered question into view (250ms delay), final question glides to the CTA.
- [x] Register modal now has optional **company name field**.
- [x] `npx tsc --noEmit` → 0 errors.

### Session 5 E2E verification (API smoke, all green)

- Register with company → Company row created + linked (fixed: UserConfiguration
  previously `Ignore(u => u.Company)` — now a real navigation; controller saves the
  company explicitly through `ICommandRepository<Company>` before linking).
- Login returns `roles` array; bootstrapped admin2 → `["Admin"]` + all perm claims.
- `GET /api/tools/pricing` → tools with prices + packages + myAccesses.
- **Freemium verified**: free run of BRAND-VAL → `advancedIncluded:false`, `mc`/`tornado`
  keys ABSENT from result; after admin grant → `advancedIncluded:true`, keys present.
  (Root cause fix: runners return typed objects, not JsonElement — Strip now serializes
  via `ToolInput.WriteOptions` before filtering.)
- Advice checkout validates: free-advanced tool rejected, advice on tool with
  `advicePriceToman=0` rejected, unknown package → 404, unauth → 401.
- Adviser note created via `/api/admin/submissions/{id}/notes` → customer sees it via
  `/api/tools/my-submissions/{id}/notes` (seenByCustomer:true after fetch).
- Grants: POST 204 → list shows row → DELETE 204 → gone.
- Analytics: submissionsPerDay/toolUsage/planDistribution populated with live data.
- Note: BRAND-VAL synthetic payloads with all-zero forecast rows can produce Infinity
  in the MC engine (pre-existing engine behavior, unrelated to session 5); the real
  frontend initialState works.

### Session 5 deployment notes

- `AdvancedPriceToman`/`AdvicePriceToman`/`Route` are all **0/empty by default** — set
  real prices via a DB update or extend `ToolFormsSeeder` (seeder only fills new tools;
  use SQL `UPDATE ToolForms SET AdvancedPriceToman=… WHERE ToolCode='…'`).
- Seed 1–2 `AccessPackage` rows for the pricing page's package grid (none exist yet).
- `AdminBootstrap:Emails` now contains `admin@yourdomain.com` + `admin2@test.local` (dev).
- Zarinpal `MerchantId` is still the sandbox placeholder — checkout returns a clear
  Persian error until a real merchant ID is configured.

---

## Session 4 (archived) — smoke tests + admin enrichment + UI/UX refactor

**User directive:** continue from session 3; smoke-test server+client+all APIs; enrich the
admin area (more features), refactor UI, improve UX. No questions — just implement.

### Session 4 progress

- [x] **EF model fixed**: `VisibilityCondition`/`ValidationRule`/`CalculationConfig`/
      `MathExpression`(+nested `MathVariable`/`MathOperation`) were misconfigured as
      top-level entities with nonsense PKs (e.g. PK=TargetQuestionId → seeding crash).
      Re-mapped as **owned types** (inline in QuestionConfiguration — owned types cannot use
      `IEntityTypeConfiguration`); `ScoredAnswer` props configured in AssessmentResultConfiguration.
- [x] **Migration regenerated** (`InitialCreate`, 24 tables, **0 shadow FKs** — the earlier
      `AssessmentId1`-style duplicates came from unclaimed navs + duplicated parent/child
      relationship configs, all fixed in the 6 affected configurations).
- [x] **DB recreated**: connection-string port fixed (1572→5012, matches docker `sqlserver`
      container) in both appsettings.json and appsettings.Development.json;
      `database drop --force` + `database update` succeeded.
- [x] **Startup smoke test**: roles Admin/Support/Analyst created; IP assessment seeded
      (7 steps / 47 questions); 13 tool forms seeded; register/login work.
- [x] **Smoke bug fixed**: dashboard `GroupBy` with nested `Distinct().Count()` is not
      translatable by EF — `GetTopToolsAsync` rewritten (two flat projections + in-memory join).
- [x] **Admin enrichment (backend)**:
      - `GET /api/admin/submissions` — paged global submissions explorer (filter: toolCode,
        search in name/toolCode/user-email, userId)
      - `GET /api/admin/tools` — tool catalog with usage stats (uses + distinct users)
      - `GET /api/admin/companies` — paged company list (members, pro-members, paid totals)
      - `PUT /api/admin/customers/{id}/password` — admin password reset (Identity reset token)
      - `GET /api/admin/payments/export` — CSV export of the payments ledger
      - `AdminSubmissionRow` now carries `userId` + `userEmail`; company row uses `Slug`
- [x] **Backend builds clean**

### Session 4 remaining

- [x] Frontend: shared admin UI kit `app/components/admin/ui.tsx` (PageHeader, StatCard,
      Badge/PlanBadge/ActiveBadge/PaymentStatusBadge, DataTable with row-click + hover,
      Pagination, debounced SearchInput, Select, Modal with Esc-close, Loading,
      EmptyState, ErrorNote, money/dateTime fa-IR helpers)
- [x] Frontend: layout sidebar upgraded — icons, active indicator bar, new nav items
- [x] Frontend: **new pages** — `/admin/submissions` (global explorer: search + tool
      filter + side-by-side input/result JSON viewer), `/admin/tools` (catalog + usage
      KPIs), `/admin/companies` (members/pro/revenue aggregates)
- [x] Frontend: dashboard + customers + detail + payments pages refactored onto the kit;
      payments got the **CSV export** button (authenticated blob download); detail page
      got the **password reset** modal
- [x] Frontend typecheck passes (0 errors)
- [x] **E2E API round (all 12 admin endpoints) — green**: dashboard, customers list,
      customer detail, update customer, payments (handler was MISSING — added
      `GetAdminPaymentsQueryHandler`), global submissions, tools, companies, password
      reset (needed `.AddDefaultTokenProviders()` in Identity setup — fixed), CSV
      export, role assignment (200), unauth → 401
- [x] Verified end-to-end: run IDEA-ASSESS via API → submission appears in admin
      explorer with userEmail; password reset → login with new password works

**Session 4 COMPLETE.**

### Remaining (for a future session)

- [ ] Browser-level smoke of the Next.js frontend (`npm run dev`) against the running API
- [ ] IP assessment frontend (`IpAssessment.tsx`) still scores locally — should call
      `runTool`/attempt APIs like the other question tools
- [ ] Zarinpal production merchant + webhook signature validation
- [ ] patent-search → backend proxy (deferred per user)
- [ ] Refresh tokens, rate limiting

---

## Session 3 (admin area) — COMPLETE ✅

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
