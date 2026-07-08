# OpenShock Discord Activity

A Discord Activity (an iframe app launched from a voice channel) where users log in, link their
OpenShock account, manage shockers and whitelist, and — the headline feature — join a shared room
and shock each other in real time, governed by a consent model.

## Architecture

```
Discord client (voice channel)
        │  loads iframe (Embedded App SDK: OAuth, instanceId, participants)
        ▼
<APP_ID>.discordsays.com ── two URL mappings ──┬─ "/"    → activity.openshock.app      (Cloudflare Pages)
        (browser fetches /api/… , SignalR)     └─ "/api" → activity-api.openshock.app  (cloudflared → k8s)
                                                              │  (Discord strips the "/api" prefix)
   SvelteKit SPA ──Bearer JWT──────────────────────────────► ASP.NET BFF
                                                              ├─► Postgres (shared EF context)
                                                              └─► OpenShock SDK ─► OpenShock API
```

### Projects

| Project | What it is |
| --- | --- |
| `OpenShock.DiscordBot.Shared` | Class library: EF `OpenShockDiscordContext` + entities, migrations, `ApiUtils.GetApiClient()`, `OpenShockBackendService`. Referenced by the bot **and** the API. |
| `OpenShock.DiscordBot` | The existing Discord bot (unchanged behavior). |
| `OpenShock.Activity.Api` | New ASP.NET Core BFF: Discord OAuth token exchange, REST + SignalR over the shared DB/SDK. |
| `activity/` | SvelteKit 5 SPA (the Activity frontend). |

## Configuration (API)

Bound from the `Activity` config section. Non-secret defaults live in `appsettings.json`; secrets come
from user-secrets (local) or env vars (`Activity__Discord__ClientSecret`, etc.) in production.

| Key | Notes |
| --- | --- |
| `Activity:Discord:ClientId` | `1096380937496969326` (public). |
| `Activity:Discord:ClientSecret` | From the Discord Dev Portal → OAuth2. **Secret.** |
| `Activity:Jwt:Key` | HMAC signing key for our session JWT (≥ 32 bytes). **Secret.** |
| `Activity:Db:Conn` | Postgres connection string (same DB as the bot). **Secret.** |
| `Activity:CorsOrigins` | Dev only — allowed browser origins for `pnpm dev`. |

## Discord Developer Portal

1. **Activities → Enable** the embedded app.
2. **OAuth2**: scope `identify`. Generate a **client secret** → `Activity:Discord:ClientSecret`.
3. **Activities → URL Mappings** (two):
   - `/` → `activity.openshock.app` (frontend)
   - `/api` → `activity-api.openshock.app` (API)

   Discord **strips the mapping prefix**, so `/api/me` in the browser reaches the API as `/me`. The API
   deliberately serves its routes at root; the `/api` prefix is browser-side only.

## Hosting

- **Frontend:** Cloudflare Pages project (build `activity/`, adapter-cloudflare) on `activity.openshock.app`.
- **Backend:** `OpenShock.Activity.Api` in Kubernetes, exposed to Cloudflare via **cloudflared** on
  `activity-api.openshock.app`. See [`deploy/cloudflared-activity-api.yaml`](../deploy/cloudflared-activity-api.yaml)
  and [`OpenShock.Activity.Api/Dockerfile`](../OpenShock.Activity.Api/Dockerfile). WebSockets (SignalR)
  work through both the Discord proxy and cloudflared natively.

Because every browser request is same-origin (`discordsays.com`), there is **no CORS in production** and
sessions use a **Bearer JWT** (not cookies, which are unreliable in the proxied iframe).

## Database migration

The consent columns (`allow_room_shocks`, `room_max_intensity`, `room_max_duration_ms`) ship as the
`AddRoomConsent` EF migration in the Shared project. Apply it:

```bash
dotnet ef database update --project OpenShock.DiscordBot.Shared --startup-project MigrationHelper
```

(The bot also auto-applies pending migrations on startup unless `bot:Db:SkipMigration` is set.)

## Consent model

- **Whitelist** (persistent): you may shock someone only if they have whitelisted you — the existing
  bot semantics (`UsersFriendwhitelist`).
- **Room consent** (session, opt-in): if a user enables "allow room participants to shock me", anyone
  in the *same Discord Activity instance* may shock them **without** a whitelist entry, clamped to that
  user's `RoomMaxIntensity` / `RoomMaxDurationMs`.

The `/control` endpoint enforces: self → allow; whitelisted → allow; else room-consent + same instance
(clamped) → allow; otherwise `403`.

## Local development

```bash
# 1. API (needs Postgres reachable at Activity:Db:Conn with the migration applied)
dotnet run --project OpenShock.Activity.Api          # http://localhost:5199 (set ASPNETCORE_URLS)

# 2. Frontend
cd activity && pnpm install && pnpm dev              # http://localhost:5173
```

The Discord Embedded App SDK only works **inside Discord**. To test the real flow, point the two URL
mappings at a tunnel (e.g. `cloudflared tunnel --url`) to your local frontend/API, then launch the
Activity from a Discord voice channel.

For API-only testing without Discord, the Development environment exposes `POST /dev/token
{ "discordId": "…", "name": "…" }` which mints a session JWT you can use as a Bearer token against the
REST endpoints.

## Endpoints (served at root; browser calls them under `/api`)

| Method | Path | Purpose |
| --- | --- | --- |
| POST | `/auth/token` | Exchange a Discord OAuth code for a session JWT. |
| GET | `/me` | Link + consent status. |
| POST/DELETE | `/link` | Link / unlink an OpenShock account. |
| GET/PUT | `/shockers` | List / choose enabled shockers. |
| GET | `/whitelist`, POST/DELETE `/whitelist/{id}` | Manage who may shock you. |
| GET/PUT | `/consent` | Room-consent toggle + caps. |
| POST | `/control` | Shock/vibrate/sound a target (permission-checked). |
| WS | `/hubs/room?instanceId=…` | SignalR room: roster, join/leave, consent, live shock feed. |
