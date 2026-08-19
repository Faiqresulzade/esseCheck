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
here. The model does support `seed`, which is not wired up yet. OCR still uses `gpt-4o-mini`.

Both `DetectionRules` and `ScoringRules` are deliberately byte-for-byte identical across all requests
(grade/topic/word-count/detected-errors are injected in a separate later message) so Anthropic's
prompt caching applies and cuts the cost of that portion by ~90%. When editing a prompt, keep
essay-specific values out of the constant.

`POST /api/essay/ocr` (Pro Plus only, `403` for Free/Pro) reads an image and returns plain text —
it does **not** persist or evaluate; the client is expected to review/edit the OCR text, then call
`/evaluate` separately with `source: "Image"`.

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
- `FRONTEND_UNIFIED_PLAN_LIMITS.md`, `FRONTEND_NATURAL_EXPRESSION_STAT_CARD.md` — notes written for
  frontend/mobile-side implementation of specific features (plan limit display, natural-expression
  scoring UI).

## Notes

- `Yeni klasör/` is an unrelated tooling checkout (dotnet-skills marketplace — agents/skills for
  general .NET work), git-ignored, not part of this project's source.
- All responses use string enums (`JsonStringEnumConverter`), not integers.
- No CI/CD pipeline yet — deploys are manual `docker build` + `docker run`/`docker compose`.
- There is no eval harness for the prompts yet, so prompt changes ship unmeasured. If prompt quality
  work continues, building one (golden essays with teacher-marked expected errors, measuring recall /
  hallucination rate / score MAE across models) is the prerequisite for judging any further change.
