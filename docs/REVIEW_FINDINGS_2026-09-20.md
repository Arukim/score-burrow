# Score Burrow review findings — 20 Sep 2026

Reviewed **`main` @ `f491b39`** (Bulwark, technical-loss restart, campaign heroes). Progress below is against **`main` @ `4ba7171`** (#19 stats projector, #21 cache invalidation, #22 create-game validation).

Sources: architecture review, product/UX review, quality/risks review, full codebase review, Bugbot (retried with natural-language diff after empty branch-vs-main).

Status key: **Done** = shipped after this review (PR). **Confirmed** = checked in source at review time. **Likely** = strong code evidence, not runtime-reproduced. **Design** = documented behaviour, not a defect unless product intent changes.

---

## Executive summary

The league loop (roster → create game → complete / technical loss / cancel → Glicko + history) works. Complete, tech-loss, import, and Recalculate now share `PlayerStatisticsProjector` (#19). League/game/player pages and stats caches drop via a shared cache token on create/complete/TL/cancel (#21; player performance + absolute expiry on this branch). Create-game validates membership, unique players/colors, and hero↔town (#22). Remaining operator risk is **overlapping in-progress games** (P1-1/P1-2, no status CAS). README no longer claims invites, color-adjusted win rate, or in-progress game editing (P2-19–P2-28). Position is still the start seat, not a finish place (P2-4).

Azure **F1 + SQL free serverless cannot be kept always-on** without exhausting quotas. See [Keep-alive on Azure free tier](#keep-alive-on-azure-free-tier).

---

## P0 — correctness operators already feel

| ID | Severity | Location | Finding | Status | Suggested fix |
|----|----------|----------|---------|--------|----------------|
| P0-1 | High | `GameService.cs` ~176–215 vs ~318–326 | `CompleteGameAsync` increments `GamesPlayed` / `GamesWon` and maybe favorite town. It does **not** set `WinRate`, `AveragePosition`, `FavoriteHeroId`, `LastUpdated`, or `LastRatingUpdate`. Tech-loss **does** update `WinRate` / `LastUpdated` / `LastRatingUpdate`. Profiles stay wrong until Recalculate. | **Done (#19)** | `PlayerStatisticsProjector` used by complete, TL, import, and Recalculate |
| P0-2 | High | `LeagueService.InvalidateLeagueCache` ~644–674; `Game.razor` `game_{id}` 15 min sliding | After complete/TL, cache misses `game_{id}`, other users’ `league_{id}_{userId}`, player pages, `player_performance_*`, `home_leagues_top10`. Sliding expiry can hide updates while a page is viewed. | **Done (#21 + this branch)** | Shared `GetLeagueCacheExpirationToken` on league/game/player pages, town/color stats, and `player_performance_*`; absolute expiry; invalidate on create/complete/TL/cancel |
| P0-3 | High | `GameService.CreateGameAsync` ~49–86; `CreateGame.razor` color dropdowns | Create-game does not validate membership∈league, unique players, unique colors, or hero↔town. Duplicate colors fail on unique `(GameId, PlayerColor)` with a SQL error. | **Done (#22)** | Service validator; unique `(GameId, LeagueMembershipId)`; wizard swaps taken colors |
| P0-4 | Medium | `GameService.CreateGameAsync` ~84; `CancelGameAsync` ~407–415 | Create and cancel do **not** call `InvalidateLeagueCache`. League list can omit a new game or still show cancelled as in progress. | **Done (#21)** | Invalidate on those paths too |
| P0-5 | Medium | `GameService.cs` ~196–207 | Favorite town counts **all** `GameParticipants`, including in-progress and cancelled. | **Done (#19)** | Projector restricts to `Status == Completed` |

---

## P1 — rating / lifecycle integrity

| ID | Severity | Location | Finding | Status | Suggested fix |
|----|----------|----------|---------|--------|----------------|
| P1-1 | Critical (if overlapping games) | `GameService.CompleteGameAsync` ~107–155 | Completions apply Glicko from **create-time snapshots**, then last-write-wins on `LeagueMembership`. Two in-progress games for one player (or complete racing TL) overwrite live rating. `RatingHistory` keeps both rows. No rowversion / status CAS / transaction. | Likely | `UPDATE … WHERE Status = InProgress`; transaction; optional `RowVersion` |
| P1-2 | High | `GameService.ApplyTechnicalLossAsync` vs `CompleteGameAsync` | Same in-memory `Status == InProgress` check. Two admins can pass it. | Likely | Compare-and-swap on status |
| P1-3 | High | DataImport `GameImporter` vs live `GameService` | Import sets `Position` as winner=1 / else=2; live sets **color seat**. TL import snapshots **culprit only**; live snapshots everyone at create. CSV `IsTechnicalLoss` can mark every row; live sets only culprit. Recalculate then means different things for old vs new games. | Confirmed (code comparison) | Align import with live: color position, snapshot all, culprit-only TL |
| P1-4 | High | Test projects | Only `ScoreBurrow.DataImport.Tests` (CSV grouping + date backtracker). No tests for Glicko-2, multiplayer, TL restart, GameService, stats, campaign heroes. | Partial — `ScoreBurrow.Data.Tests` (#19) + `ScoreBurrow.Web.Tests` create/cancel (#22). Still no Glicko-2 or complete/TL integration tests | `ScoreBurrow.Rating.Tests` + one complete/TL integration test |
| P1-5 | High | `GameService.CreateGameAsync` membership load | Membership loaded by Id only — not scoped to `leagueId`. Cross-league participant injection possible for a league admin. | **Done (#22)** | `m.Id == … && m.LeagueId == leagueId` |
| P1-6 | Medium | `CreateGame.razor` gold calculator | UI shows balance; `CanProceedFromStep4` only requires towns. Duplicate calculator rows can double-count. | Likely | Block Next unless `abs(sum(NetGold)) ≤ 1`; unique calculator player |
| P1-7 | Medium | `GameService` TL gold −1000 | Restarted game subtracts 1000 from culprit only; not zero-sum. Import warns on imbalance. | Design / product | Document as penalty **or** redistribute |
| P1-8 | Medium | `RatingService.ApplyTechnicalLossPenalty` | Self-loss vs self has `E = 0.5`. Penalty scales with **RD**, not rating. Comment claims the opposite. | Confirmed | Fix comment; or use fixed hit / reference opponent |

---

## P2 — stats product vs README

| ID | Severity | Location | Finding | Status | Suggested fix |
|----|----------|----------|---------|--------|----------------|
| P2-1 | High | `LeagueStatisticsService.cs` ~192–193; `Player.razor`; README | Color-adjusted win rate is **not implemented**. Color stats are loaded and discarded. DTO has no field. README documents a formula. | Confirmed | Ship it or delete the README section |
| P2-2 | Medium | `LeagueStatisticsService` color “WinRate” | Column is **share of wins in that game size** (`wins / totalWinsInSize`), not `wins / gamesOnThatColor`. Size threshold uses participant-slots, not games. | Confirmed | Rename or recompute; count games for threshold |
| P2-3 | Medium | `Player.razor` | `WorstTowns` / `WorstHeroes` exist on DTO, never shown. | Confirmed | Show or drop |
| P2-4 | Medium | README “position recording” | `Position` is start-color order; never updated on complete. Average Position is seating unless Recalculate (and Recalculate still uses stored Position). Import used finish-ish 1/2. | Confirmed | Label as start seat **or** capture finish places |
| P2-5 | Medium | League page “Games Played” | Can count non-completed statuses. | Likely | Count completed only |
| P2-6 | Low | `LeagueStatisticsService.CalculateColorDistributions` ~593 | Synchronous `Count()` per participation (N+1). | Confirmed | Use included collection or grouped query |

---

## P2 — data / heroes

| ID | Severity | Location | Finding | Status | Suggested fix |
|----|----------|----------|---------|--------|----------------|
| P2-7 | Medium | `20260920080900_AddCampaignHeroes.cs`; `ScoreBurrowDbContext` seed | Campaign heroes added (Catherine…Bidley) and show in wizard via `TownId`. Same migration sets Floribert/Wynona (and id 170) to **Mercenary**; original HotA seed was **Artificer**. Matches `heroes.csv`. | Confirm vs HotA | Restore Artificer if wiki is source of truth; do not blanket-update classes |
| P2-8 | Medium | DbContext `SeedHeroes` | Reference data lives in `OnModelCreating`; every hero change is a fat migration. | Confirmed | JSON/CSV seed job; migrations for schema only |
| P2-9 | Medium | CreateGame wizard vs import 3p colors | Live wizard locks first N enum slots (3p = Red/Blue/**Tan**). Import/tests use Red/Blue/**Teal**. Color stats mix two models. | Likely | Allow any unused color, or map import the same way |

---

## P2 — auth / membership / security

| ID | Severity | Location | Finding | Status | Suggested fix |
|----|----------|----------|---------|--------|----------------|
| P2-10 | Medium | `Program.cs` Identity | Open self-registration; `RequireConfirmedAccount = false`; password min 6, no non-alphanumeric. | Confirmed | Invite-only or confirm email for prod |
| P2-11 | Medium | `Login.cshtml.cs` | `lockoutOnFailure: false`. | Likely | Enable lockout |
| P2-12 | Medium | `Login.cshtml` vs `Create.razor` | `returnUrl` query is not posted as a hidden field; POST often lands on home. | Likely | Hidden `ReturnUrl` |
| P2-13 | Medium | `LeagueService.UpdateMemberRoleAsync` | Any Admin can promote Member → Admin. Owner-only demote in UI; API weaker. | Confirmed | Owner-only promote |
| P2-14 | Medium | `AddMemberAsync` | Returns `false` for both “user not found” and “already a member”. No invite token. | Confirmed | Distinct results; optional invite links |
| P2-15 | Medium | Schema | No filtered unique `(LeagueId, UserId) WHERE UserId IS NOT NULL`. Duplicate registered memberships possible under races. | Likely | Filtered unique index |
| P2-16 | Medium | `RemoveMemberAsync` | Hard delete; `GameParticipant` is Restrict → fails after any games; generic exception in UI. No confirm dialog. | Confirmed | Soft-deactivate; clear error; confirm |
| P2-17 | Low | `League.OwnerId` | No FK to AspNetUsers; dual source of truth with `LeagueMembership.Role == Owner`. | Confirmed | FK + single owner invariant |
| P2-18 | Low | Pages vs services | Mutating APIs check `IsAdminOrOwnerAsync`. Pages are not `[Authorize]`-gated; mix of `AuthorizeView` and service checks. | Confirmed | Fine for Blazor Server if userId always passed; policies if an API is added |

---

## P2 — UX / docs drift

| ID | Severity | Location | Finding | Status |
|----|----------|----------|---------|--------|
| P2-19 | — | README vs `CreateGame.razor` | README/GAME_MANAGEMENT doc: 4-step wizard + automatic bid gold. Actual: **5 steps**, town pool, separate gold calculator, dead `RecalculateGold` / `BidAmount`. | **Done** — docs match the 5-step wizard; dead bid gold removed |
| P2-20 | — | README | “Invite players”, “game editing in progress”, “leaderboards”, “color-adjusted WR” overstated. | **Done** — README describes add-member, no color-adjusted WR; editing and leaderboards are planned |
| P2-21 | — | `AUTHENTICATION.md` | Still describes `Login.razor` / `Register.razor`; actual are `.cshtml` Razor Pages. | **Done** |
| P2-22 | — | `Towns.razor` | Town detail `href` missing leading `/`. | **Done** |
| P2-23 | — | `NavMenu.razor` | Brand still “ScoreBurrow.Web”; leftover `SurveyPrompt.razor`. No heroes index. | **Done** — brand “Score Burrow”; survey removed; `/knowledgebase/heroes` |
| P2-24 | — | `ManageGame.razor` | Color as `bg-info` text, not `PlayerColorBadge`. No rating-delta after complete. | **Done** — badge on manage; rating change on manage and game details |
| P2-25 | — | Create/manage game when anonymous | Permission error, no login CTA (unlike league create). | **Done** |
| P2-26 | — | Home vs `/leagues` | Near-duplicate browse; no “My leagues”; archived leagues lack list badge. | **Done** — home is recent + my leagues; directory shows Archived |
| P2-27 | — | Town pool | Wizard-only; not persisted on `Game`. | **Done** — `Game.TownPoolTownIds` |
| P2-28 | — | `Game.Notes` | Shown if set; only auto-filled on TL; not editable. | **Done** — optional on create, editable on manage |

---

## P3 — architecture / ops / hosting

| ID | Severity | Location | Finding | Status | Suggested fix |
|----|----------|----------|---------|--------|----------------|
| P3-1 | Medium | `Program.cs` ~75–88 | `Database.Migrate()` on startup; exceptions logged, app continues. Cold start pays JIT + migrate. | Confirmed | Migrate in deploy; fail fast in prod |
| P3-2 | Low | `Program.cs` | No explicit `AddMemoryCache()`. Pages/services inject `IMemoryCache`. **Likely registered transitively by Razor Pages** — not treated as an app-breaking P0. | Unverified at runtime | Call `AddMemoryCache()` explicitly |
| P3-3 | Medium | Blazor Server + scoped `DbContext` | Circuit-scoped context; prerender double-init; concurrent UI events can throw “second operation on this context”. | Likely | `IDbContextFactory`; short-lived contexts |
| P3-4 | Medium | EF delete behaviors | `Game`↔participants Restrict vs Cascade conflict across configs; `DeleteLeagueAsync` must manual-cascade. | Likely | One explicit delete policy |
| P3-5 | Medium | Dual rating entry | Web `RatingService` vs DataImport `RatingCalculator` wrapping another instance. Stats duplicated in four places. | Partial — stats share `PlayerStatisticsProjector` (#19). Dual Glicko entry remains | Shared application writer |
| P3-6 | Medium | Infra | F1 `alwaysOn: false`; SQL serverless `autoPauseDelay: 60`, `useFreeLimit: true`, `freeLimitExhaustionBehavior: AutoPause`. SQL public + Azure-any firewall. No CI. No Key Vault. | Confirmed | See keep-alive section; tighten firewall; migrate in pipeline |
| P3-7 | Low | Untracked junk on disk | `zip.exe`, `src/ScoreBurrow.Web/test.zip`, `template.json`, `heroes.txt` | Confirmed | Do not commit |
| P3-8 | High (hosting) | Linux F1 | **Max 5 WebSocket connections.** Blazor Server uses one per open browser. Sixth concurrent user → HTTP 429. | Confirmed (Azure docs) | B1+ for real game nights, or Blazor WebAssembly |

### Design (not bugs unless product changes)

- Glicko winner plays **N−1** virtual matches vs each loser (`RatingService`, README). 8p winners move a lot. Changing this is a **rating reset**.
- Tech-loss = self-loss + new game with −1000 gold; original marked Completed (not Cancelled) after #16.

### What to preserve

- Pure `ScoreBurrow.Rating` library and snapshot-at-create.
- Service-layer admin checks on mutations.
- Unique `(GameId, PlayerColor)` and `(GameId, LeagueMembershipId)`; gold sign (negative = paid).
- TL restart copies lineup; campaign heroes included by `TownId`.
- `PlayerStatisticsProjector` as the single stats writer (complete, TL, import, Recalculate).
- Unregistered members + later link-to-user.

### Next PR should not make worse

1. Another complete/TL/recalc path that writes live rating from a stale snapshot without chaining.
2. Lifecycle transitions that only check `Status` in memory.
3. New `IMemoryCache` reads without invalidation (especially sliding standings).
4. A third definition of “games played” (cancelled vs in-progress vs TL).
5. Blanket hero-class `UpdateData` (Floribert/Wynona).

---

## Roadmap (priority after review)

**Shipped:** shared stats writer (#19); cache invalidation on create/complete/TL/cancel (#21); create-game validation + unique membership index (#22); `player_performance_*` + stats caches on the league token, absolute expiry (this branch).

**Now:** status CAS + transaction (P1-1/P1-2); Glicko-2 + complete/TL tests (rest of P1-4).

**Next:** full standings; edit in-progress games (no rating rewrite); finish or drop color-adjusted WR; login `returnUrl` and small UX polish.

**Later:** password reset / invites; rematch clone; rating replay UI; export; soft-deactivate members; extract application layer; then email / API / SignalR / achievements.

Do **not** start with SignalR, public API, or i18n while overlapping in-progress games can last-write-wins live ratings (P1-1).

---

## Keep-alive on Azure free tier

Current stack (`infrastructure/`):

| Resource | Setting | Free-tier trap |
|----------|---------|----------------|
| App Service Plan | SKU **F1**, Linux | Shared VM; **Always On not available** (`alwaysOn: false` is required) |
| App Service F1 quotas | **60 CPU minutes/day** (reset midnight UTC); **3 CPU minutes / 5-minute window** | Exceed → app **stopped**, HTTP **403**, until quota resets |
| Linux F1 WebSockets | **Max 5 connections** | Blazor Server = 1 socket per browser; 6th user **429** |
| Idle unload | Process unloaded after ~20 min idle | First request cold-starts .NET 8 + (today) `Database.Migrate()` |
| SQL | Serverless GP_S_Gen5, `useFreeLimit: true`, `AutoPause` | **100,000 vCore-seconds/month**, then **paused until next calendar month** |
| SQL auto-pause | `autoPauseDelay: 60` minutes | Resume adds **tens of seconds** on first query |
| SQL min capacity | 0.5 vCore while awake | 0.5 × 3600 = **1,800 vCore-sec/hour** → **~55 hours/month** if never paused |

There is **no supported trick to Always On on F1**. Portal / Bicep `alwaysOn: true` is ignored or rejected on Free/Shared.

### Why “ping every 5 minutes” fails here

A 24/7 keep-alive (UptimeRobot, GitHub Actions cron, cron-job.org, Logic App) keeps the **worker process loaded**. That burns the **60 CPU min/day** quota.

Rough math:

- Idle .NET 8 + Blazor Server even at ~4–5% of a shared core → `0.05 × 24h × 60 ≈ 72 CPU-minutes/day` → **over quota**.
- This app’s home page (`Index.razor`) injects **DbContext + IMemoryCache** and queries leagues. A ping to `/` is not cheap: it can **resume SQL** (burns monthly vCore seconds) and, after idle, pay **JIT + Migrate()**.
- Short-window quota (**3 CPU min / 5 min**) punishes cold starts: a fat wake can 403 the site for the rest of that window.
- If pings also keep a SQL connection open, auto-pause never fires. At 0.5 vCore, **100k vCore-sec lasts ~55 hours/month** (~1.8 h/day). 24/7 SQL wake exhausts the free DB in **~2 days**, then `AutoPause` makes the DB **inaccessible until next month**.

So a naive keep-alive does not just “use a free ping service.” It **turns F1 403s and SQL monthly lockout on**.

Blazor Server makes it worse: SignalR circuits hold memory/CPU while anyone has a tab open, and F1 allows **five** WebSockets.

### Tricks that *do* fit free tier

These reduce pain. They do **not** produce Always On.

1. **Session pre-warm (recommended if staying on F1)**  
   GitHub Action or cron **only before game night** (e.g. 15–20 min prior, a few pings until HTTP 200). Do not ping 24/7. Wakes app + SQL once, then humans keep it warm. Costs a slice of daily CPU and monthly vCore seconds, not the whole quota.

2. **Cheap `/health` (or `/ping`) that does not touch SQL or Blazor**  
   `MapGet("/health", () => Results.Ok("ok"));` registered **before** Blazor fallback. Pingers must hit this URL, never `/`. Still wakes the process (CPU quota), but avoids SQL resume and circuit setup. Useless as 24/7 keep-alive; useful for pre-warm and uptime checks.

3. **Stop paying cold-start tax**  
   Move `Database.Migrate()` out of `Program.cs` into `deploy-app.sh`. Fail deploy if migrate fails. Cold start should not compile schema.

4. **Leave SQL auto-pause at 60 minutes**  
   Do **not** hold a connection from a ping job. First real user after 60 min idle will wait on SQL resume; that is the price of the free offer. Pre-warm (1) can issue **one** cheap SQL query at session start if you want the wait to happen before players arrive.

5. **Watch quotas instead of guessing**  
   Portal → App Service → **App Service plan → Quotas** (CPU Day, CPU Short, bandwidth). SQL → metric **Free amount remaining**. Alert below ~10% SQL free amount.

6. **Blazor reconnect UX**  
   On F1 the process *will* unload. Custom `_blazor` reconnect UI is cheaper than keep-alive and matches reality.

### What not to do

| Idea | Why it fails on this stack |
|------|----------------------------|
| Portal Always On | Not available on F1 |
| Continuous WebJob / in-process timer | Needs Always On; burns CPU quota |
| Ping `/` or league pages | Loads Blazor + SQL; worst CPU/SQL cost |
| Application Insights availability test on `/` | Same; can also cost money |
| Keep SQL awake so “first click is fast” | Exhausts 100k vCore-sec; month-end outage |
| `WEBSITES_CONTAINER_IDLE_TIMEOUT` | Does not enable Always On; does not add CPU quota |
| Scale F1 to multiple instances | Free plan is shared/single; not a warm pool |

### Honest paid off-ramps (still cheap)

| Option | Approx. | What you get |
|--------|---------|----------------|
| **B1 Linux** Always On | ~US$12–15/mo | Real keep-alive, no 60-min CPU cap, WebSockets ≫ 5. **Best fit for Blazor Server game nights.** SQL can stay free serverless if you still allow auto-pause. |
| B1 + SQL leave free | Same + $0 SQL | App stays warm; SQL may still pause after 60 min unless you accept vCore burn |
| B1 + SQL billed serverless | App + SQL $ | Low latency always; leave `useFreeLimit: false` or `BillOverUsage` **only if** you accept charges |
| Azure Container Apps consumption | Pay per request | Scale to zero (cold starts remain); not a keep-alive |
| Blazor WASM + static host + API | Rebuild | Static UI is always “on”; API can cold-start. Large rewrite. |

### Recommendation

Stay on F1 **only if** cold starts and “max 5 people with the site open” are acceptable. Then: `/health` + **pre-warm before sessions** + migrate-on-deploy.

If league nights have more than a handful of open tabs, **F1 WebSockets are the blocker**, not idle timeout. Upgrade the App Service plan to **B1** and leave SQL on the free serverless offer with auto-pause. That is the smallest paid trick that actually matches Blazor Server.

---

## Unresolved / not reproduced in this pass

- Whether production currently 403s on CPU quota (needs Portal Quotas).
- Whether `IMemoryCache` resolves at runtime without explicit registration.
- Whether any duplicate `(LeagueId, UserId)` rows exist (needs a SQL query).
- Floribert/Wynona class: HotA wiki vs `heroes.csv` — pick a source of truth.
- Live overlapping-game rating last-write-wins — needs a two-game fixture.

Logged against `main` `f491b39` on 20 Sep 2026. Progress marked 22 Sep 2026 against `main` `4ba7171` (#19, #21, #22) plus P0-2 leftover on `f/league-cache-expiry`.
