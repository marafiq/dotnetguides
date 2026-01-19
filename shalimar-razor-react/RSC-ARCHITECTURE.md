# Shalimar RSC Architecture

## The Vision

Write .NET Razor → Run `dotnet run` + `npm run` → Full React 19 RSC app

```
┌─────────────────────────────────────────────────────────────────┐
│                        MyPage.razor                              │
│  @page "/dashboard"                                              │
│  @server                                                         │
│                                                                  │
│  <div>                                                           │
│      <Suspense fallback="<Skeleton />">                          │
│          <UserProfile user="@await GetUser()" />                 │
│      </Suspense>                                                 │
│                                                                  │
│      @client                                                     │
│      <InteractiveChart data="@Props.ChartData" />                │
│  </div>                                                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
         ┌────────────────────┴────────────────────┐
         │                                         │
         ▼                                         ▼
┌─────────────────────┐                 ┌─────────────────────┐
│   dotnet run        │                 │   npm run dev       │
│   (.NET Server)     │                 │   (React Client)    │
│                     │                 │                     │
│ • Renders @server   │    RSC Wire     │ • Hydrates @client  │
│   components        │ ──────────────► │   components        │
│ • Streams Suspense  │    Format       │ • Handles Suspense  │
│ • Async data fetch  │                 │ • shadcn/ui works   │
└─────────────────────┘                 └─────────────────────┘
```

## Razor-RSC Syntax

### 1. Server Components (Default)
```razor
@* Server Component - rendered by .NET, zero JS *@
@server

<Card>
    <CardHeader>
        <CardTitle>@await GetTitle()</CardTitle>
    </CardHeader>
    <CardContent>
        @await RenderMarkdown(Props.Content)
    </CardContent>
</Card>
```

### 2. Client Components
```razor
@* Client Component - hydrated by React *@
@client

<Button variant="outline" @onclick="HandleClick">
    Click me: @State.Count
</Button>

@code {
    [State] int Count { get; set; } = 0;
    void HandleClick() => Count++;
}
```

### 3. Suspense Boundaries
```razor
<Suspense fallback="<Skeleton className='h-20' />">
    @await LoadExpensiveData()
</Suspense>
```

### 4. Streaming
```razor
@streaming
<UserList>
    @await foreach (var user in StreamUsers())
    {
        <UserCard user="@user" />
    }
</UserList>
```

### 5. shadcn/ui Imports
```razor
@using Shadcn.UI { Button, Card, CardHeader, CardTitle, CardContent }
@using Lucide { Icon }

<Card>
    <CardHeader>
        <Icon name="user" />
        <CardTitle>Profile</CardTitle>
    </CardHeader>
</Card>
```

## RSC Wire Format

.NET generates React 19's RSC wire format:

```
0:["$","div",null,{"children":[...]}]
1:["$","$Suspense",null,{"fallback":"...","children":"$L2"}]
2:["$","UserProfile",null,{"user":{"name":"Alex"}}]
```

## File Structure

```
my-app/
├── app/                      # Razor pages (like Next.js app router)
│   ├── page.razor           # → /
│   ├── dashboard/
│   │   └── page.razor       # → /dashboard
│   └── layout.razor         # Root layout
├── components/
│   ├── server/              # @server components
│   │   └── UserProfile.razor
│   └── client/              # @client components
│       └── Counter.razor
├── Program.cs               # .NET entry point
└── package.json             # React/shadcn deps
```

## Commands

```bash
# Development
dotnet run          # Starts .NET RSC server on :5000
npm run dev         # Starts React dev client on :3000 (proxies to :5000)

# Production
dotnet publish      # Builds .NET server
npm run build       # Builds React client
```

## The Leverage

| Component | Lines of Code | Source |
|-----------|---------------|--------|
| Razor Parser | 50,000+ | Microsoft (battle-tested) |
| RSC Wire Format | ~200 | React team spec |
| Shalimar Emitter | ~800 | Our glue code |
| **Total new code** | **~1,000** | |

We write 1,000 lines. We get:
- Full RSC support
- Streaming/Suspense
- shadcn/ui compatibility
- .NET server-side power
- React client-side interactivity
