# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

EssayCheck AI — a .NET 8 backend API for a mobile app (Azerbaijani market) that evaluates English
essays against official DİM (State Examination Center of Azerbaijan) writing criteria using an AI
model via OpenRouter. Users register, submit essay text (or a photo of handwritten text via OCR
for Pro Plus), get scored/corrected feedback, keep a history, and subscribe to Free/Pro/Pro Plus
plans (manual or Google Play Billing).

No frontend code lives in this repo — it's API-only, consumed by a separate mobile app.

## Commands

```bash
dotnet build EssayCheck.sln              # build everything
dotnet run --project src/EssayChecker.Api   # run locally (Swagger UI enabled in all environments)
dotnet ef migrations add <Name> --project src/EssayChecker.Persistence --startup-project src/EssayChecker.Api
dotnet ef database update --project src/EssayChecker.Persistence --startup-project src/EssayChecker.Api
docker build -t essaycheck-api:latest .
docker compose -f docker-compose.dev.yml up   # local Postgres for dev
```

There are no test projects in the solution (`EssayCheck.sln` has 5 projects, no test project).

Local secrets (JWT key, DB connection string, email, OpenRouter API key) are kept in
`dotnet user-secrets` for `EssayChecker.Api`, never committed. See `DEPLOYMENT.md` for the full
list of required environment variables in production (`Section__Key` double-underscore format).

## Architecture

Clean Architecture, 5 projects under `src/`, dependency direction strictly inward:

- **EssayChecker.Domain** — entities only (`Entities/Essays`, `Entities/Subscriptions`,
  `Entities/Users`, `Entities/Logs`, `Entities/Marketing`), enums, no dependencies on other layers.
- **EssayChecker.Application** — DTOs (`DTOs/Auth`, `DTOs/Essays`, `DTOs/Subscriptions`, `DTOs/Account`,
  `DTOs/App`), service interfaces (`DTOs/Interfaces`), strongly-typed settings (`Settings/`, bound
  via `AddOptions<T>().ValidateOnStart()` in `Program.cs`), and small shared types like `AuthResult`
  (`Common/AuthResult.cs` — the `{ succeeded, message, errors }` shape used across all auth/account
  responses).
- **EssayChecker.Infrastructure** — all service implementations, wired up in one place:
  `Infrastructure/DependencyInjection.cs` (`AddInfrastructure()`). Key areas:
  - `Ai/` — `OpenRouterClient`, `EssayPrompts` (the two DİM system prompts), `EssaySchemas`
    (OpenAI strict `json_schema` structured-output schemas), `OpenRouterModels`.
  - `Services/Essays/` — `OpenRouterEssayEvaluator` (grading), `OpenRouterOcrService` (image→text,
    Pro Plus only), `EssayService` (orchestration + history), `AiEssayResponseParser` (parses/repairs
    the AI's JSON), `CorrectedEssayBuilder` (builds the marked-up essay deterministically),
    deterministic post-processing rules that used to live in the prompt (`IntroductoryCommaRule`,
    `SentenceInitialBecauseRule`, `SentenceBoundaries`) — these were deliberately moved from
    AI-decided to code-decided for consistency (see recent commit history).
  - `Services/Subscriptions/` — `SubscriptionService`, `UsageLimitService` (daily free-plan quota),
    `ReferralRewardService`.
  - `Services/Users/` — `AuthService`, `AccountService`, `JwtService`, email senders (`EmailService`
    for SMTP, `BrevoEmailService` for HTTPS API — selected at runtime based on whether
    `Email:BrevoApiKey` is set; Render blocks outbound SMTP so production uses Brevo).
  - `GooglePlay/` — `GooglePlayPurchaseVerifier`, server-side Google Play Billing purchase/RTDN
    webhook verification.
  - Hosted background services: `RefreshTokenCleanupService`, `AccountPurgeService` (finishes soft
    deletes), `RequestLogCleanupService`.
- **EssayChecker.Persistence** — EF Core: `Context/EssayDbContext`, `Configurations/` (Fluent API,
  including the important convention that every `DateTime` is explicitly mapped to
  `timestamp with time zone` for Npgsql/UTC correctness — new `DateTime` properties inherit this
  automatically), `Migrations/`, `Repositories/`, ASP.NET Identity setup (`Identity/`).
- **EssayChecker.Api** — `Program.cs` (composition root — see below), `Controllers/` (thin, one per
  resource: Auth, Account, Essay, Subscription, App, Legal, ResetPasswordPage),
  `Logging/RequestResponseLoggingMiddleware`, `GlobalExceptionHandler`.

Database: **PostgreSQL** (Npgsql), migrated from SQL Server in July 2026 — see `DEPLOYMENT.md` §1/§3
for the historical context and the UTC/`timestamp with time zone` gotcha.

### Request flow / Program.cs composition root

`Program.cs` wires, in order: exception handler + ProblemDetails → controllers with
`JsonStringEnumConverter` (all enums serialize as strings, e.g. `"plan": "ProPlus"`) → custom
`InvalidModelStateResponseFactory` so DataAnnotations validation failures return the same
`AuthResult`-shaped `{ message, errors }` body as the rest of the API, not ASP.NET's default
`ValidationProblemDetails` → Swagger (enabled everywhere **except** `Production` — closed 2026-08-25;
it used to be open everywhere on purpose, which meant the whole API schema and every endpoint were
world-readable) → strongly-typed settings with fail-fast `ValidateOnStart()` (except `GooglePlaySettings`, which is
intentionally allowed to stay unconfigured so the rest of the API isn't blocked before Play Console
setup is complete) → `AddPersistence` / `AddInfrastructure` → JWT auth → CORS (`Cors:AllowedOrigins`
empty ⇒ allow any origin, since mobile clients aren't subject to CORS) → `ForwardedHeaders`
middleware (trusts all proxies — needed behind PaaS load balancers) → auto `MigrateAsync()` on
startup (no manual `dotnet ef database update` step in deploy — there's no CI/CD pipeline yet, so
this exists specifically to prevent forgetting it).

### Essay evaluation flow (the core feature)

`POST /api/essay/evaluate` (`EssayController` → `EssayService`):
1. Check daily free-plan quota (`UsageLimitService`) — if exhausted, return `429` **without** calling
   the AI.
2. `OpenRouterEssayEvaluator` makes **two sequential AI calls**, not one:
   - **Call A — detection** (`EssayPrompts.DetectionRules` + `EssaySchemas.Detection`): finds the
     language errors and decides `isEssay`. Nothing else.
   - **Call B — scoring** (`EssayPrompts.ScoringRules` + `EssaySchemas.Scoring`): applies the DİM
     rubric and writes the Azerbaijani feedback. Call A's finished mistake list is passed *into* it
     via `GetScoringInput` so the grammar/vocabulary scores reflect real error density and the model
     never searches twice.
   They are sequential (B depends on A), and each call falls back to `FallbackModel` independently.
3. If call A returns `isEssay: false`, return `422` — nothing is persisted, quota isn't consumed.
4. `EssayEvaluationMapper.BuildMistakes` validates the AI's items against the essay itself, then the
   deterministic rules (`IntroductoryCommaRule`, `SentenceInitialBecauseRule`) add their own. The
   result is persisted to `Essays` (with `EssayScores`, `EssayStatistics`, `EssayMistake[]`,
   `TeacherFeedback`), and only then is the daily counter incremented.

**Mistake validation is deliberate and load-bearing** (`EssayEvaluationMapper.AddAiMistakes`). An AI
item is dropped unless its `wrong` is an exact substring of the essay occurring **exactly once**.
That kills hallucinated corrections and makes each item's position unambiguous. The prompt tells the
model to widen a `wrong` span with neighbouring words until it is unique — if you loosen this filter,
the B1 repetition rule ("leave the first occurrence, replace later ones") silently breaks.

**`correctedEssay` is built in code, never by the AI** (`CorrectedEssayBuilder`). Each mistake carries
an explicit `MistakeSpan` (start + length) — AI items get theirs from the uniqueness check, rule-based
items from the rule's own match position. The output is stitched from the *original* essay, so a
silent unmarked edit by the model is structurally impossible, and a phrase that is already correct
(e.g. a `However,` that has its comma) is never marked just because an identical-looking one elsewhere
was wrong. Don't reintroduce search-based marking.

**Model caveat:** the evaluation model is `openai/gpt-5.6-luna` (`OpenRouter:Model`). It does **not**
support `temperature`, so the `Temperature: 0` setting is silently dropped for it and output varies
between runs on the same essay — the same essay can score 2.4 or 2.7 and produce 18 or 21 mistakes.
`OpenRouterSettings.Temperature`'s "same essay always gets the same result" comment no longer holds
here. OCR still uses `gpt-4o-mini`.

`seed` **is** wired up now (`OpenRouterSettings.Seed` → `ChatCompletionRequest.seed`, omitted entirely
when null) but is deliberately left unset, because it was measured on 2026-08-20 and **does nothing on
this model**: nine runs of the same three essays with the same seed produced nine different results,
and a minimal probe with an identical seed returned three different completions. The request is
accepted without error and `system_fingerprint` comes back null, so there is no way to detect this
from the response — don't assume enabling it fixes run-to-run variance. The plumbing is kept so that
switching to a model that honours it is a config-only change.

**Measured accuracy (2026-08-20, 10 hand-written ~100-word Grade11 essays, 543 planted errors across
3 runs):** recall ~94.5%, zero invented errors, precision ~98%. Run-to-run variance is large — the
same essay swings ±3 mistakes and ±0.5 points — so a single A/B run cannot resolve a ~1% prompt change;
average several runs per variant before believing any prompt result. Score accuracy against real
teacher marks has never been measured (no ground-truth set exists yet).

Both `DetectionRules` and `ScoringRules` are deliberately byte-for-byte identical across all requests
(grade/topic/word-count/detected-errors are injected in a separate later message) so Anthropic's
prompt caching applies and cuts the cost of that portion by ~90%. When editing a prompt, keep
essay-specific values out of the constant.

`POST /api/essay/ocr` (Pro Plus only, `403` for Free/Pro) reads an image and returns plain text —
it does **not** persist or evaluate; the client is expected to review/edit the OCR text, then call
`/evaluate` separately with `source: "Image"`.

### Teacher mode: groups and students

A user can keep a roster: `StudentGroup` (owned by `TeacherId`) → `Student` (exactly one group).
Students are **not app users** — no login, no email, no quota of their own. The teacher submits every
essay and the teacher's quota is charged. There is deliberately **no teacher role and no plan gate**:
anyone can create groups; the paid plan only raises the daily essay limit. Abuse is bounded by
`TeachingService`'s caps (50 groups/teacher, 200 students/group).

`Essay.StudentId` is nullable — selecting a student is optional, and essays predating this feature
have none. On evaluate, the controller resolves the grade as `request.Grade ?? student.Grade`, so a
student card with a grade lets the client omit it; if both are missing the request is rejected with
400. `EvaluateEssayRequest.Grade` is therefore nullable now, but existing clients that always send it
are unaffected.

Ownership is checked on every path by walking `Student → Group → TeacherId`; a foreign or missing id
returns "not found" rather than "forbidden" so existence doesn't leak. Deletes are **soft** for both
groups and students (deleting a group soft-deletes its students), and essays are never touched — the
`StudentId` link survives so past results and future progress analytics stay intact. History is one
list carrying `studentId`/`studentName`, filterable by `studentId` or `groupId`.

### Analytics (student progress / weaknesses)

`GET /api/analytics/overview` (whole account), `/analytics/groups/{id}`, `/analytics/students/{id}`.
Everything is derived from rows that already exist — **no extra AI call**, so these endpoints don't
touch the daily quota and cost nothing.

- Capped at the **most recent 500 essays** per query (`AnalyticsRepository.MaxRows`) — it loads rows
  into memory to aggregate, so an uncapped query would scale with a teacher's whole history. Rows are
  fetched newest-first then reversed, so the *oldest* essays are what falls off the edge and
  `AnalyticsAggregator` still receives them chronologically.
- `AnalyticsRepository` only fetches flat rows (`EssayAnalyticsRow`) — scores and mistake counts are
  plain columns (`Scores`/`Statistics` are `OwnsOne`, not JSON), so this is a cheap projection that
  deliberately excludes `OriginalText`/`CorrectedEssay`.
- All the arithmetic lives in `AnalyticsAggregator` (pure functions, no DB) so the same essay can't
  produce different numbers on the student, group and overview screens.
- **The four directions have different maxima** (`content` is 2.0, the rest 1.0). Never rank them by
  raw score — `DirectionStat.Percent` (score ÷ max) is what "weakest direction" and every chart must
  use.
- "Weaknesses" / "recommendations" are **not regenerated**; they are the `TeacherFeedback` lists the
  grading call already wrote, taken from the last 10 essays and grouped by normalised text
  (whitespace + case + trailing punctuation). Grouping is textual only — the AI wording varies, so
  treat `count` as a hint, not a measurement.
- `MistakeSummary.PerHundredWords` is the length-independent metric; raw totals favour short essays.
- `HasEnoughData` = at least 2 essays. Data is still returned below that (zeros/nulls), the flag only
  tells the client not to draw a trend line.
- Group analytics excludes soft-deleted students so the numbers match the visible roster — note this
  differs from `/essay/history?groupId=`, which deliberately still finds their essays.

### Lessons (topic explanations) — a SHARED library, not per-user content

`POST /api/lessons` builds a 6-8 slide English lesson (Azerbaijani explanations, English examples)
plus a 3-question quiz. `GET /api/lessons` lists it, `GET /api/lessons/{id}` reads it — both free of
AI and quota, and both open to **every** authenticated user, not just the one who created it. There
is no delete endpoint. Originally built to the frontend order in `BACKEND_LESSON_FEATURE.md`, then
reshaped into a shared library per an explicit later product decision (2026-08-23): a teacher who
generates a lesson makes it visible to every other teacher, so nobody burns tokens regenerating a
topic someone else already paid for.

- `Lesson` has **no owner in the access-control sense**. `CreatedByUserId` is metadata only (who to
  credit, `IsMine` in the response) — it is `OnDelete(Restrict)`, not `Cascade`: a shared lesson must
  outlive the account that made it, so deleting that user is blocked while their lessons still exist
  (there is currently no admin path to reassign/orphan them if that ever needs to happen).
- **`(NormalizedTopic, Grade)` is globally unique** — one lesson per topic+grade, period. There is no
  concept of "my copy" vs "their copy"; `LessonTemplate` (the old per-topic cache table) is gone
  because the `Lesson` row now serves both roles at once.
- **Quota logic:** `LessonService.CreateAsync` looks the topic up first. Found → return it, no AI
  call, no quota touched (this is the whole point — token cost is what's being avoided). Not found →
  check the daily counter, call the AI, and only charge the counter *after* the row is actually
  inserted. `PlanPolicy.LessonDailyLimit` is tiered by plan (1/1/2/4, see "Subscriptions" below), but
  the limit is never about rationing lesson *access* — that stays unlimited for everyone via the
  library — only about how many brand-new topics a plan may generate per day.
- **Race handled, not ignored:** if two users submit the same new topic at once, the DB's unique
  index will reject the loser's insert. `LessonService` catches that specific Postgres error
  (matched by index name, `IsDuplicateTopic`), re-fetches the winner's row, and returns it as a
  normal "already in library" result — the loser's quota is *not* charged, so a race never costs a
  user their one daily generation for nothing.
- `request.Grade` is **required** now (`[Required]` on `CreateLessonRequest`) — there is no student
  card to infer it from anymore, since lessons dropped `StudentId` entirely (see below).
- `LessonContentMapper` deliberately does *not* repair slide/quiz counts (the product decision was
  "return what the AI gave"). It only guarantees every field is present (null/empty array), drops
  a quiz question whose `correctIndex` falls outside its own `options` (such a question would mark
  the right answer wrong), and **rotates the options so the correct answer lands on a different
  position in each question** — `gpt-4o-mini` put every correct answer at index 0 in every measured
  lesson despite the prompt forbidding it. The rotation is a cyclic shift driven by a stable FNV-1a
  hash of the question text plus the question's position, so it is deterministic (never `Random`,
  never `string.GetHashCode`, which is per-process randomised) and a cached template can never
  disagree with the lessons copied from it. Options containing "above"/"yuxarıdakı" are left alone.
- Uses its own model (`OpenRouter:LessonModel`), separate from the essay model — switching it is a
  config-only change. **Measured three candidates 2026-08-23** on the real prompt (cost via
  OpenRouter's `usage.cost`, quality via Intro/Rule body length against the ~900-1400 char target):
  `gpt-4o-mini` ($0.0011/lesson, ~12-17s, bodies plateaued at ~575-660 chars regardless of how hard
  the prompt pushed for more); `gpt-5.6-luna` (the essay model — $0.0032/lesson, noticeably better
  reasoning and exam-relevant content, but pushed `Pro`/`ProPlus`/`Premium` worst-case cost slightly
  *above* their break-even floor at current prices); `google/gemini-2.5-flash-lite` ($0.0016/lesson,
  ~13s — the fastest of six models tried including `deepseek-v3.2` and `llama-3.3-70b-instruct` — and
  the only one landing inside the target length on the first try, 963-1011 chars). Settled on
  `gemini-2.5-flash-lite`: cheaper and faster than `gpt-5.6-luna` with equal or better length, and it
  restores `Pro`/`ProPlus` to (just barely) profitable at current prices — `Premium` is still ~$1/mo
  under its worst-case floor, see "Subscriptions" below.
- Slides and quiz are nested JSON columns (`OwnsMany(...).ToJson()` with owned collections inside);
  verified to round-trip losslessly including `comparison` and `examples`.
- **Prompt depth, measured 2026-08-22 → fixed 2026-08-23:** the first version of the prompt produced
  bodies of only ~100-150 characters per slide — one throwaway sentence, no real teaching content.
  `LessonPrompts.Rules` now requires a strict two-paragraph structure per Intro/Rule body (paragraph
  1: why the rule exists and how it differs from Azerbaijani instinct; paragraph 2: concrete usage
  situations, an exception, and the exam/essay angle) — plain sentence-count targets ("write 9-14
  sentences") measurably under-performed this on `gpt-4o-mini`, the two-paragraph framing did better.
  `LessonPrompts.Version` has been bumped several times since (**always bump it when `Rules`
  changes**, most recently 4 → 5 for the paragraph-structure edit): a lesson already in the library is
  never regenerated just because the prompt got better or the topic-validation logic was loosened
  (see below) — the version field is informational only (no auto-invalidation), so an old version
  sitting in the library is a signal to regenerate by hand, not something the system fixes on its own.
- **Topic validation was too conservative, fixed 2026-08-23:** Azerbaijani-phrased topics
  ("zamanlar", "feillər", and even "İngilis dilində məktub yazmaq" — literally the prompt's own
  example of a valid topic) were being rejected by `gpt-4o-mini` as not English-related, despite the
  prompt already saying Azerbaijani phrasing was fine. Since this app only ever teaches English, the
  prompt now says so explicitly and defaults to accepting rather than leaving the judgment call to
  the model, with a literal list of accepted Azerbaijani phrasings. Verified against the real model:
  previously-rejected topics now return `isEnglishTopic: true`.
- The `ReshapeLessonsIntoSharedLibrary` migration was **hand-edited** after scaffolding: EF's
  auto-generated version renamed the `UserId` column straight to `PromptVersion` and added
  `CreatedByUserId` with `defaultValue: 0` — which would have silently discarded who created each
  existing lesson (and then failed outright, since the new FK requires a real `AspNetUsers.Id` and
  `0` isn't one). Fixed by adding `CreatedByUserId` first, copying `UserId`'s value into it with raw
  SQL, and only then dropping the old columns. If you ever re-scaffold a migration that renames a
  column EF also wants to repurpose, read the generated SQL before trusting it — the "may result in
  data loss" warning during `migrations add` is not decorative.

### Error response conventions (see POSTMAN_DOCS.md §1.6 for full detail)

- Validation/logic errors: `{ "message": "..." }`, usually `400`.
- Auth/account mutation endpoints (register/login/forgot-reset password/account ops): `AuthResult`
  shape `{ "succeeded": bool, "message": string, "errors": string[] }`.
- Unhandled server errors: caught by `GlobalExceptionHandler` → `{ "message": "Gözlənilməz xəta baş verdi." }`.
- AI service unavailable: `503` (temporary) or `502` (unreachable).

### Owner panel (`/admin`, server-rendered)

Read-only reporting for the app owner: overview (registrations, subscriptions, essays, content,
server health), users (full list with plan + per-user essay/lesson/group counts, filterable and
searchable), activity (who checked essays in a period and how many times).

- **Was a JSON API under `/api/admin/*` first, replaced same-day.** Returning raw JSON for a
  human to read (worse: with the shared secret sitting in the URL) was the wrong shape for "let
  me look at some numbers" — it's now Razor Pages under `/admin`, cookie-authenticated, rendered
  as actual HTML tables. The JSON controller is gone; `IAdminReportRepository` (Persistence) and
  the DTOs in `Application/DTOs/Admin` are unchanged and now feed `Pages/Admin/*.cshtml.cs`.
- **Auth is a second, separate scheme from the mobile JWT.** `AddCookie(AdminAuth.Scheme, ...)`
  alongside the existing JWT bearer scheme; `AdminAuth.Policy` requires that scheme specifically,
  so a mobile user's JWT cannot open the panel and the panel's cookie cannot call the JSON API.
  Login (`Pages/Admin/Login.cshtml`) checks the submitted key against `Admin:ApiKey` with a
  constant-time comparison and calls `SignInAsync` — no separate admin user table, just one shared
  secret gating one cookie-carrying session (8h sliding expiration).
- **`Admin:ApiKey` unset ⇒ the whole panel 404s**, both the login page and every `/admin/*` page —
  same fail-closed shape as `GooglePlaySettings`/`Testing`. Production value goes in Render as
  `Admin__ApiKey`; never in `appsettings.json`.
- `RequestResponseLoggingMiddleware` **skips `/admin` entirely** (same early-return it already had
  for `/swagger`) — HTML pages are large and log-worthless, and this also means the login POST
  body (which carries the key in plaintext, unlike the old `?secret=` query approach) never
  touches `RequestLogs` in the first place.
- Periods (`today`, `yesterday`, `last7days`, `last30days`, `all`) are resolved in **Azerbaijan
  time (fixed UTC+4)**, not UTC — "today" has to mean the owner's calendar day.
  `AdminPeriodRange` hardcodes the offset rather than using `TimeZoneInfo` on purpose: the lookup
  fails on Linux containers without tzdata, and Azerbaijan has had no DST since 2016.
- Plans shown are the **raw DB state** and deliberately ignore `Testing:ForceProPlusForAllUsers` —
  the owner needs the real subscription picture, not effective entitlements. A user's `isPaying`
  tag is true only for `SubscriptionPlatform.GooglePlay`, the sole revenue-bearing source;
  `Trial` and `Manual` show as a plain plan tag, not "paying".
- The user list runs one subquery per row for the counts. Fine at hundreds of users; convert to a
  JOIN + GroupBy before this reaches thousands.

### Auth model

JWT access token + rotating refresh token (hashed with SHA-256 in DB). Refresh endpoint revokes the
used token immediately and issues a new pair — reusing an old refresh token is rejected (standard
theft-detection pattern). Password reset and password change both revoke **all** refresh tokens for
the user. Account deletion is soft (`IsDeleted=true`); essay history is preserved, only login is
blocked. `AccountPurgeService` then hard-deletes the row 30 days later (the promise made on
`/legal/delete-account`).

**That purge is a single bulk `ExecuteDeleteAsync`, so one bad FK breaks it for everyone.** It did
exactly that until 2026-08-24: `Lessons.CreatedByUserId` was `Restrict` (to keep a shared lesson
alive after its creator leaves), which made the whole batch throw, and `AccountPurgeService` caught
and logged the error — so *no account was ever purged*, silently. It's now nullable with `SetNull`:
the lesson survives with `createdByName: "Silinmiş istifadəçi"` (`LessonCreator.DisplayName`).
Before adding any new FK to `AspNetUsers`, decide Cascade or SetNull — **never Restrict**.

`DeviceTrials` deliberately has no FK to `AspNetUsers` at all, so purging a user does not release
their device's one free trial.

Auth endpoints are rate-limited per IP (`AuthRateLimiting`): registration 5/hour, login 10/15min,
forgot-password 3/hour. Identity's own lockout only protects a *known* account from password
guessing — it does nothing about mass registration, email enumeration, or using forgot-password as a
mail bomb. The limiter must stay **after** `UseForwardedHeaders()` in `Program.cs`, otherwise every
request partitions on the proxy's IP and the limit applies to all users collectively.

### Subscriptions

Four plans, all rules centralised in `PlanPolicy` (`SubscriptionPlan` enum: `Free`, `Pro`, `ProPlus`,
`Premium`). OCR and text checks share one counter — there is no OCR-specific plan gate anywhere in
the code, only the shared daily total.

**Only `/evaluate` consumes that counter; `/ocr` checks it but does not increment.** A photo essay is
one check to the user, not two. When both consumed (before 2026-08-24) a Free user (1/day) could
*never* finish the photo flow: OCR ate the single slot and the follow-up `/evaluate` returned 429.
`/ocr` still verifies remaining quota so an exhausted user can't use it as a free OCR service.

| Plan | Essays/day (`DailyLimit`) | Lessons/day (`LessonDailyLimit`) |
|---|---|---|
| Free | 1 | 1 |
| Pro | 10 | 1 |
| ProPlus | 20 | 2 |
| Premium | 40 | 4 |

**None of these are `null`/truly unlimited anymore** — that changed 2026-08-23. `ProPlus` used to be
literal unlimited essays; it is now capped at 20/day, a real behaviour change for any already-paying
ProPlus subscriber. `Premium`'s 40/day is deliberately marketed to users as "limitsiz esse yoxlama"
while the backend enforces a real fair-use number.

Lesson limits are tiered too now (used to be flat 1/day for every plan) — see `PlanPolicy` for why
this still doesn't touch the shared-library reasoning: the counter limits new *generation*, not
reading.

**Cost floor, re-measured 2026-08-23 after the lesson model switch to `gemini-2.5-flash-lite`**
(essay $0.0083 × 2 calls unchanged, lesson now $0.0016 × 1 call). Worst-case monthly AI cost assumes
every daily limit is hit every day for 30 days — real average usage is far below this:

| Plan | Worst-case AI cost/mo | Break-even price (15% Google cut) | Current price | Margin |
|---|---|---|---|---|
| Pro | $2.54 | $2.99 | $2.99 | ~breakeven, no buffer |
| ProPlus | $5.08 | $5.97 | $5.99 | ~breakeven, no buffer |
| Premium | $10.15 | $11.94 | $10.99 | **below floor by ~$1/mo** |

`Premium` is still priced under its theoretical worst-case floor — acceptable only because real usage
never sustains the daily cap every day; treat this as a known, accepted tail risk, not a bug. If you
ever raise `DailyLimit`/`LessonDailyLimit` or change either model, **re-run this measurement** (real
`usage.cost` from OpenRouter, not an estimate) before assuming the current prices still hold — model
pricing and prompt length both drift, and this table goes stale silently.

`PlanCatalog`'s `Price`/`Currency` fields are **display-only placeholders**, not the real Google Play
charge — the actual amount is configured per-region directly in Play Console and this backend has no
way to read it back. `GooglePlaySettings:Products` (a `productId → SubscriptionPlan` dictionary, e.g.
`"pro_monthly": "Pro"`) is what actually matters for real purchases — an unmapped Play Console product
ID fails purchase verification with "Naməlum Google Play məhsulu", so **any new Play Console product
must be added here or its purchases can never be verified**. The `"premium"` key specifically maps to
the Play Console subscription whose *product ID* is `premium` — note its display *name* in Play
Console was set to `premium_monthly`, the reverse of the `pro_monthly`/`pro_plus_monthly` naming
pattern; double-check this against Play Console if purchase verification for Premium ever fails with
an "unknown product" error.

**Free trial on registration (2026-08-23).** A new account gets 1 month of `Pro` automatically, but
the entitlement is bound to the **device**, not the account — otherwise the user just registers again
next month. `RegisterRequest.DeviceId` (Android `ANDROID_ID`) is hashed with SHA-256 and claimed in
`DeviceTrials` (unique index). `TrialService` claims the device row *first* and only then writes the
subscription, so two simultaneous registrations from one device can't both win. The `DeviceTrials`
row has **no FK to `AspNetUsers` on purpose** — deleting the account must not release the device, or
"delete account → re-register" would bypass the whole thing. Granted subscriptions use
`SubscriptionPlatform.Trial` so they're distinguishable from real purchases in reporting.

A missing `DeviceId` yields **no trial** (Free), deliberately: if absence granted a trial, omitting
the field would be a trivial bypass. Registration never fails because of the trial path — any error
there is logged and swallowed.

`TrialSettings.RequireIntegrityToken` is the seam for Play Integrity, which is **not set up yet**.
`RegisterRequest.IntegrityToken` is already accepted (and ignored) so that turning verification on
later doesn't require a new app release. Until then, `ANDROID_ID` is trusted on its own and a
determined user can forge it — a known, accepted risk documented in `FRONTEND_TRIAL.md`. Note that
Play Integrity alone does **not** supply a stable device ID; it attests that a real app on a real
device sent the ID. (Play Integrity "Device Recall" is the feature that survives factory reset —
verify its current availability before relying on it.)

`SubscriptionController` handles real Google Play Billing (`/google/verify` validates a purchase
server-side against the Google Play Developer API; `/google/rtdn` is the Pub/Sub push webhook for
Google-initiated subscription state changes, secured by a shared-secret query param, idempotent via
`ProcessedGoogleNotifications`). Daily usage resets at UTC midnight.

**`POST /subscribe` (manual/test plan assignment) was removed 2026-08-24.** It had no restriction
beyond `[Authorize]` — any authenticated user could grant themselves any plan for any duration for
free. It went unnoticed through most of this project's development because it was the tool used to
set up test accounts throughout. If a manual grant is ever needed again (e.g. customer support
comping a subscription), do it via a direct DB write, not a re-exposed authenticated endpoint.

## Reference docs in this repo

- `DEPLOYMENT.md` — production deployment, required env vars, Postgres/UTC gotcha, Docker.
- `POSTMAN_DOCS.md` — full endpoint reference with request/response examples for every route (23
  requests across Auth/Account/Essay/Subscription); `EssayCheck.postman_collection.json` is the
  importable collection.
- `FRONTEND_NATURAL_EXPRESSION_STAT_CARD.md`, `FRONTEND_TEACHER_GROUPS.md`, `FRONTEND_LESSONS.md`,
  `FRONTEND_PREMIUM_PLAN.md`, `FRONTEND_TRIAL.md` — notes written for frontend/mobile-side
  implementation of specific features (natural-expression scoring UI, teacher groups/students +
  analytics, topic-explanation lessons, the four-plan pricing model, the device-bound free trial). `FRONTEND_UNIFIED_PLAN_LIMITS.md` is **stale** as of
  2026-08-23 (describes 3 plans, shows `ProPlus` as unlimited) — `FRONTEND_PREMIUM_PLAN.md`
  supersedes it; keep it around for its OCR-unification history but don't hand it to frontend as
  current.
- `BACKEND_LESSON_FEATURE.md` — the frontend team's original order for the lesson feature; the
  built result and the three places it deviates are documented in `FRONTEND_LESSONS.md` §1.

## Notes

- `Yeni klasör/` is an unrelated tooling checkout (dotnet-skills marketplace — agents/skills for
  general .NET work), git-ignored, not part of this project's source.
- All responses use string enums (`JsonStringEnumConverter`), not integers.
- No CI/CD pipeline yet — deploys are manual `docker build` + `docker run`/`docker compose`.
- There is no eval harness for the prompts yet, so prompt changes ship unmeasured. If prompt quality
  work continues, building one (golden essays with teacher-marked expected errors, measuring recall /
  hallucination rate / score MAE across models) is the prerequisite for judging any further change.
