# End-to-End Flows: Development (HMR) & Production

## Development Flow with HMR

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DEVELOPMENT FLOW                                   │
│                              (dotnet run)                                    │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: BUILD                                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Developer Code                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ // Program.cs                                                          │ │
│  │ app.MapGet("/residents/{id}", Handler.Detail)                          │ │
│  │    .Impulse<ResidentDetailProps>();                                    │ │
│  │                                                                        │ │
│  │ // CreateResidentValidator.cs                                          │ │
│  │ RuleFor(x => x.Name).NotEmpty().MaxLength(100);                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                              │                                               │
│                              ▼                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    ROSLYN COMPILER + SOURCE GENERATOR                  │ │
│  │                         (runs at compile time)                         │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                              │                                               │
│          ┌───────────────────┼───────────────────┐                          │
│          ▼                   ▼                   ▼                          │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────────────┐            │
│   │ Routes.g.cs │    │Handlers.g.cs│    │  TypeScript.g.cs    │            │
│   │             │    │             │    │  /* IMPULSE:...*/   │            │
│   │ const List  │    │ MapEndpoints│    │  types.ts           │            │
│   │ const Detail│    │ extension   │    │  routes.tsx         │            │
│   │ DetailPath()│    │             │    │  validation.ts      │            │
│   └─────────────┘    └─────────────┘    │  forms.ts           │            │
│                                         └─────────────────────┘            │
│                                                  │                          │
│                                                  ▼                          │
│                              ┌────────────────────────────────────┐         │
│                              │      MSBUILD EXTRACT TASK          │         │
│                              │   (AfterBuild, regex extraction)   │         │
│                              └────────────────────────────────────┘         │
│                                                  │                          │
│                    ┌─────────────┬───────────────┼───────────────┐          │
│                    ▼             ▼               ▼               ▼          │
│            ┌───────────┐ ┌───────────┐ ┌─────────────┐ ┌───────────┐       │
│            │ types.ts  │ │routes.tsx │ │validation.ts│ │ forms.ts  │       │
│            └───────────┘ └───────────┘ └─────────────┘ └───────────┘       │
│                    │             │               │               │          │
│                    └─────────────┴───────────────┴───────────────┘          │
│                                         │                                    │
│                              ClientApp/generated/                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: DEV SERVERS START                                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────┐      ┌─────────────────────────────┐       │
│  │      KESTREL (.NET)         │      │      VITE (Bun)             │       │
│  │      localhost:5000         │      │      localhost:5173         │       │
│  │                             │      │                             │       │
│  │  • Serves API endpoints     │      │  • Serves React app         │       │
│  │  • /api/residents/*         │      │  • HMR WebSocket            │       │
│  │  • Returns JSON             │      │  • Watches *.tsx files      │       │
│  │                             │      │  • Watches generated/       │       │
│  └─────────────────────────────┘      └─────────────────────────────┘       │
│              │                                    │                          │
│              │         ┌──────────────────────────┘                          │
│              │         │                                                     │
│              ▼         ▼                                                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                         BROWSER                                         ││
│  │                      localhost:5173                                     ││
│  │                                                                         ││
│  │   React App ◄───── HMR WebSocket ────► Vite                            ││
│  │       │                                                                 ││
│  │       │ fetch('/api/residents/1')                                       ││
│  │       │                                                                 ││
│  │       └────────► Vite Proxy ────────► Kestrel ────────► JSON           ││
│  │                                                                         ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: HOT MODULE REPLACEMENT                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SCENARIO A: Edit React Component (.tsx)                                     │
│  ────────────────────────────────────────                                    │
│                                                                              │
│  Features/Residents/Detail.tsx                                               │
│         │ (save)                                                             │
│         ▼                                                                    │
│  ┌─────────────┐    WebSocket     ┌─────────────┐                           │
│  │    Vite     │ ───────────────► │   Browser   │                           │
│  │  (watches)  │   "hmr update"   │   (React)   │                           │
│  └─────────────┘                  └─────────────┘                           │
│                                          │                                   │
│                                          ▼                                   │
│                                   Component re-renders                       │
│                                   State preserved ✓                          │
│                                   ~50ms                                      │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  SCENARIO B: Edit C# Handler/Model                                           │
│  ─────────────────────────────────                                           │
│                                                                              │
│  ResidentsHandler.cs                                                         │
│         │ (save)                                                             │
│         ▼                                                                    │
│  ┌─────────────┐                  ┌─────────────┐                           │
│  │dotnet watch │                  │   Roslyn    │                           │
│  │  (detects)  │ ───────────────► │  recompile  │                           │
│  └─────────────┘                  └─────────────┘                           │
│                                          │                                   │
│                                          ▼                                   │
│                                   Source Generator runs                      │
│                                   TypeScript.g.cs updated                    │
│                                          │                                   │
│                                          ▼                                   │
│                                   MSBuild Extract runs                       │
│                                   generated/*.ts updated                     │
│                                          │                                   │
│                                          ▼                                   │
│  ┌─────────────┐    WebSocket     ┌─────────────┐                           │
│  │    Vite     │ ───────────────► │   Browser   │                           │
│  │  (watches)  │  "hmr update"    │   (React)   │                           │
│  └─────────────┘                  └─────────────┘                           │
│                                          │                                   │
│                                          ▼                                   │
│                                   Types/routes updated                       │
│                                   ~2s (full recompile)                       │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  SCENARIO C: Edit FluentValidation Rule                                      │
│  ──────────────────────────────────────                                      │
│                                                                              │
│  CreateResidentValidator.cs                                                  │
│  RuleFor(x => x.Name).MaxLength(100) → MaxLength(200)                       │
│         │ (save)                                                             │
│         ▼                                                                    │
│  Roslyn recompile → Source Gen → validation.ts updated                      │
│         │                                                                    │
│         ▼                                                                    │
│  z.string().max(100) → z.string().max(200)                                  │
│         │                                                                    │
│         ▼                                                                    │
│  Vite HMR → Browser updates → Client validation matches server              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘


================================================================================


┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRODUCTION FLOW                                    │
│                            (dotnet publish)                                  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: BUILD & PUBLISH                                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  dotnet publish -c Release                                                   │
│         │                                                                    │
│         ▼                                                                    │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    ROSLYN COMPILER + SOURCE GENERATOR                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│         │                                                                    │
│         ├──► Routes.g.cs                                                     │
│         ├──► Handlers.g.cs                                                   │
│         └──► TypeScript.g.cs                                                 │
│                    │                                                         │
│                    ▼                                                         │
│         ┌────────────────────────────────────────────────────────────────┐  │
│         │              MSBUILD EXTRACT TASK                              │  │
│         └────────────────────────────────────────────────────────────────┘  │
│                    │                                                         │
│                    ▼                                                         │
│         ClientApp/generated/                                                 │
│         ├── types.ts                                                         │
│         ├── routes.tsx                                                       │
│         ├── validation.ts                                                    │
│         └── forms.ts                                                         │
│                    │                                                         │
│                    ▼                                                         │
│         ┌────────────────────────────────────────────────────────────────┐  │
│         │                    BUN RUN BUILD                               │  │
│         │              (Vite production build)                           │  │
│         └────────────────────────────────────────────────────────────────┘  │
│                    │                                                         │
│                    ▼                                                         │
│         ClientApp/dist/                                                      │
│         ├── assets/                                                          │
│         │   ├── index-a1b2c3d4.js      (hashed)                             │
│         │   ├── index-e5f6g7h8.css     (hashed)                             │
│         │   └── chunk-vendor-xyz.js    (code split)                         │
│         └── .vite/                                                           │
│             └── manifest.json                                                │
│                    │                                                         │
│                    ▼                                                         │
│         ┌────────────────────────────────────────────────────────────────┐  │
│         │              MSBUILD COPY TO WWWROOT                           │  │
│         └────────────────────────────────────────────────────────────────┘  │
│                    │                                                         │
│                    ▼                                                         │
│         publish/                                                             │
│         ├── MyApp.dll                                                        │
│         ├── MyApp.deps.json                                                  │
│         ├── appsettings.json                                                 │
│         └── wwwroot/                                                         │
│             └── assets/                                                      │
│                 ├── index-a1b2c3d4.js                                        │
│                 ├── index-e5f6g7h8.css                                       │
│                 ├── chunk-vendor-xyz.js                                      │
│                 └── .vite/                                                   │
│                     └── manifest.json                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: PRODUCTION RUNTIME                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                         KESTREL (.NET)                                  ││
│  │                        (single process)                                 ││
│  │                                                                         ││
│  │  ┌───────────────────────────────────────────────────────────────────┐ ││
│  │  │                    REQUEST PIPELINE                               │ ││
│  │  │                                                                   │ ││
│  │  │  Request ──► UseStaticFiles ──► UseImpulseSpaFallback ──► API    │ ││
│  │  │                    │                     │                        │ ││
│  │  │                    ▼                     ▼                        │ ││
│  │  │            /assets/*.js           / or /residents/*              │ ││
│  │  │            /assets/*.css          (no extension)                  │ ││
│  │  │                    │                     │                        │ ││
│  │  │                    ▼                     ▼                        │ ││
│  │  │            wwwroot/assets/        Serve index.html                │ ││
│  │  │            (immutable cache)      (with hashed paths)             │ ││
│  │  │                                                                   │ ││
│  │  └───────────────────────────────────────────────────────────────────┘ ││
│  │                                                                         ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: BROWSER REQUEST FLOW                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Browser: GET /residents/123                                                 │
│         │                                                                    │
│         ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Kestrel: No static file, no extension → SPA fallback                    ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│         │                                                                    │
│         ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ AssetManifest reads .vite/manifest.json                                 ││
│  │ Returns HTML with hashed asset paths:                                   ││
│  │                                                                         ││
│  │ <!DOCTYPE html>                                                         ││
│  │ <html>                                                                  ││
│  │   <head>                                                                ││
│  │     <link rel="stylesheet" href="/assets/index-e5f6g7h8.css">          ││
│  │   </head>                                                               ││
│  │   <body>                                                                ││
│  │     <div id="root"></div>                                               ││
│  │     <script type="module" src="/assets/index-a1b2c3d4.js"></script>    ││
│  │   </body>                                                               ││
│  │ </html>                                                                 ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│         │                                                                    │
│         ▼                                                                    │
│  Browser: GET /assets/index-a1b2c3d4.js                                     │
│         │                                                                    │
│         ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ Kestrel: UseStaticFiles                                                 ││
│  │ Response Headers:                                                       ││
│  │   Cache-Control: public, max-age=31536000, immutable                    ││
│  │   (cached forever - filename changes on content change)                 ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│         │                                                                    │
│         ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ React App boots:                                                        ││
│  │   1. TanStack Router matches /residents/123                             ││
│  │   2. Route loader: fetch('/api/residents/123')                          ││
│  │   3. Kestrel API returns JSON                                           ││
│  │   4. Component renders with data                                        ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘


================================================================================


┌─────────────────────────────────────────────────────────────────────────────┐
│                      COMPARISON: DEV vs PROD                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                    DEVELOPMENT                    PRODUCTION                 │
│                    ───────────                    ──────────                 │
│                                                                              │
│  Servers:          2 (Kestrel + Vite)            1 (Kestrel only)           │
│                                                                              │
│  Assets:           Served by Vite                Served by Kestrel          │
│                    (unbundled, fast HMR)         (bundled, hashed)          │
│                                                                              │
│  JS Files:         Individual modules            Single bundle + chunks     │
│                    (no minification)             (minified, tree-shaken)    │
│                                                                              │
│  CSS:              Individual files              Single file (hashed)       │
│                    (HMR updates)                 (immutable cache)          │
│                                                                              │
│  Source Maps:      Full (for debugging)          Optional (smaller)         │
│                                                                              │
│  API Calls:        Vite proxy → Kestrel          Direct to Kestrel          │
│                                                                              │
│  Caching:          No cache (always fresh)       Aggressive (immutable)     │
│                                                                              │
│  TypeScript:       tsgo (fast, dev mode)         tsgo (production build)    │
│                                                                              │
│  Startup:          ~2s (both servers)            ~200ms (Kestrel only)      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```
