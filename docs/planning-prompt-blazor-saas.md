# Planning Prompt: Blazor Admin + Blazor/MAUI Mobile + Stripe SaaS

Use this prompt in Claude CLI planning mode (`/plan`).

---

## Prompt

I need a phased implementation plan to extend my existing .NET 10 TrainingApp into a SaaS product with two new frontend applications and Stripe subscription billing. The existing codebase is a headless REST API (92 endpoints, 24 domain entities, PostgreSQL, EF Core 10, ASP.NET Core Identity already configured but not enforced). All projects target net10.0 with C# 13, file-scoped namespaces, nullable reference types, and warnings-as-errors.

### Current Architecture

```
src/
├── TrainingApp.Api              # Minimal APIs, 18 endpoint files, Swagger, FluentValidation
├── TrainingApp.Core             # Domain entities, interfaces, services (zero external deps)
├── TrainingApp.Infrastructure   # EF Core 10, PostgreSQL, wger API (Refit+Polly), Identity stores
└── TrainingApp.Orchestration    # Background jobs (DailyMetricsJob, ExerciseSync, Reminders)
tests/
├── TrainingApp.Core.Tests
├── TrainingApp.Api.Tests
└── TrainingApp.Integration.Tests
```

Key facts:
- ASP.NET Core Identity is configured (User extends IdentityUser<Guid>, roles enabled, password policy set) but NO auth middleware or login endpoints exist yet
- All entities use UserId foreign keys — multi-user ready but no tenant isolation
- DbContext extends IdentityDbContext<User, IdentityRole<Guid>, Guid> with snake_case naming convention
- Docker-compose runs PostgreSQL 16 on port 5433
- No frontend code exists — no wwwroot, no package.json, no static files
- ICurrentUserService exists with dev fallback to hardcoded user ID

### What I Need Built

#### 1. Blazor Server Admin Dashboard (`TrainingApp.Admin`)
- Blazor Server (not WASM) for the admin portal — keeps secrets server-side, simpler deployment
- Admin authentication with role-based access (Admin, SuperAdmin roles)
- Pages needed:
  - **Dashboard**: Active subscribers count, MRR, churn rate, new signups chart, revenue chart
  - **User Management**: List/search/filter users, view user details, impersonate user, suspend/reactivate accounts, reset passwords, assign roles
  - **Subscription Management**: View all subscriptions, filter by plan/status, handle cancellations/refunds, apply credits/coupons, manage plan changes
  - **Plan Configuration**: CRUD for subscription plans (name, price, billing interval, feature limits), map plans to feature flags/limits
  - **Stripe Integration Monitor**: Webhook event log, failed payment alerts, sync status dashboard
  - **System Health**: API health status, background job status, database metrics, error log viewer
  - **Exercise Library Management**: Approve/reject custom exercises, manage wger sync
  - **Content/Notifications**: Send announcements, manage in-app notification templates
- Use MudBlazor or Radzen component library for rapid UI development
- The admin app should call the API (not bypass it to the database directly) to maintain clean architecture — add new `/api/v1/admin/*` endpoints as needed

#### 2. Blazor Hybrid MAUI App for iPhone (`TrainingApp.Mobile`)
- .NET MAUI Blazor Hybrid targeting iOS (iPhone primary, iPad secondary)
- Shares Blazor Razor components with a shared component library (`TrainingApp.UI.Shared`)
- User-facing features consuming existing API endpoints:
  - **Onboarding**: Account creation, profile setup (experience level, body metrics, goals), plan selection + Stripe checkout
  - **Dashboard**: Today's training summary, readiness score, weight trend, quick-log actions
  - **Workout Tracking**: Start/complete workouts, log sets with RPE, real-time autoregulation recommendations, rest timer
  - **Programs**: Browse/generate periodized programs, view program calendar, active program progress
  - **Body Metrics**: Weight logging with trend chart, body composition tracking
  - **Cardio**: Log cardio sessions, weekly TRIMP summary, zone distribution
  - **Analytics/Charts**: Strength progression (e1RM), volume trends, fatigue model (CTL/ATL/TSB), body weight moving averages
  - **Goals**: Create/track goals with checkpoints, progress visualization
  - **Recovery**: Log sleep/stress/energy, readiness score breakdown
  - **Settings**: Profile management, subscription management (upgrade/downgrade/cancel via Stripe Customer Portal), notification preferences, data export
  - **Partner Training**: Invite partners, shared session scheduling
- Offline-first architecture with local SQLite cache and background sync
- Push notifications via Apple Push Notification Service (APNs) for workout reminders
- Secure token storage via iOS Keychain (via MAUI SecureStorage)
- Native iOS feel — use platform-appropriate navigation patterns (tab bar, pull-to-refresh, haptic feedback)

#### 3. Stripe SaaS Billing Integration (in API + Core + Infrastructure)
- New domain entities in Core:
  - `SubscriptionPlan` (Id, Name, StripePriceId, MonthlyPrice, AnnualPrice, Features as JSON, Limits, IsActive, trial days)
  - `UserSubscription` (Id, UserId, PlanId, StripeSubscriptionId, StripeCustomerId, Status, CurrentPeriodStart/End, CancelAtPeriodEnd, TrialEnd)
  - `PaymentHistory` (Id, UserId, StripePaymentIntentId, Amount, Currency, Status, InvoiceUrl, CreatedAt)
  - `WebhookEvent` (Id, StripeEventId, Type, Payload, ProcessedAt, Status, ErrorMessage)
- Stripe integration in Infrastructure:
  - `IStripeService` interface in Core, implementation in Infrastructure using Stripe.net SDK
  - Checkout Session creation for new subscriptions
  - Customer Portal session creation for self-service management
  - Subscription lifecycle management (create, update, cancel, reactivate)
  - Webhook handler for events: `checkout.session.completed`, `customer.subscription.created/updated/deleted`, `invoice.payment_succeeded/failed`, `customer.updated`
  - Idempotent webhook processing using WebhookEvent entity
  - Metered billing support for potential future usage-based features
- Subscription plans (suggested starting tiers):
  - **Free**: Limited workouts/month, no program generation, no analytics charts, no partner features
  - **Pro** ($9.99/mo or $99/yr): Unlimited workouts, program generation, full analytics, data export
  - **Elite** ($19.99/mo or $199/yr): Everything in Pro + partner training, priority support, advanced insights
- Feature gating middleware/service:
  - `ISubscriptionGuard` that checks user's active plan before allowing access to gated endpoints
  - Enforce limits (e.g., max workouts/month for free tier) at the API level
  - Return 402 Payment Required or 403 with upgrade prompt for gated features

#### 4. Authentication & Authorization (extend existing)
- Add JWT Bearer authentication for mobile app (access + refresh tokens)
- Add cookie authentication for admin Blazor Server app
- Auth endpoints: `/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/auth/refresh`, `/api/v1/auth/forgot-password`, `/api/v1/auth/reset-password`, `/api/v1/auth/confirm-email`
- Role-based authorization: User (default), Admin, SuperAdmin
- Admin endpoints require Admin or SuperAdmin role
- Wire up ICurrentUserService to extract user from JWT claims (replace hardcoded dev user)
- Email confirmation flow (use a simple email service interface — can plug in SendGrid/Mailgun later)

#### 5. Shared Component Library (`TrainingApp.UI.Shared`)
- Razor Class Library (RCL) shared between Admin and Mobile apps
- Shared components: charts (using a Blazor charting library), data tables, form components, loading states, error boundaries
- Shared services: API client (typed HttpClient), auth state provider, theme/styling
- Shared models: Can reference TrainingApp.Api.Contracts or create a dedicated shared contracts package

### Constraints & Preferences
- Maintain clean architecture — Core must remain free of external dependencies
- All new entities need EF Core configurations, migrations, and snake_case naming
- Continue using minimal APIs for new endpoints (not controllers)
- Continue FluentValidation for all new request DTOs
- All new code must compile with warnings-as-errors and nullable reference types
- Prefer composition over inheritance
- Keep Stripe API keys in configuration (user-secrets for dev, environment variables for prod)
- Webhook endpoint must validate Stripe signatures
- Design for testability — all Stripe interactions behind interfaces for mocking
- Add new test projects as needed: `TrainingApp.Admin.Tests`, `TrainingApp.Mobile.Tests`
- Docker-compose should be updated to include any new services needed

### Deliverables I Want in the Plan
1. **Updated solution structure** showing all new projects and their dependencies
2. **Phased implementation roadmap** (what to build in what order, with dependencies between phases)
3. **New entity designs** with properties and relationships
4. **New API endpoint specifications** for auth, admin, and subscription management
5. **Migration strategy** for adding subscription tables to existing database
6. **Authentication flow diagrams** for both mobile (JWT) and admin (cookie) apps
7. **Stripe integration architecture** — webhook flow, event processing, subscription state machine
8. **Offline sync strategy** for the MAUI app
9. **Shared component library structure** — what goes in shared vs. app-specific
10. **Feature gating design** — how subscription tiers map to API access
11. **Deployment architecture** — how admin app, API, and mobile backend are hosted
12. **Key risks and technical decisions** that need to be made early

Focus on practical, implementable steps. Prioritize getting auth + Stripe + basic admin working first since the mobile app depends on those foundations. Each phase should result in a working, testable increment.
