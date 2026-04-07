# BeerTaste: Evolution to Interactive Web Application

## Context

BeerTaste is currently a read-only web app that displays beer tasting results, paired with a Console app that handles all data entry (creating events, adding beers/tasters, entering scores). The long-term goal is to become a **web-only, Kahoot-style interactive beer tasting platform** where tasters score beers on their own devices in real-time. The Console project will eventually be removed.

Authentication will use **Firebase** (Google OAuth + email login), following the same pattern as the HabitTeller repo. The branch `copilot/add-login-widget-feature` already has client-side Firebase auth wired up (login widget, Google sign-in popup, all templates updated with `firebaseConfig` parameter threading). This plan builds on that work.

This plan is incremental — each step is independently deployable. Only the first ~5 tasks are detailed; later tasks are broad.

---

## Existing Work (branch `copilot/add-login-widget-feature`)

Already done on this branch:
- `FirebaseConfig` type in `Localization.fs` (ApiKey, AuthDomain, ProjectId)
- Firebase SDK v10.7.1 loaded in `Layout.fs` with `onAuthStateChanged` listener
- Google sign-in popup via `firebaseScripts` function
- Login widget in nav bar across all pages
- All 12 templates updated to accept `firebaseConfig` parameter
- CSS for login widget (black & white theme)
- EN/NO translations for Login/Logout
- README with Firebase setup instructions
- Config via user-secrets: `BeerTaste:Firebase:ApiKey`, `AuthDomain`, `ProjectId`

**Not done yet:** Server-side token validation, sessions, Users table, protected routes, any write endpoints.

---

## Phase 1: Foundation — Server-Side Auth (builds on existing branch)

### Task 1: Move BeerTasteTableStorage and DataCache into DI

Pure refactor. Currently these are manually instantiated in `main` and threaded through as parameters. Moving them to DI is prerequisite for auth middleware and new services.

**Modify `BeerTaste.Web/Program.fs`:**
- Register `BeerTasteTableStorage` as singleton (from connection string config)
- Register `DataCache` as singleton (depends on storage + IMemoryCache)
- Remove `dc` / `storage` parameters from `endpoints` and `configureApp`
- Each handler resolves `DataCache` via `ctx.GetService<DataCache>()`

No user-visible changes.

### Task 2: Add Users, Sessions tables and server-side auth middleware

Follow the HabitTeller pattern: Firebase ID tokens are exchanged for server-side sessions stored in Azure Table Storage.

**Add `FirebaseAdmin` NuGet package** to `BeerTaste.Web/BeerTaste.Web.fsproj`

**Create `BeerTaste.Common/Users.fs`** (new file, add to `.fsproj` between `BeerTaste.fs` and `Scores.fs`):
- `User` record: `AuthenticationScheme: string`, `AccountId: string` (Firebase UID), `UserId: Guid`, `Name: string`
- Entity conversion: PartitionKey = AuthenticationScheme (e.g. `"Firebase"`), RowKey = AccountId
- `addUser`, `fetchUser`, `getOrCreateUser`

**Create `BeerTaste.Common/Sessions.fs`** (new file, after Users.fs):
- `Session` record: `SessionId: Guid`, `UserId: Guid`, `AccountId: string`, `AuthScheme: string`, `Name: string`, `LastActiveAt: DateTimeOffset`
- Entity conversion: PartitionKey = first 8 chars of SessionId, RowKey = full SessionId
- `addSession`, `fetchSession`, `deleteSession`, `updateLastActiveAt`
- Session expiry: 90 days, update `LastActiveAt` only if >1 hour old

**Modify `BeerTaste.Common/Storage.fs`:**
- Add `UsersTableClient` and `SessionsTableClient` (tables: `"users"`, `"sessions"`)

**Create `BeerTaste.Web/FirebaseAuth.fs`** (before Program.fs in compilation):
- `FirebaseServerConfig` type: `ProjectId`, optional `ServiceAccountKeyPath`
- `initialize()`: sets up FirebaseAdmin SDK
- `verifyIdToken(token)`: validates Firebase ID token, returns claims

**Create `BeerTaste.Web/AuthMiddleware.fs`** (after FirebaseAuth.fs):
- Custom middleware following HabitTeller pattern:
  - Path 1: `Authorization: Bearer <token>` → verify with Firebase → get/create user → set in `HttpContext.Items`
  - Path 2: `session` cookie → lookup in sessions table → validate expiry → set user in `HttpContext.Items`
- Helper: `getCurrentUser(ctx)` returns `User option`

**Modify `BeerTaste.Web/Program.fs`:**
- Add auth middleware to pipeline
- Add `POST /auth/session` endpoint: accepts Firebase ID token, creates session, sets HttpOnly cookie
- Add `POST /auth/logout` endpoint: deletes session, clears cookie
- Initialize Firebase Admin SDK on startup

### Task 3: Add POST infrastructure and anti-forgery

**Modify `BeerTaste.Web/Program.fs`:**
- Add `AddAntiforgery()` to DI
- Add `UseAntiforgery()` to middleware pipeline

**Create `BeerTaste.Web/FormHelpers.fs`:**
- Helper to render anti-forgery hidden input in Oxpecker views

### Task 4: Wire up client-side login to server-side sessions

Update the existing Firebase client code (from the branch) to exchange tokens for sessions.

**Modify `BeerTaste.Web/templates/Layout.fs`:**
- In `firebaseScripts`: after successful Google sign-in or email link callback, call `user.getIdToken()` and POST to `/auth/session`
- On logout: POST to `/auth/logout` in addition to `firebase.auth().signOut()`
- Auth state now driven by server-side session (cookie), not just client-side Firebase state

**Modify `BeerTaste.Web/templates/Layout.fs` (nav):**
- Show user name from server-side session (passed to template), not just client-side JS
- Fallback: client-side JS still updates UI for immediate feedback

### Task 5: Create Beer Tasting via Web

First write feature — authenticated users can create a new tasting event.

**Modify `BeerTaste.Common/BeerTaste.fs`:**
- Add `AdminEmail: string` field to `BeerTaste` record
- Update entity conversion functions
- Update `addBeerTaste` to accept `adminEmail`

**Modify `BeerTaste.Console/Workflow.fs`:**
- Pass a hardcoded admin email to keep Console functional

**Create `BeerTaste.Web/templates/CreateBeerTasteView.fs`:**
- Form: ShortName, Description, Date → POST `/create`

**Modify `BeerTaste.Web/Program.fs`:**
- Add GET/POST `/create` (requires auth via `getCurrentUser`)
- Homepage shows "Create New Tasting" button when logged in

---

## Phase 2: Beer and Taster Management

### Task 6: Admin Beer Management UI
Web forms for taste admin to add/edit/delete beers. Authorization: only the user whose email matches `AdminEmail`. Add individual `addBeer`/`deleteBeer` to Common. Invalidate DataCache on writes.

### Task 7: Taster Management — Manual Mode
Admin adds tasters (name + email) via web form. Individual `addTaster`/`deleteTaster` in Common.

### Task 8: Taster Self-Registration — Kahoot Mode
Public page at `/{beerTasteGuid}/join` — anyone with the link or QR code can register as a taster. Admin dashboard shows QR code. No auth required for this page.

---

## Phase 3: Scoring

### Task 9: Token-Based Scoring Links
Generate unique scoring token per taster (new column on taster entity, or separate tokens table). Email tasters a link like `/{beerTasteGuid}/score?token=xxx`. Scoring page shows all beers, taster enters scores 1-10. Individual `upsertScore` in Common.

### Task 10: Admin Dashboard
`/{beerTasteGuid}/admin` — shows beers, tasters, score completion matrix, QR code. Only visible to taste admin. Initially requires manual refresh.

---

## Phase 4: Live Experience

### Task 11: Real-Time Dashboard
Add SignalR (or server-sent events) to the admin dashboard. Score submissions trigger live updates.

### Task 12: Beer-by-Beer Scoring Mode (optional embellishment)
Admin controls which beer is currently active. Taster scoring page shows only the current beer. Add `CurrentBeerIndex` to the beertaste entity.

---

## Phase 5: Results and Polish

### Task 13: Admin Publishes Results
Add `Published` boolean to beertaste entity. Results pages return 403 for non-admins until published.

### Task 14: Email Results from Web
Move email-sending from Console to Web. "Send Results Email" button on admin dashboard. Reuse existing `Email.createBeerTasteResultsEmail` and `Email.sendEmails`.

### Task 15: My Tastings Page
`/my-tastings` — lists all tastings where current user is admin. Landing page for authenticated users.

---

## Phase 6: Cleanup

### Task 16: Input Validation and Error Handling
Server-side validation for all forms. Friendly error messages. Optional client-side validation.

### Task 17: Cache Invalidation on Writes
Explicit `cache.Remove(key)` after web UI writes, or shorter TTL for active tastings.

### Task 18: Console Deprecation
Mark Console features overlapping with Web as deprecated. Ensure Console compiles with updated Common types. No new Console features.

---

## Key Files Touched Across Plan

| File | Tasks |
|------|-------|
| `BeerTaste.Web/Program.fs` | 1-5, 6-10, 11, 13-14 |
| `BeerTaste.Common/Storage.fs` | 2 |
| `BeerTaste.Common/Users.fs` (new) | 2 |
| `BeerTaste.Common/Sessions.fs` (new) | 2 |
| `BeerTaste.Common/BeerTaste.fs` | 5, 12, 13 |
| `BeerTaste.Web/FirebaseAuth.fs` (new) | 2 |
| `BeerTaste.Web/AuthMiddleware.fs` (new) | 2 |
| `BeerTaste.Web/FormHelpers.fs` (new) | 3 |
| `BeerTaste.Web/Localization.fs` | 5-10, 13-15 |
| `BeerTaste.Web/templates/Layout.fs` | 4 |
| `BeerTaste.Web/BeerTaste.Web.fsproj` | 1-5 |

## Reference: HabitTeller Auth Pattern

The server-side auth follows the HabitTeller repo pattern:
- `FirebaseAuth.fs` — Firebase Admin SDK init + token verification
- `AuthMiddleware.fs` — custom middleware checking Bearer token or session cookie
- `POST /auth/session` — exchanges Firebase ID token for server-side session
- `POST /auth/logout` — clears session
- Users table: PartitionKey = `"Firebase"`, RowKey = Firebase UID
- Sessions table: PartitionKey = first 8 chars of SessionId, RowKey = full SessionId
- HttpOnly, Secure, SameSite=Strict cookies, 90-day expiry

## Verification (per task)

- Build: `dotnet build` from root (all 3 projects must compile)
- Tests: `dotnet test BeerTaste.Tests/BeerTaste.Tests.fsproj`
- Manual: run web app (`dotnet run --project BeerTaste.Web`) and verify existing result pages still work
- Console: `dotnet build BeerTaste.Console/BeerTaste.Console.fsproj` (must compile after Common changes)
