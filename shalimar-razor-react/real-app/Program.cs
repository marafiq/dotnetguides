using Shalimar.Razor;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Compile all Razor files to TSX using Microsoft Razor parser
var razorDir = Path.Combine(Directory.GetCurrentDirectory(), "razor-components");
var compiler = new RazorReactCompiler();
var generatedComponents = new Dictionary<string, string>();

Console.WriteLine("Compiling Razor → TSX (via Microsoft Razor Parser)...");
foreach (var file in Directory.GetFiles(razorDir, "*.razor"))
{
    var source = File.ReadAllText(file);
    var result = compiler.Compile(source, Path.GetFileName(file));
    var name = Path.GetFileNameWithoutExtension(file);
    generatedComponents[name] = result.TsxOutput ?? "";

    var directive = source.Contains("@client") ? "@client" : "@server";
    Console.WriteLine($"  ✓ {name}.razor → {name}.tsx [{directive}]");
}
Console.WriteLine($"\nCompiled {generatedComponents.Count} components.\n");

// Serve static files and the app
app.UseStaticFiles();
app.MapGet("/", () => Results.Content(GenerateReactApp(generatedComponents), "text/html"));

// RSC Wire Format endpoint - demonstrates React Server Components protocol
app.MapGet("/rsc", () => Results.Content(GenerateRscApp(generatedComponents), "text/html"));

// RSC payload endpoint - returns pure wire format
app.MapGet("/api/rsc", () =>
{
    var payload = GenerateRscPayload();
    return Results.Text(payload, "text/x-component");
});

app.Run("http://0.0.0.0:3003");

static string GenerateReactApp(Dictionary<string, string> components)
{
    // Transform TSX for browser use (strip TypeScript, fix imports)
    var componentCode = string.Join("\n\n", components.Select(c =>
    {
        var code = c.Value;
        code = System.Text.RegularExpressions.Regex.Replace(code, @"import React from 'react';\s*", "");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"import \{[^}]+\} from '[^']+';\s*", "");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"export interface \w+Props \{[^}]+\}\s*", "");
        code = code.Replace("export function", "function");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"\(props: \w+Props\)", "(props)");
        code = code.Replace("props.Children", "props.children");
        return $"// ═══ {c.Key}.razor → TSX ═══\n{code}";
    }));

    return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Shalimar: Razor → React</title>
    <script src="/react.min.js"></script>
    <script src="/react-dom.min.js"></script>
    <script src="/babel.min.js"></script>
    <style>
        *{box-sizing:border-box;margin:0;padding:0}
        body{font-family:system-ui,sans-serif;background:linear-gradient(135deg,#1a1a2e,#16213e);min-height:100vh;color:#fff}
        .app-container{max-width:900px;margin:0 auto;padding:40px 20px}
        .app-header{text-align:center;margin-bottom:40px}
        .app-header h1{font-size:32px;background:linear-gradient(90deg,#00d4ff,#7b2ff7);-webkit-background-clip:text;-webkit-text-fill-color:transparent}
        .app-header p{color:#888;margin-top:8px}
        .app-main{display:grid;gap:24px}
        .app-footer{text-align:center;margin-top:40px;color:#666}
        .card{background:rgba(255,255,255,0.05);border-radius:16px;border:1px solid rgba(255,255,255,0.1)}
        .card-header{padding:20px;border-bottom:1px solid rgba(255,255,255,0.1)}
        .card-title{font-size:20px}
        .card-subtitle{color:#888;font-size:14px;margin-top:4px}
        .card-body{padding:20px}
        .profile-header{display:flex;align-items:center;gap:16px;margin-bottom:20px}
        .avatar{width:72px;height:72px;border-radius:50%;background:linear-gradient(135deg,#667eea,#764ba2);display:flex;align-items:center;justify-content:center}
        .avatar-image{width:100%;height:100%;object-fit:cover;border-radius:50%}
        .avatar-fallback{font-size:24px;font-weight:bold;color:#fff}
        .badge{padding:4px 12px;border-radius:20px;font-size:12px;font-weight:600}
        .badge.success{background:rgba(34,197,94,0.2);color:#22c55e}
        .stats-row{display:flex;gap:24px;padding:16px 0;border-top:1px solid rgba(255,255,255,0.1);border-bottom:1px solid rgba(255,255,255,0.1);margin:16px 0}
        .stat-item{text-align:center;flex:1}
        .stat-value{display:block;font-size:24px;font-weight:bold;color:#00d4ff}
        .stat-label{font-size:11px;color:#888;text-transform:uppercase}
        .profile-bio p{color:#aaa;line-height:1.6;margin-bottom:16px}
        .skills-title{font-size:12px;color:#888;margin-bottom:8px;text-transform:uppercase}
        .skills-container{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:20px}
        .skill-tag{background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;padding:6px 14px;border-radius:20px;font-size:13px}
        .profile-actions{display:flex;gap:12px}
        .btn{padding:12px 24px;border:none;border-radius:10px;font-size:14px;font-weight:600;cursor:pointer;transition:transform 0.1s}
        .btn:hover{transform:scale(1.02)}
        .btn-primary{background:linear-gradient(135deg,#667eea,#764ba2);color:#fff}
        .btn-secondary{background:rgba(255,255,255,0.1);color:#fff;border:1px solid rgba(255,255,255,0.2)}
        .shalimar-badge{position:fixed;bottom:20px;right:20px;background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;padding:10px 20px;border-radius:10px;font-size:12px;font-weight:600;box-shadow:0 4px 15px rgba(102,126,234,0.4)}
    </style>
</head>
<body>
<div id="root"></div>
<script type="text/babel">
// ═══════════════════════════════════════════════════════════════
// All components below compiled from .razor files by Shalimar
// using Microsoft.AspNetCore.Razor.Language parser
// ═══════════════════════════════════════════════════════════════

{{componentCode}}

// ═══════════════════════════════════════════════════════════════
// App Data & Render
// ═══════════════════════════════════════════════════════════════
const appData = {
    Title: "Shalimar: Razor → React",
    Description: "Write .razor files, get React components - powered by Microsoft Razor Parser",
    Users: [
        {
            Name: "Alex Johnson",
            JobTitle: "Senior Engineer",
            Initials: "AJ",
            IsVerified: true,
            Bio: "Full-stack developer with 10+ years experience in React, .NET, and cloud architecture.",
            Stats: [{Value:"47",Label:"Projects"},{Value:"2.3k",Label:"Followers"},{Value:"4.9",Label:"Rating"}],
            Skills: ["React","TypeScript","C#",".NET","Azure"],
            OnFollow: () => alert("Following Alex!"),
            OnMessage: () => alert("Message sent to Alex!")
        },
        {
            Name: "Sarah Chen",
            JobTitle: "Product Designer",
            Initials: "SC",
            IsVerified: false,
            Bio: "Design systems enthusiast creating beautiful, accessible interfaces.",
            Stats: [{Value:"32",Label:"Projects"},{Value:"1.8k",Label:"Followers"},{Value:"4.7",Label:"Rating"}],
            Skills: ["Figma","UI/UX","Design Systems"],
            OnFollow: () => alert("Following Sarah!"),
            OnMessage: () => alert("Message sent to Sarah!")
        }
    ]
};

ReactDOM.createRoot(document.getElementById('root')).render(<App {...appData} />);
</script>

<div class="shalimar-badge">
    Razor → React
</div>
</body>
</html>
""";
}

// Generate RSC wire format payload
static string GenerateRscPayload()
{
    // RSC Wire Format: line-delimited JSON representing the component tree
    // Format: id:["$","tagName",key,{props}]
    var sb = new System.Text.StringBuilder();

    // Build the component tree as RSC wire format
    // This is what .NET generates - React interprets it on client
    var users = new[]
    {
        new { Name = "Alex Johnson", JobTitle = "Senior Engineer", Initials = "AJ", IsVerified = true,
              Bio = "Full-stack developer with 10+ years experience.", Stats = new[] {
                  new { Value = "47", Label = "Projects" },
                  new { Value = "2.3k", Label = "Followers" },
                  new { Value = "4.9", Label = "Rating" }
              }, Skills = new[] { "React", "TypeScript", "C#", ".NET", "Azure" } },
        new { Name = "Sarah Chen", JobTitle = "Product Designer", Initials = "SC", IsVerified = false,
              Bio = "Design systems enthusiast creating beautiful interfaces.", Stats = new[] {
                  new { Value = "32", Label = "Projects" },
                  new { Value = "1.8k", Label = "Followers" },
                  new { Value = "4.7", Label = "Rating" }
              }, Skills = new[] { "Figma", "UI/UX", "Design Systems" } }
    };

    // Build Users array in wire format
    var usersArray = users.Select((u, i) => new {
        Name = u.Name,
        JobTitle = u.JobTitle,
        Initials = u.Initials,
        IsVerified = u.IsVerified,
        Bio = u.Bio,
        Stats = u.Stats,
        Skills = u.Skills
    }).ToArray();

    var usersJson = System.Text.Json.JsonSerializer.Serialize(usersArray);

    // Root App component with full props
    sb.AppendLine($@"0:[""$"",""App"",null,{{""Title"":""Shalimar RSC"",""Description"":""RSC Wire Format from .NET"",""Users"":{usersJson}}}]");

    return sb.ToString();
}

// Generate RSC-based app that uses wire format
static string GenerateRscApp(Dictionary<string, string> components)
{
    var componentCode = string.Join("\n\n", components.Select(c =>
    {
        var code = c.Value;
        code = System.Text.RegularExpressions.Regex.Replace(code, @"import React from 'react';\s*", "");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"import \{[^}]+\} from '[^']+';\s*", "");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"export interface \w+Props \{[^}]+\}\s*", "");
        code = code.Replace("export function", "function");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"\(props: \w+Props\)", "(props)");
        code = code.Replace("props.Children", "props.children");
        return $"// {c.Key}.razor → TSX\n{code}";
    }));

    var rscPayload = GenerateRscPayload();

    return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Shalimar RSC Mode</title>
    <script src="/react.min.js"></script>
    <script src="/react-dom.min.js"></script>
    <script src="/babel.min.js"></script>
    <style>
        *{box-sizing:border-box;margin:0;padding:0}
        body{font-family:system-ui,sans-serif;background:linear-gradient(135deg,#0f172a,#1e293b);min-height:100vh;color:#fff}
        .app-container{max-width:900px;margin:0 auto;padding:40px 20px}
        .app-header{text-align:center;margin-bottom:40px}
        .app-header h1{font-size:32px;background:linear-gradient(90deg,#22c55e,#3b82f6);-webkit-background-clip:text;-webkit-text-fill-color:transparent}
        .app-header p{color:#888;margin-top:8px}
        .app-main{display:grid;gap:24px}
        .app-footer{text-align:center;margin-top:40px;color:#666}
        .card{background:rgba(255,255,255,0.05);border-radius:16px;border:1px solid rgba(255,255,255,0.1)}
        .card-header{padding:20px;border-bottom:1px solid rgba(255,255,255,0.1)}
        .card-title{font-size:20px}
        .card-subtitle{color:#888;font-size:14px;margin-top:4px}
        .card-body{padding:20px}
        .profile-header{display:flex;align-items:center;gap:16px;margin-bottom:20px}
        .avatar{width:72px;height:72px;border-radius:50%;background:linear-gradient(135deg,#22c55e,#3b82f6);display:flex;align-items:center;justify-content:center}
        .avatar-fallback{font-size:24px;font-weight:bold;color:#fff}
        .badge{padding:4px 12px;border-radius:20px;font-size:12px;font-weight:600}
        .badge.success{background:rgba(34,197,94,0.2);color:#22c55e}
        .stats-row{display:flex;gap:24px;padding:16px 0;border-top:1px solid rgba(255,255,255,0.1);border-bottom:1px solid rgba(255,255,255,0.1);margin:16px 0}
        .stat-item{text-align:center;flex:1}
        .stat-value{display:block;font-size:24px;font-weight:bold;color:#22c55e}
        .stat-label{font-size:11px;color:#888;text-transform:uppercase}
        .profile-bio p{color:#aaa;line-height:1.6;margin-bottom:16px}
        .skills-container{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:20px}
        .skill-tag{background:linear-gradient(135deg,#22c55e,#3b82f6);color:#fff;padding:6px 14px;border-radius:20px;font-size:13px}
        .profile-actions{display:flex;gap:12px}
        .btn{padding:12px 24px;border:none;border-radius:10px;font-size:14px;font-weight:600;cursor:pointer}
        .btn-primary{background:linear-gradient(135deg,#22c55e,#3b82f6);color:#fff}
        .btn-secondary{background:rgba(255,255,255,0.1);color:#fff;border:1px solid rgba(255,255,255,0.2)}
        .rsc-badge{position:fixed;bottom:20px;right:20px;background:linear-gradient(135deg,#22c55e,#3b82f6);color:#fff;padding:10px 20px;border-radius:10px;font-size:12px;font-weight:600}
        .wire-format{background:rgba(0,0,0,0.3);border-radius:8px;padding:16px;margin:20px 0;font-family:monospace;font-size:11px;overflow-x:auto;white-space:pre;color:#22c55e}
    </style>
</head>
<body>
<div id="root"></div>

<script type="text/babel">
// ═══════════════════════════════════════════════════════════════
// Components compiled from .razor files
// ═══════════════════════════════════════════════════════════════
{{componentCode}}

// ═══════════════════════════════════════════════════════════════
// RSC Wire Format Runtime
// ═══════════════════════════════════════════════════════════════
const RSC_PAYLOAD = `{{rscPayload.Replace("`", "\\`")}}`;

// RSC Wire Format Parser & Renderer
function parseRscPayload(payload) {
    const lines = payload.trim().split('\n');
    const elements = {};
    for (const line of lines) {
        const colonIdx = line.indexOf(':');
        const id = line.substring(0, colonIdx);
        const data = JSON.parse(line.substring(colonIdx + 1));
        elements[id] = data;
    }
    return elements;
}

function resolveRscElement(data, elements, components) {
    if (!Array.isArray(data) || data[0] !== '$') return data;

    const [marker, type, key, props] = data;

    // Resolve children references ($1, $2, etc.)
    const resolvedProps = { ...props };
    if (props?.children) {
        if (typeof props.children === 'string' && props.children.startsWith('$')) {
            const refId = props.children.substring(1);
            if (refId.startsWith('L')) {
                // Lazy reference
                resolvedProps.children = resolveRscElement(elements[refId.substring(1)], elements, components);
            } else {
                resolvedProps.children = resolveRscElement(elements[refId], elements, components);
            }
        } else if (Array.isArray(props.children)) {
            resolvedProps.children = props.children.map(c => {
                if (typeof c === 'string' && c.startsWith('$L')) {
                    return resolveRscElement(elements[c.substring(2)], elements, components);
                }
                return c;
            });
        }
    }

    const Component = components[type] || type;
    return React.createElement(Component, { key, ...resolvedProps });
}

// Component registry
const COMPONENTS = { App, UserProfile, Card, Avatar, Badge, StatsRow, StatItem, SkillsList, SkillTag, Button };

// Parse and render RSC payload
const elements = parseRscPayload(RSC_PAYLOAD);
const rootElement = resolveRscElement(elements['0'], elements, COMPONENTS);

// Show wire format for demo
const wireFormatStyle = {marginTop: '40px', padding: '20px', background: 'rgba(0,0,0,0.2)', borderRadius: '12px'};
const headingStyle = {marginBottom: '12px', color: '#22c55e'};
const noteStyle = {color: '#888', fontSize: '12px', marginTop: '12px'};
const WireFormatDemo = () => (
    <div style={wireFormatStyle}>
        <h3 style={headingStyle}>RSC Wire Format (generated by .NET)</h3>
        <pre className="wire-format">{RSC_PAYLOAD}</pre>
        <p style={noteStyle}>
            This wire format was generated server-side by .NET. React interprets it to render the UI above.
        </p>
    </div>
);

// Render with wire format demo
ReactDOM.createRoot(document.getElementById('root')).render(
    <div className="app-container">
        {rootElement}
        <WireFormatDemo />
    </div>
);
</script>

<div class="rsc-badge">RSC Mode</div>
</body>
</html>
""";
}
