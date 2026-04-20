---
title: "Building the N-Queens Game with ASP.NET Core and Angular"
subtitle: "A step-by-step learning guide"
---

# Building the N-Queens Game with ASP.NET Core and Angular

**A step-by-step learning guide**  
This document walks through how this repository’s N-Queens application was built: backend API in **.NET 8**, frontend in **Angular 19**, and how they work together. Follow the steps in order to reproduce the project and understand each layer.

---

## What you will build

- A **REST-style JSON API** that starts a game, validates queen placement (no two queens may attack each other), and reports when the puzzle is solved.
- An **Angular** single-page app that draws the board, talks to the API, and shows errors and progress.
- Optional **single-host** deployment: the API serves the compiled Angular files from `wwwroot`.

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), [Node.js LTS](https://nodejs.org/) (this project uses Angular 19), and a code editor (Visual Studio, VS Code, or Rider).

---

## Part 1 — Create the solution and Web API

### Step 1.1: Create a folder and solution

Open a terminal in an empty folder (or your repo root) and run:

```bash
dotnet new sln -n NQueens
dotnet new webapi -n NQueens.Api -o src/NQueens.Api
dotnet sln add src/NQueens.Api/NQueens.Api.csproj
```

**Why:** A `.sln` groups projects. `webapi` gives you Kestrel, middleware, and a starting `Program.cs`.

### Step 1.2: Understand the default template

Open `src/NQueens.Api/Program.cs`. The template registers Swagger and maps sample endpoints. You will replace sample routes with game routes.

**Why:** ASP.NET Core 8 often uses *minimal APIs*: `app.MapGet`, `app.MapPost`, etc., with small lambdas or method groups instead of large `Controllers` classes. Controllers are still valid; this project uses minimal APIs for clarity.

### Step 1.3: Model the JSON contract

Create `Models/GameDtos.cs` with records that match what the client will send and receive, for example:

- `StartGameRequest` with `N` (board size).
- `ToggleCellRequest` with `Row` and `Col`.
- `GameStateResponse` with `Id`, `N`, `Grid` (jagged `int[][]`: 0 = empty, 1 = queen), `QueensPlaced`, `Solved`, and optional `SolutionCount`.

**Why:** Records are immutable-friendly DTOs. The API serializes property names; you will configure **camelCase** JSON so Angular receives `n`, `grid`, etc.

### Step 1.4: Implement game rules in a service class

Create `Services/NQueensGameSession.cs`:

- Store an `N × N` grid and a queen count.
- **Toggle:** If the cell has a queen, remove it. If empty, allow placement only if no existing queen attacks that square (same row, column, or diagonal).
- Expose `GetState()` returning a `GameStateResponse` (copy the grid so callers cannot mutate internal state).

**Why:** Keeping rules in a dedicated class makes `Program.cs` thin and testable.

### Step 1.5: Count all solutions (optional but educational)

Create `Services/NQueensSolver.cs` with a classic **bitmask backtracking** count for n ≤ 14.

**Why:** The game only needs “is this placement valid,” but reporting *how many* complete solutions exist for a given `n` connects the UI to classic CS algorithms.

### Step 1.6: Store sessions in memory

Create `GameSessionStore`:

- `Start(n)` validates `n` (e.g. 4–14), creates `Guid`, stores `NQueensGameSession`, returns it.
- `TryGet(id, out session)` for later requests.
- `Remove(id)` if you want explicit cleanup.

Register it in `Program.cs`:

```csharp
builder.Services.AddSingleton<GameSessionStore>();
```

**Why:** `Singleton` matches an in-memory dictionary. For production you might use Redis or a database; the learning goal here is HTTP + state shape.

### Step 1.7: Configure JSON and map HTTP routes

In `Program.cs`:

1. Call `ConfigureHttpJsonOptions` to use `JsonNamingPolicy.CamelCase` and ignore nulls when writing JSON.
2. Map a group: `var games = app.MapGroup("/api/games");`
3. **`POST /api/games`** — body `{ "n": 8 }`, call `store.Start`, return `Ok(session.GetState())`, catch bad `n` with `BadRequest`.
4. **`GET /api/games/{id}`** — return 404 if missing.
5. **`POST /api/games/{id}/toggle`** — body `{ "row": 0, "col": 0 }`, call `session.Toggle`, map argument and invalid-move exceptions to `BadRequest`.
6. **`DELETE /api/games/{id}`** — optional.

**Why:** Route grouping keeps URLs consistent. Typed results (`TypedResults.Ok`, etc.) give clear OpenAPI/Swagger output.

### Step 1.8: Enable CORS for the Angular dev server

Angular’s dev server runs on another origin (e.g. `http://localhost:4200`). Browsers block cross-origin API calls unless the API sends CORS headers.

In `Program.cs`, add `AddCors` with `WithOrigins(...)` matching your Angular URL. In `appsettings.json`, store origins under a `Cors` section and read them in code.

**Order matters:** call `app.UseCors()` before endpoints that the browser will call from Angular.

**Why:** Without CORS, `HttpClient` from `ng serve` will fail with network/CORS errors even if the API works from curl.

### Step 1.9: Run and try the API

```bash
dotnet run --project src/NQueens.Api
```

Use Swagger UI (Development environment) or `curl`/REST Client to `POST /api/games` and `POST /api/games/{id}/toggle`.

**Why:** Verifying the API before UI work isolates bugs.

---

## Part 2 — Create the Angular application

### Step 2.1: Scaffold the client

From the repo root (or a `src` folder):

```bash
npx @angular/cli new nqueens-client --directory=src/NQueens.Client --routing --style=css --ssr=false
```

Answer prompts or pass `--defaults` for a non-interactive setup.

**Why:** The CLI sets up TypeScript, build pipeline, and a root `AppComponent`.

### Step 2.2: Provide HttpClient

In `app.config.ts`, add:

```typescript
import { provideHttpClient } from '@angular/common/http';
// ...
providers: [/* ... */, provideHttpClient()]
```

**Why:** Angular 19 uses standalone bootstrap; `provideHttpClient` registers the HTTP stack for injection.

### Step 2.3: Proxy API calls during development

Add `proxy.conf.json` at the Angular project root:

```json
{
  "/api": {
    "target": "http://localhost:5043",
    "secure": false
  }
}
```

In `angular.json`, under `serve.options`, set `"proxyConfig": "proxy.conf.json"`.

**Why:** The browser calls `http://localhost:4200/api/...`; the dev server forwards to the API port so you avoid CORS during local development (the browser sees same origin for `/api`).

### Step 2.4: Create a small API service

Add `game.service.ts` with `HttpClient` injected (`inject(HttpClient)`):

- `startGame(n: number)` → `POST /api/games`, body `{ n }`.
- `toggleCell(id, row, col)` → `POST /api/games/${id}/toggle`, body `{ row, col }`.

Map `HttpErrorResponse` to a user-readable message (read `error.message` from the API’s `{ "message": "..." }` body).

**Why:** Components stay focused on UI; all HTTP details live in one service.

### Step 2.5: Build the board UI

In `app.component.ts` / template:

- Signals or fields for `game`, `loading`, `error`, and selected `n`.
- **Start** button calls `startGame`, stores returned state (including `id` and `grid`).
- Render an `n × n` grid with `@for` loops; each cell is a `<button>` that calls `toggleCell` with row/column indices.
- Show queen glyph or styling when `grid[row][col] === 1`.
- Display `queensPlaced`, `solved`, and optional `solutionCount`.

**Why:** Buttons are keyboard-accessible; nested `@for` matches the jagged `grid` shape from the API.

### Step 2.6: Run client and server together

Terminal 1:

```bash
dotnet run --project src/NQueens.Api
```

Terminal 2:

```bash
cd src/NQueens.Client && npm install && npm start
```

Open the URL the CLI prints (usually `http://localhost:4200`).

**Why:** Hot reload on the client while the API restarts separately is the standard full-stack dev loop.

### Step 2.7: Tests (optional)

Update `app.component.spec.ts` to provide `provideHttpClient()` and `provideHttpClientTesting()` so the component can be created without a real network.

Run:

```bash
cd src/NQueens.Client && npx ng test --watch=false --browsers=ChromeHeadless
```

**Why:** Even one smoke test catches broken imports after refactors.

---

## Part 3 — Serve the Angular build from ASP.NET Core

### Step 3.1: Add static files and SPA fallback

In `Program.cs`, after routing setup for the API:

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
// ... API routes ...
app.MapFallbackToFile("index.html");
```

Ensure `wwwroot` exists (Angular build output will go there).

**Why:** `MapFallbackToFile` returns `index.html` for non-file routes so Angular routing (if you add more routes later) still works.

### Step 3.2: Automate Angular build on Release

In `NQueens.Api.csproj`, add an MSBuild target that:

1. Runs `npm ci` in the Angular project folder.
2. Runs `npm run build -- --configuration production`.
3. Copies `dist/nqueens-client/browser/**` into `wwwroot`.

Run this target only for **Release** (or when a property like `RunAngularBuild=true`) so **Debug** builds stay fast.

**Why:** CI and `dotnet publish -c Release` produce a single deployable folder: API + static UI.

### Step 3.3: Production configuration notes

- Set `Cors:AllowedOrigins` to your real front-end URL if the UI is hosted separately.
- If API and UI share one host, CORS is less critical for that deployment.
- Use HTTPS in production; configure certificates or a reverse proxy (nginx, Azure App Service, etc.).

---

## Part 4 — Suggested learning exercises

1. **Add a “Reset board”** endpoint or client button that clears queens without changing `n`.
2. **Highlight attacked squares** on hover using only client-side logic (derive from current queens).
3. **Persist games** with EF Core + SQLite instead of the in-memory dictionary.
4. **Add Angular routes**: a home page and a `/play` route that lazy-loads the board component.

---

## Quick reference — project layout

| Path | Role |
|------|------|
| `NQueens.sln` | Solution file |
| `src/NQueens.Api/Program.cs` | Middleware, CORS, routes, SPA fallback |
| `src/NQueens.Api/Models/` | JSON DTOs |
| `src/NQueens.Api/Services/` | Game session + solution counter |
| `src/NQueens.Api/wwwroot/` | Angular production output (after Release build) |
| `src/NQueens.Client/` | Angular app, `proxy.conf.json`, `game.service.ts` |

---

## Appendix — Generate this PDF yourself

If you edit the Markdown source `docs/NQueens-Build-Guide.md`, you can rebuild the PDF with [Pandoc](https://pandoc.org/):

```bash
pandoc docs/NQueens-Build-Guide.md -o docs/NQueens-Build-Guide.pdf --pdf-engine=wkhtmltopdf -V margin-top=20 -V margin-bottom=20 -V margin-left=25 -V margin-right=25
```

---

*End of guide.*
