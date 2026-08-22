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
`ValidationProblemDetails` → Swagger (enabled in **all** environments, including production) →
strongly-typed settings with fail-fast `ValidateOnStart()` (except `GooglePlaySettings`, which is
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

### Lessons (topic explanations)

`POST /api/lessons` builds a 6-8 slide English lesson (Azerbaijani explanations, English examples)
plus a 3-question quiz; `GET /api/lessons`, `GET|DELETE /api/lessons/{id}` are free of AI and quota.
Built to the frontend order in `BACKEND_LESSON_FEATURE.md`.

- Uses its own model (`OpenRouter:LessonModel`, currently `gpt-4o-mini`) and its own daily counter
  (`DailyUsage.LessonCount`, `PlanPolicy.LessonDailyLimit` = 1/5/unlimited) — **completely separate
  from the essay limit**, so a Free user gets 1 essay *and* 1 lesson per day.
- Off-topic requests come back through the same one-call pattern as essays: the AI returns
  `isEnglishTopic: false` → `422`, nothing persisted, counter untouched. There is no second
  validation call, so a rejected topic costs one cheap AI call and no quota.
- **Three-level saving.** (1) The user already has that topic+grade → the existing lesson is
  returned, no AI *and no quota*. (2) A `LessonTemplate` exists → no AI, but quota is still charged
  (deliberate, per spec §6). (3) Otherwise generate and cache. The cache key is
  `NormalizedTopic + Grade + LessonPrompts.Version`, so **bumping `LessonPrompts.Version` after
  editing the prompt is mandatory** — otherwise every user keeps getting the pre-edit lesson forever.
- `LessonContentMapper` deliberately does *not* repair slide/quiz counts (the product decision was
  "return what the AI gave"). It only guarantees every field is present (null/empty array), drops
  a quiz question whose `correctIndex` falls outside its own `options` (such a question would mark
  the right answer wrong), and **rotates the options so the correct answer lands on a different
  position in each question** — `gpt-4o-mini` put every correct answer at index 0 in every measured
  lesson despite the prompt forbidding it. The rotation is a cyclic shift driven by a stable FNV-1a
  hash of the question text plus the question's position, so it is deterministic (never `Random`,
  never `string.GetHashCode`, which is per-process randomised) and a cached template can never
  disagree with the lessons copied from it. Options containing "above"/"yuxarıdakı" are left alone.
- Slides and quiz are nested JSON columns (`OwnsMany(...).ToJson()` with owned collections inside);
  verified to round-trip losslessly including `comparison` and `examples`.
- **Measured 2026-08-22 with `gpt-4o-mini`:** structure is reliable (7 slides in the right order,
  all `highlight` values were literal substrings, 3×4 quiz options, correct 422/429/404 behaviour),
  but content quality is mediocre: the index-0 problem is now fixed in code (see above), yet the
  Grade9/Grade11 difference remains weak (703 vs 975 characters of explanation, near-identical
  examples) and summary points tend toward the generic. Switching `LessonModel` is a config-only
  change if this matters more later.

### Error response conventions (see POSTMAN_DOCS.md §1.6 for full detail)

- Validation/logic errors: `{ "message": "..." }`, usually `400`.
- Auth/account mutation endpoints (register/login/forgot-reset password/account ops): `AuthResult`
  shape `{ "succeeded": bool, "message": string, "errors": string[] }`.
- Unhandled server errors: caught by `GlobalExceptionHandler` → `{ "message": "Gözlənilməz xəta baş verdi." }`.
- AI service unavailable: `503` (temporary) or `502` (unreachable).

### Auth model

JWT access token + rotating refresh token (hashed with SHA-256 in DB). Refresh endpoint revokes the
used token immediately and issues a new pair — reusing an old refresh token is rejected (standard
theft-detection pattern). Password reset and password change both revoke **all** refresh tokens for
the user. Account deletion is soft (`IsDeleted=true`); essay history is preserved, only login is
blocked.

### Subscriptions

Plans: `Free` (1 essay/day, no OCR), `Pro` (unlimited text, no OCR), `ProPlus` (unlimited text + OCR).
Lessons have their own separate daily allowance on top of this (see above).
`SubscriptionController` supports manual/test plan assignment (`/subscribe`) plus real Google Play
Billing (`/google/verify` validates a purchase server-side against the Google Play Developer API;
`/google/rtdn` is the Pub/Sub push webhook for Google-initiated subscription state changes, secured
by a shared-secret query param, idempotent via `ProcessedGoogleNotifications`). Daily usage resets at
UTC midnight.

## Reference docs in this repo

- `DEPLOYMENT.md` — production deployment, required env vars, Postgres/UTC gotcha, Docker.
- `POSTMAN_DOCS.md` — full endpoint reference with request/response examples for every route (23
  requests across Auth/Account/Essay/Subscription); `EssayCheck.postman_collection.json` is the
  importable collection.
- `FRONTEND_UNIFIED_PLAN_LIMITS.md`, `FRONTEND_NATURAL_EXPRESSION_STAT_CARD.md`,
  `FRONTEND_TEACHER_GROUPS.md`, `FRONTEND_LESSONS.md` — notes written for frontend/mobile-side
  implementation of specific features (plan limit display, natural-expression scoring UI, teacher
  groups/students + analytics, topic-explanation lessons).
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
