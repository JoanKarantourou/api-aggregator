using ApiAggregator.BackgroundTasks;
using ApiAggregator.Configuration;
using ApiAggregator.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<StatsService>(); // API usage counts
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ExternalApiOptions>(
    builder.Configuration.GetSection("ExternalApis"));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddHttpClient("OpenWeather", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApis:OpenWeather:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("NewsApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApis:NewsApi:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApis:OpenAI:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Authorization",
        $"Bearer {builder.Configuration["ExternalApis:OpenAI:ApiKey"]}");
});

builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<OpenAIService>();

builder.Services.AddHostedService<PerformanceMonitorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();