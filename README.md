# API Aggregator

A **single-endpoint REST API** that fetches data from three external services in parallel—**GitHub**, **NewsAPI**, and **OpenAI**—then returns a merged JSON response together with per-call status flags.  

---

## Features

| Area | Details |
|------|---------|
| Parallel calls | Fetches data from all services simultaneously for optimal performance - for GitHub, news, OpenAI completion |
| Resilience | Polly **retry + timeout** policies on every `HttpClient` |
| Caching | In-memory (`IMemoryCache`) with per-API TTL |
| Auth | Optional **JWT bearer** (demo credentials via `appsettings.*`) |
| Metrics | Thread-safe `StatsService` buckets fast/med/slow, exposed at `/api/aggregator/stats` |
| Background | `PerformanceMonitorService` logs latency anomalies every 5 min |
| Docs | Swagger UI with JWT “Authorize” button |
| Tests | Minimal happy-path & cache-hit unit tests for each service |

---

## Prerequisites

| Tool | Version |
|------|---------|
| **Visual Studio 2022** | 17.10+ (or `dotnet 8.0.x` CLI) |
| **.NET SDK** | 8.0 |
| **API keys** | GitHub, NewsAPI, OpenAI |

---

## Configuration

Use [**dotnet-user-secrets**](https://learn.microsoft.com/aspnet/core/security/app-secrets) (recommended) or `appsettings.Development.json`.

```bash
# From the project directory
dotnet user-secrets init

dotnet user-secrets set "ExternalApis:GitHub:ApiKey"     "<GITHUB_PAT>"
dotnet user-secrets set "ExternalApis:NewsApi:ApiKey"    "<NEWS_KEY>"
dotnet user-secrets set "ExternalApis:OpenAI:ApiKey"     "<OPENAI_KEY>"

# Demo login (optional JWT)
dotnet user-secrets set "DemoCredentials:Username" "demo"
dotnet user-secrets set "DemoCredentials:Password" "demo123"
dotnet user-secrets set "JwtSettings:SecretKey"    "A_very_long_random_secret_key"
```

All other settings (base URLs, model, temperature) live in **`appsettings.json`**.

---

## Running the API

```bash
dotnet run --project ApiAggregator
```

> Visual Studio users: just **F5**.

Navigate to **https://localhost:{port}/swagger** for interactive docs.

---

## JWT Workflow (optional)

1. **Login**

```bash
curl -X POST https://localhost:{port}/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{ "username": "demo", "password": "demo123" }'
```

2. **Call secured endpoints**

Copy the returned token into Swagger’s **Authorize** dialog or add:

```
Authorization: Bearer <token>
```

---

## Endpoints

| Method & Path | Description |
|---------------|-------------|
| **GET `/api/aggregator`** | Main endpoint. Query params:<br/>`gitHubOwner` (default *dotnet*)<br/>`gitHubRepo` (default *runtime*)<br/>`newsQuery` or `newsCategory` (default *general*)<br/>`sortBy` (`relevancy \| popularity \| publishedAt`)<br/>`openAiPrompt` |
| **GET `/api/aggregator/stats`** | Returns per-API totals, averages, and fast/medium/slow counters |
| **POST `/api/auth/login`** | Returns JWT (demo only) |

---

### Sample Call

```bash
curl "https://localhost:{port}/api/aggregator?gitHubOwner=vercel&gitHubRepo=next.js&newsCategory=technology&openAiPrompt=Summarize%20Next.js"
```

Sample response (trimmed):

```json
{
  "gitHub": {
    "full_name": "vercel/next.js",
    "stars": 110000,
    ...
  },
  "news": [{ "title": "...", "source": "The Verge", ... }],
  "openAi": {
    "prompt": "Summarize Next.js",
    "completionText": "Next.js is a React framework for building fast web apps...",
    "retrievedAt": "2025-07-04T06:12:22Z"
  },
  "gitHubStatus": "OK",
  "newsStatus": "OK",
  "openAiStatus": "OK",
  "generatedAtUtc": "2025-07-04T06:12:22Z"
}
```

---

## Running Unit Tests

```bash
dotnet test
```

*Guarantees cache-hit behavior, JSON mapping, and stats bucketing.*

---

## Project Structure (excerpt)

```
ApiAggregator/
│
├── Controllers/
│   ├── AggregatorController.cs
│   └── AuthController.cs
├── Services/
│   ├── GitHubService.cs
│   ├── NewsService.cs
│   ├── OpenAIService.cs
│   └── StatsService.cs
├── HostedServices/
│   └── PerformanceMonitorService.cs
├── Models/
│   └── (DTOs & response wrappers)
├── Configuration/
│   ├── ExternalApiOptions.cs
│   └── JwtSettings.cs
└── Program.cs
```

---

## Known Limitations

* In-memory cache only (no distributed cache)  
* Credentials are static demo values  

---

## Contributing / Extending

- Swap `IMemoryCache` for Redis via `IDistributedCache`  
- Add new APIs (e.g., Twitter/X) by creating a new `I<Source>Service`  
- Wire Polly bulkhead or circuit-breaker policies for advanced resilience  
```
