using Shalimar.Razor;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Compile all Razor files to TSX on startup
var razorDir = Path.Combine(Directory.GetCurrentDirectory(), "razor-components");
var compiler = new RazorReactCompiler();
var generatedComponents = new Dictionary<string, string>();

Console.WriteLine("Compiling Razor components to TSX...");
foreach (var file in Directory.GetFiles(razorDir, "*.razor"))
{
    var source = File.ReadAllText(file);
    var result = compiler.Compile(source, Path.GetFileName(file));
    var name = Path.GetFileNameWithoutExtension(file);
    generatedComponents[name] = result.TsxOutput ?? "";
    Console.WriteLine($"  ✓ {name}.razor → {name}.tsx");
}
Console.WriteLine($"Compiled {generatedComponents.Count} components.\n");

// Serve static files and the app
app.UseStaticFiles();
app.MapGet("/", () => Results.Content(GenerateHtml(generatedComponents), "text/html"));

app.Run("http://0.0.0.0:3003");

static string GenerateHtml(Dictionary<string, string> components)
{
    var componentCode = string.Join("\n\n", components.Select(c =>
    {
        var code = c.Value;
        // Remove import statements
        code = System.Text.RegularExpressions.Regex.Replace(code, @"import React from 'react';\s*", "");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"import \{[^}]+\} from '[^']+';\s*", "");
        // Remove entire interface declarations (export interface Name { ... })
        code = System.Text.RegularExpressions.Regex.Replace(code, @"export interface \w+Props \{[^}]+\}\s*", "");
        // Change export function to just function and strip TypeScript types
        code = code.Replace("export function", "function");
        // Remove TypeScript type annotations from function parameters
        code = System.Text.RegularExpressions.Regex.Replace(code, @"\(props: \w+Props\)", "(props)");
        // React uses props.children (lowercase) for JSX children
        code = code.Replace("props.Children", "props.children");
        return $"// ═══ Generated from {c.Key}.razor ═══\n" + code;
    }));

    return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Shalimar Real App</title>
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
        .btn{padding:12px 24px;border:none;border-radius:10px;font-size:14px;font-weight:600;cursor:pointer}
        .btn-primary{background:linear-gradient(135deg,#667eea,#764ba2);color:#fff}
        .btn-secondary{background:rgba(255,255,255,0.1);color:#fff;border:1px solid rgba(255,255,255,0.2)}
        .info-box{background:rgba(0,212,255,0.1);border:1px solid rgba(0,212,255,0.3);border-radius:12px;padding:20px;margin-top:30px;text-align:center}
        .info-box code{background:rgba(0,0,0,0.3);padding:2px 8px;border-radius:4px}
    </style>
</head>
<body>
<div id="root"></div>
<script type="text/babel">
{{componentCode}}

// Sample data
const appData = {
    Title: "Shalimar Real App",
    Description: "All components compiled from .razor files by dotnet run",
    Users: [
        {
            Name: "Alex Johnson", JobTitle: "Senior Engineer", Initials: "AJ", IsVerified: true,
            Bio: "Full-stack developer with 10+ years experience in React, .NET, and cloud architecture.",
            Stats: [{Value:"47",Label:"Projects"},{Value:"2.3k",Label:"Followers"},{Value:"4.9",Label:"Rating"}],
            Skills: ["React","TypeScript","C#",".NET","Azure"],
            OnFollow: () => alert("Following!"), OnMessage: () => alert("Message!")
        },
        {
            Name: "Sarah Chen", JobTitle: "Product Designer", Initials: "SC", IsVerified: false,
            Bio: "Design systems enthusiast creating beautiful, accessible interfaces.",
            Stats: [{Value:"32",Label:"Projects"},{Value:"1.8k",Label:"Followers"},{Value:"4.7",Label:"Rating"}],
            Skills: ["Figma","UI/UX","Design Systems"],
            OnFollow: () => alert("Following!"), OnMessage: () => alert("Message!")
        }
    ]
};

ReactDOM.createRoot(document.getElementById('root')).render(<App {...appData} />);
</script>
</body>
</html>
""";
}
