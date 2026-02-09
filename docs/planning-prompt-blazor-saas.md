# Planning Prompt: Blazor Admin + Blazor/MAUI Mobile + Stripe SaaS + Landing Page

Use this prompt in Claude CLI planning mode (`/plan`).

---

## Prompt

I need a phased implementation plan to extend my existing .NET 10 TrainingApp into a SaaS product with a public landing page, an admin dashboard, a mobile app, and Stripe subscription billing. The existing codebase is a headless REST API (92 endpoints, 24 domain entities, PostgreSQL, EF Core 10, ASP.NET Core Identity already configured but not enforced). All projects target net10.0 with C# 13, file-scoped namespaces, nullable reference types, and warnings-as-errors.

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
- The app has a sophisticated training engine: periodized program generation, RPE-based autoregulation, Banister fatigue modeling (CTL/ATL/TSB), metabolic adaptation tracking, concurrent training interference analysis, and AI-driven training insights

### What I Need Built

#### 1. Public Landing Page & Marketing Site (`TrainingApp.Web`)
- Blazor Static SSR or Razor Pages — fast, SEO-friendly, minimal JS
- This is the front door of the product. It needs to convert visitors into trial signups. The copy and structure should make a compelling case.
- **App identity**: The app name is "TrainingApp" (placeholder — plan should note where name/branding is referenced so it's easy to rename later). Positioning: the smartest training companion for lifters who take their progress seriously.
- **Page structure**:
  - **Hero section**: Bold headline communicating the core value prop — this isn't another workout logger, it's an adaptive training system that learns how you respond to training and adjusts in real time. Clear CTA: "Start your 30-day free trial." Secondary CTA: links to App Store.
  - **Problem/Solution section**: Most lifters either follow cookie-cutter programs that don't adapt, or try to self-coach without the data to make good decisions. TrainingApp bridges that gap — it gives you the intelligence of a high-level coach in your pocket.
  - **Key feature showcase** (with visuals/mockup placeholders):
    - **Autoregulation that actually works**: RPE-based load adjustment every session. The app reads your readiness and modifies your training in real time — not just "do less when tired" but intelligent volume and intensity manipulation based on your fatigue signature.
    - **Periodized program generation**: Accumulation, intensification, and deload phases built from your training history, not generic templates. Programs that evolve as you do.
    - **Fatigue modeling you can see**: Real-time CTL/ATL/TSB charts so you understand your fitness, fatigue, and preparedness at a glance. Know exactly when to push and when to back off.
    - **Metabolic intelligence**: Track deficit phases with adaptive TDEE, metabolic adaptation detection, and automatic diet break scheduling. Your nutrition strategy informed by real data, not guesswork.
    - **Concurrent training management**: Run strength and cardio simultaneously without interference. The app analyzes session timing and modality to protect your gains.
    - **Training insights engine**: Pattern recognition across your training history surfaces actionable insights — plateau detection, volume landmarks, recovery trends, and progression opportunities you'd miss on your own.
  - **Social proof section**: Placeholder for testimonials, metrics ("X workouts tracked", "Y programs generated"), and trust signals
  - **Plan comparison**: Side-by-side comparison of the two tiers (see below) with clear differentiation. Annual pricing shown with monthly equivalent and savings percentage. Both plans start with 30-day free trial.
  - **FAQ section**: Common questions about the trial, billing, data ownership, platform availability
  - **Footer**: Links to App Store, privacy policy, terms of service, support contact
- **Additional pages**:
  - `/pricing` — Detailed plan comparison with feature matrix
  - `/privacy` and `/terms` — Legal pages (template content, to be reviewed by legal)
  - `/support` — Contact form or help center link
- **Technical requirements**:
  - Fast initial load — target < 1s LCP
  - Mobile-responsive design
  - Open Graph / Twitter Card meta tags for social sharing
  - Structured data (JSON-LD) for SEO
  - Analytics integration point (Google Analytics / Plausible placeholder)
  - App Store badge/link placement
  - Cookie consent banner (GDPR compliance)

#### 2. Blazor Server Admin Dashboard (`TrainingApp.Admin`)
- Blazor Server (not WASM) for the admin portal — keeps secrets server-side, simpler deployment
- Admin authentication with role-based access (Admin, SuperAdmin roles)
- Pages needed:
  - **Dashboard**: Active subscribers count, MRR, ARR, churn rate, trial conversion rate, new signups chart, revenue chart, active trials approaching expiry
  - **User Management**: List/search/filter users, view user details and training activity, impersonate user, suspend/reactivate accounts, reset passwords, assign roles, view subscription history per user
  - **Subscription Management**: View all subscriptions, filter by plan/status, handle cancellations/refunds, apply credits/coupons, manage plan changes, extend trials, view upcoming renewals
  - **Plan Configuration**: CRUD for subscription plans (name, price, billing interval, feature limits), map plans to feature flags/limits
  - **Stripe Integration Monitor**: Webhook event log with filtering, failed payment alerts and retry status, sync status dashboard, revenue reconciliation
  - **Trial Funnel**: Trial signups over time, conversion rate by cohort, drop-off analysis, users approaching trial end (for outreach)
  - **System Health**: API health status, background job status/history, database metrics, error log viewer
  - **Exercise Library Management**: Approve/reject custom exercises, manage wger sync
  - **Content/Notifications**: Send announcements, manage in-app notification templates, push notification campaigns
- Use MudBlazor or Radzen component library for rapid UI development
- The admin app should call the API (not bypass it to the database directly) to maintain clean architecture — add new `/api/v1/admin/*` endpoints as needed

#### 3. Blazor Hybrid MAUI App for iPhone (`TrainingApp.Mobile`)
- .NET MAUI Blazor Hybrid targeting iOS (iPhone primary, iPad secondary)
- Shares Blazor Razor components with a shared component library (`TrainingApp.UI.Shared`)
- User-facing features consuming existing API endpoints:
  - **Onboarding**: Account creation, profile setup (experience level, body metrics, goals), 30-day trial starts automatically on signup, plan selection shown near trial end + Stripe checkout
  - **Dashboard**: Today's training summary, readiness score, weight trend, quick-log actions
  - **Workout Tracking**: Start/complete workouts, log sets with RPE, real-time autoregulation recommendations, rest timer
  - **Programs**: Browse/generate periodized programs, view program calendar, active program progress
  - **Body Metrics**: Weight logging with trend chart, body composition tracking
  - **Cardio**: Log cardio sessions, weekly TRIMP summary, zone distribution
  - **Analytics/Charts**: Strength progression (e1RM), volume trends, fatigue model (CTL/ATL/TSB), body weight moving averages
  - **Goals**: Create/track goals with checkpoints, progress visualization
  - **Recovery**: Log sleep/stress/energy, readiness score breakdown
  - **Insights**: AI-generated training insights with actionable recommendations
  - **Settings**: Profile management, subscription management (upgrade/downgrade/cancel via Stripe Customer Portal), notification preferences, data export
  - **Partner Training** (Competitor tier only): Invite partners, shared session scheduling, interleaved workout generation
- **Trial experience**: Full access to all features for 30 days. Gentle, non-annoying prompts starting at day 20 showing value delivered ("You've logged X workouts, tracked Y PRs, generated Z insights — keep going?"). At trial end, app becomes read-only (can view history but not log new data) until they subscribe.
- Offline-first architecture with local SQLite cache and background sync
- Push notifications via Apple Push Notification Service (APNs) for workout reminders and trial milestones
- Secure token storage via iOS Keychain (via MAUI SecureStorage)
- Native iOS feel — use platform-appropriate navigation patterns (tab bar, pull-to-refresh, haptic feedback)

#### 4. Stripe SaaS Billing Integration (in API + Core + Infrastructure)
- New domain entities in Core:
  - `SubscriptionPlan` (Id, Name, Slug, StripePriceIdMonthly, StripePriceIdAnnual, MonthlyPrice, AnnualPrice, Features as JSON, Limits as JSON, TrialDays, DisplayOrder, IsActive)
  - `UserSubscription` (Id, UserId, PlanId, StripeSubscriptionId, StripeCustomerId, Status [Trialing/Active/PastDue/Canceled/Expired], CurrentPeriodStart, CurrentPeriodEnd, TrialStart, TrialEnd, CancelAtPeriodEnd, CanceledAt, BillingInterval [Monthly/Annual])
  - `PaymentHistory` (Id, UserId, StripePaymentIntentId, StripeInvoiceId, Amount, Currency, Status, Description, InvoiceUrl, ReceiptUrl, CreatedAt)
  - `WebhookEvent` (Id, StripeEventId, Type, Payload, ProcessedAt, Status [Pending/Processed/Failed], ErrorMessage, RetryCount)
- Stripe integration in Infrastructure:
  - `IStripeService` interface in Core, implementation in Infrastructure using Stripe.net SDK
  - Checkout Session creation for post-trial conversion
  - Customer Portal session creation for self-service management
  - Subscription lifecycle management (create, update, cancel, reactivate)
  - Webhook handler for events: `checkout.session.completed`, `customer.subscription.created/updated/deleted`, `invoice.payment_succeeded/failed`, `customer.updated`, `charge.refunded`
  - Idempotent webhook processing using WebhookEvent entity (dedup on StripeEventId)
  - Grace period handling for failed payments (3-day grace before downgrade)
- **No free tier. All users get a 30-day free trial with full feature access, then must choose a paid plan.**
- Subscription plans — two tiers designed for genuinely different users:

  **Lifter** ($12.99/mo or $119/yr — save 24%):
  *For the dedicated gym-goer who wants smarter training without the complexity.*
  - Unlimited workout logging with RPE tracking
  - Autoregulation — real-time load adjustments based on daily readiness
  - Periodized program generation (one active program at a time)
  - Full analytics suite: strength progression (e1RM), volume tracking, body weight trends with moving averages
  - Weight and body composition tracking with projections
  - Cardio logging with TRIMP and weekly summaries
  - Goal setting with progress checkpoints
  - Recovery logging (sleep, stress, energy) with readiness scoring
  - Data export (CSV)
  - Push notification reminders

  **Competitor** ($24.99/mo or $229/yr — save 24%):
  *For the serious athlete, coach-in-training, or competitor who needs the full picture.*
  Everything in Lifter, plus:
  - **Fatigue modeling dashboard**: Full CTL/ATL/TSB curves with fitness/freshness visualization — the same model used by elite endurance and strength coaches
  - **Metabolic intelligence**: Adaptive TDEE tracking, metabolic adaptation detection, automated diet break scheduling, deficit phase management with rate-of-loss optimization
  - **Concurrent training manager**: Interference analysis between strength and cardio sessions, optimal session spacing recommendations, modality-specific impact tracking
  - **Advanced insights engine**: Pattern recognition across your full training history — plateau detection, volume landmark identification, recovery trend analysis, progression opportunity alerts
  - **Multiple active programs**: Run concurrent periodized programs (e.g., peaking for a meet while maintaining a GPP block)
  - **Partner training**: Invite training partners, shared session scheduling, interleaved workout generation for gym partners
  - **Priority support**: Faster response times, direct feedback channel

  The distinction: **Lifter** gives you everything you need to train smart and make consistent progress. **Competitor** adds the deep analytical and coaching tools that competitive athletes, serious bodybuilders, and powerlifters need to optimize performance at a higher level. Competitor isn't "Lifter with extras bolted on" — it's a fundamentally more powerful training intelligence system.

- Feature gating middleware/service:
  - `ISubscriptionGuard` that checks user's active plan/trial status before allowing access to gated endpoints
  - During trial: all features unlocked (users experience Competitor-level access so they see full value)
  - After trial: enforce tier limits based on chosen plan
  - Gated Competitor features: fatigue model endpoints, metabolism/deficit endpoints, concurrent training analysis, insights generation, partner/shared session endpoints, multiple program creation
  - Return 402 Payment Required with a clear message about which plan unlocks the feature
  - Trial expiry: return 403 with trial-expired status and checkout link

#### 5. Authentication & Authorization (extend existing)
- Add JWT Bearer authentication for mobile app (access + refresh tokens)
- Add cookie authentication for admin Blazor Server app
- Auth endpoints: `/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/auth/refresh`, `/api/v1/auth/forgot-password`, `/api/v1/auth/reset-password`, `/api/v1/auth/confirm-email`
- Registration automatically creates a Stripe customer and starts a 30-day trial UserSubscription
- Role-based authorization: User (default), Admin, SuperAdmin
- Admin endpoints require Admin or SuperAdmin role
- Wire up ICurrentUserService to extract user from JWT claims (replace hardcoded dev user)
- Email confirmation flow (use a simple email service interface — can plug in SendGrid/Mailgun later)
- Apple Sign In support for iOS app (future-ready interface, implement later)

#### 6. Shared Component Library (`TrainingApp.UI.Shared`)
- Razor Class Library (RCL) shared between Admin, Mobile, and Landing Page where appropriate
- Shared components: charts (using a Blazor charting library), data tables, form components, loading states, error boundaries, plan comparison card component
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
- Landing page must be fast — no heavy Blazor WASM bundle for the marketing site
- Branding/app name references should be centralized so renaming is a single change

### Deliverables I Want in the Plan
1. **Updated solution structure** showing all new projects and their dependencies
2. **Phased implementation roadmap** (what to build in what order, with dependencies between phases)
3. **New entity designs** with properties and relationships
4. **New API endpoint specifications** for auth, admin, subscription management, and trial lifecycle
5. **Migration strategy** for adding subscription tables to existing database
6. **Authentication flow diagrams** for both mobile (JWT) and admin (cookie) apps
7. **Stripe integration architecture** — webhook flow, event processing, subscription state machine, trial-to-paid conversion flow
8. **Offline sync strategy** for the MAUI app
9. **Shared component library structure** — what goes in shared vs. app-specific
10. **Feature gating design** — how the two subscription tiers map to API access, with full feature-to-endpoint mapping
11. **Trial lifecycle design** — signup through conversion or expiry, including the in-app prompting strategy and read-only fallback
12. **Landing page information architecture** — page structure, component breakdown, SEO strategy
13. **Deployment architecture** — how landing page, admin app, API, and mobile backend are hosted (consider single host vs. separate)
14. **Key risks and technical decisions** that need to be made early

Focus on practical, implementable steps. Prioritize getting auth + Stripe + landing page working first since the mobile app and admin dashboard depend on those foundations. Each phase should result in a working, testable increment.
