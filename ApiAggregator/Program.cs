using ApiAggregator.Configuration;
using ApiAggregator.HostedServices;
using ApiAggregator.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Pull OpenAI API Key from config
var openAiApiKey = builder.Configuration["ExternalApis:OpenAI:ApiKey"];

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// MVC / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Aggregator", Version = "v1" });
});

// Caching & Stats
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<StatsService>();

// Options binding
builder.Services.AddOptions<ExternalApiOptions>()
    .Bind(builder.Configuration.GetSection("ExternalApis"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<OpenAIConfig>(
    builder.Configuration.GetSection("ExternalApis:OpenAI"));

builder.Services.PostConfigure<ExternalApiOptions>(opts =>
{
    if (string.IsNullOrWhiteSpace(opts.OpenWeather.ApiKey) ||
        string.IsNullOrWhiteSpace(opts.NewsApi.ApiKey))
    {
        throw new InvalidOperationException(
            "One or more API keys are missing. " +
            "Populate them via dotnet user-secrets or CI secrets.");
    }
});

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var secretKey = builder.Configuration["JwtSettings:SecretKey"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey!))
    };
});

// HttpClients with Polly policies
builder.Services.AddHttpClient("OpenWeather", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalApis:OpenWeather:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

builder.Services.AddHttpClient("NewsApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalApis:NewsApi:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", openAiApiKey);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddPolicyHandler(GetRetryPolicy());

// DI: services & hosted tasks
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddHostedService<PerformanceMonitorService>();

var app = builder.Build();

// Swagger UI tweaks
app.UseSwagger();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}
else
{
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        opts.DisplayRequestDuration();
        opts.ConfigObject.AdditionalItems["tryItOutEnabled"] = false;
    });
}

// Request pipeline
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Polly helper policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => (int)msg.StatusCode == 429)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
    Policy.TimeoutAsync<HttpResponseMessage>(5);

// Model config record for OpenAI
public record OpenAIConfig(string Model, double Temperature, int MaxTokens);