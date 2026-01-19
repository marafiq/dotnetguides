using System.Text.Json;
using Shalimar.Razor;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Sample data
var currentUser = new
{
    Name = "Alex Johnson",
    Email = "alex@example.com",
    AvatarUrl = "/avatar.png",
    Initials = "AJ",
    IsVerified = true,
    ProjectCount = 47,
    FollowerCount = "2.3k",
    Rating = "4.9"
};

var activities = new[]
{
    new { Type = "commit", Message = "Fixed auth bug", Time = "2m ago" },
    new { Type = "pr", Message = "Merged feature branch", Time = "1h ago" },
    new { Type = "review", Message = "Approved PR #123", Time = "3h ago" }
};

// RSC Wire Format endpoint
app.MapGet("/rsc", async (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/x-component";

    // Emit RSC wire format
    var wireFormat = new[]
    {
        // Root layout
        "0:[\"$\",\"div\",null,{\"className\":\"container mx-auto p-6\",\"children\":\"$1\"}]",

        // Header
        "1:[\"$\",\"h1\",null,{\"className\":\"text-3xl font-bold mb-6\",\"children\":\"Dashboard\"}]",

        // Suspense boundary for UserProfile (server component)
        "2:[\"$\",\"$Suspense\",null,{\"fallback\":\"<div class='h-32 bg-muted animate-pulse rounded'></div>\",\"children\":\"$L3\"}]",

        // UserProfile data (streamed)
        $"3:{JsonSerializer.Serialize(currentUser)}",

        // Client component reference (LikeButton)
        "4:[\"$\",\"$Lclient:LikeButton\",null,{\"postId\":\"123\",\"initialCount\":42}]",

        // Client component reference (CommentForm)
        "5:[\"$\",\"$Lclient:CommentForm\",null,{\"postId\":\"123\"}]"
    };

    foreach (var line in wireFormat)
    {
        await ctx.Response.WriteAsync(line + "\n");
        await ctx.Response.Body.FlushAsync();
        await Task.Delay(100); // Simulate streaming
    }
});

// Server Actions endpoint
app.MapPost("/actions/like", async (HttpContext ctx) =>
{
    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(ctx.Request.Body);
    Console.WriteLine($"[Server Action] ToggleLike: {JsonSerializer.Serialize(body)}");
    return Results.Ok(new { success = true });
});

app.MapPost("/actions/comment", async (HttpContext ctx) =>
{
    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(ctx.Request.Body);
    Console.WriteLine($"[Server Action] AddComment: {JsonSerializer.Serialize(body)}");
    return Results.Ok(new { success = true, id = Guid.NewGuid() });
});

// Main HTML page
app.MapGet("/", () => Results.Content(GetHtmlPage(), "text/html"));

app.Run("http://0.0.0.0:5000");

static string GetHtmlPage() => """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Shalimar RSC Demo</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <style>
        .rsc-wire { font-family: monospace; font-size: 12px; background: #1a1a2e; color: #0f0; padding: 10px; border-radius: 8px; overflow-x: auto; }
        .rsc-line { padding: 2px 0; }
        .rsc-line.streaming { animation: flash 0.5s; }
        @keyframes flash { 0% { background: #333; } 100% { background: transparent; } }
        .badge { display: inline-block; padding: 2px 8px; border-radius: 9999px; font-size: 12px; font-weight: 500; }
        .badge-server { background: #22c55e20; color: #22c55e; border: 1px solid #22c55e40; }
        .badge-client { background: #3b82f620; color: #3b82f6; border: 1px solid #3b82f640; }
        .badge-suspense { background: #f59e0b20; color: #f59e0b; border: 1px solid #f59e0b40; }
    </style>
</head>
<body class="bg-gray-950 text-white min-h-screen">
    <div class="container mx-auto p-8">
        <header class="mb-8">
            <h1 class="text-3xl font-bold bg-gradient-to-r from-purple-400 to-pink-400 bg-clip-text text-transparent">
                Shalimar RSC Architecture Demo
            </h1>
            <p class="text-gray-400 mt-2">
                Write .NET Razor → Run <code class="bg-gray-800 px-2 py-1 rounded">dotnet run</code> + <code class="bg-gray-800 px-2 py-1 rounded">npm run</code> → Full React 19 RSC app
            </p>
        </header>

        <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
            <!-- Left: Source Razor -->
            <div class="space-y-4">
                <h2 class="text-xl font-semibold flex items-center gap-2">
                    <span>📝</span> Source: page.razor
                </h2>
                <div class="bg-gray-900 rounded-lg p-4 border border-gray-800">
                    <pre class="text-sm overflow-x-auto"><code class="text-gray-300">@page "/"
<span class="text-green-400">@server</span>
@using Shadcn.UI { Card, Button }

&lt;div className="container"&gt;
    <span class="text-yellow-400">&lt;Suspense fallback="&lt;Skeleton /&gt;"&gt;</span>
        &lt;UserProfile user="<span class="text-purple-400">@await</span> GetUser()" /&gt;
    <span class="text-yellow-400">&lt;/Suspense&gt;</span>

    <span class="text-blue-400">@client</span>
    &lt;LikeButton postId="123" /&gt;
&lt;/div&gt;</code></pre>
                </div>

                <div class="flex gap-2 flex-wrap">
                    <span class="badge badge-server">@server = .NET renders HTML</span>
                    <span class="badge badge-client">@client = React hydrates</span>
                    <span class="badge badge-suspense">Suspense = Streaming</span>
                </div>
            </div>

            <!-- Right: RSC Wire Format -->
            <div class="space-y-4">
                <h2 class="text-xl font-semibold flex items-center gap-2">
                    <span>📡</span> RSC Wire Format (React 19)
                    <button onclick="streamRsc()" class="ml-auto text-sm bg-purple-600 hover:bg-purple-700 px-3 py-1 rounded">
                        Stream →
                    </button>
                </h2>
                <div id="rsc-output" class="rsc-wire min-h-48">
                    <div class="text-gray-500">Click "Stream" to see RSC wire format...</div>
                </div>
            </div>
        </div>

        <!-- Rendered Output -->
        <div class="mt-8">
            <h2 class="text-xl font-semibold mb-4 flex items-center gap-2">
                <span>🖥️</span> Rendered Output (Server + Client Components)
            </h2>
            <div class="bg-gray-900 rounded-lg p-6 border border-gray-800">
                <div id="app-root" class="space-y-6">
                    <!-- This would be hydrated by React in production -->
                    <div class="bg-white/5 rounded-lg p-6">
                        <h3 class="font-semibold mb-4">User Profile <span class="badge badge-server">Server</span></h3>
                        <div class="flex items-center gap-4">
                            <div class="w-16 h-16 rounded-full bg-gradient-to-br from-purple-500 to-pink-500 flex items-center justify-center text-xl font-bold">AJ</div>
                            <div>
                                <div class="font-semibold text-lg">Alex Johnson</div>
                                <div class="text-gray-400">alex@example.com</div>
                                <span class="inline-block mt-1 px-2 py-0.5 bg-green-500/20 text-green-400 text-xs rounded-full">✓ Verified</span>
                            </div>
                        </div>
                        <div class="grid grid-cols-3 gap-4 mt-4 text-center">
                            <div><div class="text-2xl font-bold text-purple-400">47</div><div class="text-sm text-gray-500">Projects</div></div>
                            <div><div class="text-2xl font-bold text-purple-400">2.3k</div><div class="text-sm text-gray-500">Followers</div></div>
                            <div><div class="text-2xl font-bold text-purple-400">4.9</div><div class="text-sm text-gray-500">Rating</div></div>
                        </div>
                    </div>

                    <div class="flex gap-4">
                        <div class="bg-white/5 rounded-lg p-6 flex-1">
                            <h3 class="font-semibold mb-4">Like Button <span class="badge badge-client">Client</span></h3>
                            <button id="like-btn" onclick="handleLike()" class="flex items-center gap-2 px-4 py-2 rounded-lg border border-gray-700 hover:bg-gray-800 transition">
                                <span id="heart">🤍</span>
                                <span id="like-count">42</span>
                            </button>
                        </div>
                        <div class="bg-white/5 rounded-lg p-6 flex-1">
                            <h3 class="font-semibold mb-4">Comment Form <span class="badge badge-client">Client</span></h3>
                            <textarea placeholder="Write a comment..." class="w-full bg-gray-800 border border-gray-700 rounded-lg p-3 text-sm resize-none" rows="2"></textarea>
                            <button class="mt-2 px-4 py-2 bg-purple-600 hover:bg-purple-700 rounded-lg text-sm transition">Send</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Architecture Diagram -->
        <div class="mt-8 bg-gradient-to-r from-purple-900/20 to-pink-900/20 rounded-lg p-6 border border-purple-500/20">
            <h2 class="text-xl font-semibold mb-4">🏗️ Architecture</h2>
            <pre class="text-sm text-gray-300">
┌────────────────────────────────────────────────────────────────────┐
│                         page.razor                                  │
│  @server components + @client components + Suspense boundaries      │
└────────────────────────────────────────────────────────────────────┘
                                │
              ┌─────────────────┴─────────────────┐
              ▼                                   ▼
┌──────────────────────────┐         ┌──────────────────────────┐
│      dotnet run          │         │      npm run dev         │
│   (.NET RSC Server)      │         │    (React Client)        │
│                          │         │                          │
│ • Parses Razor           │   RSC   │ • Consumes wire format   │
│ • Renders @server        │  Wire   │ • Hydrates @client       │
│ • Streams Suspense       │ Format  │ • shadcn/ui components   │
│ • Handles Server Actions │ ──────► │ • Full interactivity     │
└──────────────────────────┘         └──────────────────────────┘
            </pre>
        </div>
    </div>

    <script>
        let liked = false;
        function handleLike() {
            liked = !liked;
            document.getElementById('heart').textContent = liked ? '❤️' : '🤍';
            const count = parseInt(document.getElementById('like-count').textContent);
            document.getElementById('like-count').textContent = liked ? count + 1 : count - 1;

            // Call server action
            fetch('/actions/like', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ postId: '123', liked })
            });
        }

        async function streamRsc() {
            const output = document.getElementById('rsc-output');
            output.innerHTML = '';

            const response = await fetch('/rsc');
            const reader = response.body.getReader();
            const decoder = new TextDecoder();

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const text = decoder.decode(value);
                const lines = text.split('\n').filter(l => l.trim());

                for (const line of lines) {
                    const div = document.createElement('div');
                    div.className = 'rsc-line streaming';
                    div.textContent = line;
                    output.appendChild(div);
                }
            }
        }
    </script>
</body>
</html>
""";
