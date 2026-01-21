using System.CommandLine;
using Shalimar.Razor;

var rootCommand = new RootCommand("Shalimar - Compile Razor components to React TSX");

// Build command
var buildCommand = new Command("build", "Compile .razor files to .tsx");
var sourceOption = new Option<string>("--source", "Source directory containing .razor files") { IsRequired = true };
sourceOption.AddAlias("-s");
var manifestOption = new Option<string?>("--manifest", "Output path for component manifest");
manifestOption.AddAlias("-m");
var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
verboseOption.AddAlias("-v");

buildCommand.AddOption(sourceOption);
buildCommand.AddOption(manifestOption);
buildCommand.AddOption(verboseOption);

buildCommand.SetHandler((source, manifest, verbose) =>
{
    Console.WriteLine($"[Shalimar] Compiling Razor files in: {source}");

    var compiler = new ShalimarCompiler(new ShalimarOptions { Verbose = verbose });
    var summary = compiler.CompileDirectory(source);

    Console.WriteLine();
    Console.WriteLine($"[Shalimar] Compilation complete: {summary.SuccessCount} succeeded, {summary.FailureCount} failed");

    if (!string.IsNullOrEmpty(manifest))
    {
        compiler.GenerateComponentManifest(source, manifest);
    }

}, sourceOption, manifestOption, verboseOption);

// Watch command
var watchCommand = new Command("watch", "Watch .razor files and recompile on change");
var watchSourceOption = new Option<string>("--source", "Source directory to watch") { IsRequired = true };
watchSourceOption.AddAlias("-s");

watchCommand.AddOption(watchSourceOption);

watchCommand.SetHandler((source) =>
{
    Console.WriteLine($"[Shalimar] Watching for changes in: {source}");
    Console.WriteLine("[Shalimar] Press Ctrl+C to stop");

    // Initial build
    var compiler = new ShalimarCompiler();
    compiler.CompileDirectory(source);

    // Watch for changes
    using var watcher = compiler.Watch(source);

    // Keep running until cancelled
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    try
    {
        Task.Delay(-1, cts.Token).Wait();
    }
    catch (AggregateException)
    {
        Console.WriteLine("\n[Shalimar] Stopped watching");
    }

}, watchSourceOption);

// Init command
var initCommand = new Command("init", "Initialize a new Shalimar project");
var initPathOption = new Option<string>("--path", () => ".", "Path to initialize project");
initPathOption.AddAlias("-p");

initCommand.AddOption(initPathOption);

initCommand.SetHandler((path) =>
{
    Console.WriteLine($"[Shalimar] Initializing project in: {path}");

    // Create directories
    var featuresDir = Path.Combine(path, "Features");
    Directory.CreateDirectory(featuresDir);
    Directory.CreateDirectory(Path.Combine(path, "wwwroot"));

    // Create package.json
    var packageJson = """
    {
      "name": "shalimar-app",
      "private": true,
      "version": "0.0.0",
      "type": "module",
      "scripts": {
        "dev": "vite",
        "build": "vite build",
        "preview": "vite preview"
      },
      "dependencies": {
        "react": "^18.2.0",
        "react-dom": "^18.2.0",
        "@tanstack/react-router": "^1.20.0"
      },
      "devDependencies": {
        "@types/react": "^18.2.0",
        "@types/react-dom": "^18.2.0",
        "@vitejs/plugin-react": "^4.2.0",
        "typescript": "^5.3.0",
        "vite": "^5.0.0"
      }
    }
    """;
    File.WriteAllText(Path.Combine(path, "package.json"), packageJson);

    // Create vite.config.ts
    var viteConfig = """
    import { defineConfig } from 'vite';
    import react from '@vitejs/plugin-react';
    import { resolve } from 'path';
    import { readdirSync, statSync } from 'fs';

    // Auto-discover entry points from Features folders
    function getFeatureEntries(featuresDir: string): Record<string, string> {
      const entries: Record<string, string> = {};

      try {
        const features = readdirSync(featuresDir);
        for (const feature of features) {
          const featurePath = resolve(featuresDir, feature);
          if (statSync(featurePath).isDirectory()) {
            const files = readdirSync(featurePath);
            for (const file of files) {
              if (file.endsWith('.tsx')) {
                const name = file.replace('.tsx', '');
                entries[`${feature}/${name}`] = resolve(featurePath, file);
              }
            }
          }
        }
      } catch (e) {
        // Features directory doesn't exist yet
      }

      return entries;
    }

    export default defineConfig({
      plugins: [react()],
      build: {
        outDir: 'wwwroot/dist',
        manifest: true,
        rollupOptions: {
          input: {
            main: resolve(__dirname, 'index.html'),
            ...getFeatureEntries(resolve(__dirname, 'Features'))
          }
        }
      },
      server: {
        proxy: {
          '/api': 'http://localhost:5000'
        }
      }
    });
    """;
    File.WriteAllText(Path.Combine(path, "vite.config.ts"), viteConfig);

    // Create tsconfig.json
    var tsConfig = """
    {
      "compilerOptions": {
        "target": "ES2020",
        "useDefineForClassFields": true,
        "lib": ["ES2020", "DOM", "DOM.Iterable"],
        "module": "ESNext",
        "skipLibCheck": true,
        "moduleResolution": "bundler",
        "allowImportingTsExtensions": true,
        "resolveJsonModule": true,
        "isolatedModules": true,
        "noEmit": true,
        "jsx": "react-jsx",
        "strict": true,
        "noUnusedLocals": true,
        "noUnusedParameters": true,
        "noFallthroughCasesInSwitch": true
      },
      "include": ["Features/**/*", "*.ts", "*.tsx"]
    }
    """;
    File.WriteAllText(Path.Combine(path, "tsconfig.json"), tsConfig);

    // Create index.html
    var indexHtml = """
    <!DOCTYPE html>
    <html lang="en">
      <head>
        <meta charset="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>Shalimar App</title>
      </head>
      <body>
        <div id="root"></div>
        <script type="module" src="/main.tsx"></script>
      </body>
    </html>
    """;
    File.WriteAllText(Path.Combine(path, "index.html"), indexHtml);

    // Create main.tsx entry point
    var mainTsx = """
    import React from 'react';
    import { createRoot } from 'react-dom/client';
    import { RouterProvider, createRouter, createRootRoute, createRoute } from '@tanstack/react-router';

    // Root route
    const rootRoute = createRootRoute();

    // Define routes - these will be auto-generated from Features
    const indexRoute = createRoute({
      getParentRoute: () => rootRoute,
      path: '/',
      component: () => <div>Welcome to Shalimar! Add components to Features/ folder.</div>
    });

    const routeTree = rootRoute.addChildren([indexRoute]);
    const router = createRouter({ routeTree });

    // Render
    const root = createRoot(document.getElementById('root')!);
    root.render(
      <React.StrictMode>
        <RouterProvider router={router} />
      </React.StrictMode>
    );
    """;
    File.WriteAllText(Path.Combine(path, "main.tsx"), mainTsx);

    // Create sample feature
    var usersDir = Path.Combine(featuresDir, "Users");
    Directory.CreateDirectory(usersDir);

    var userProfileRazor = """
    @server

    <div className="user-profile">
      <h2>@Props.Name</h2>
      <p>@Props.Email</p>
      @if (Props.IsActive)
      {
        <span className="badge active">Active</span>
      }
    </div>

    @code {
      [Parameter] public string Name { get; set; }
      [Parameter] public string Email { get; set; }
      [Parameter] public bool IsActive { get; set; }
    }
    """;
    File.WriteAllText(Path.Combine(usersDir, "UserProfile.razor"), userProfileRazor);

    Console.WriteLine("[Shalimar] Created:");
    Console.WriteLine("  - package.json");
    Console.WriteLine("  - vite.config.ts");
    Console.WriteLine("  - tsconfig.json");
    Console.WriteLine("  - index.html");
    Console.WriteLine("  - main.tsx");
    Console.WriteLine("  - Features/Users/UserProfile.razor");
    Console.WriteLine();
    Console.WriteLine("Next steps:");
    Console.WriteLine("  1. npm install");
    Console.WriteLine("  2. dotnet shalimar build -s ./Features");
    Console.WriteLine("  3. npm run dev");

}, initPathOption);

rootCommand.AddCommand(buildCommand);
rootCommand.AddCommand(watchCommand);
rootCommand.AddCommand(initCommand);

return await rootCommand.InvokeAsync(args);
