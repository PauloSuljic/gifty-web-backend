using System.Runtime.CompilerServices;
using FirebaseAdmin;
using Gifty.Api.Utils;
using Google.Apis.Auth.OAuth2;
using Gifty.Infrastructure;
using Gifty.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

[assembly: InternalsVisibleTo("Gifty.Tests")]

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ✅ Load environment variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

// ✅ 1. Read Connection String
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
                       ?? configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("❌ No connection string found!");
}

// ✅ 2. Firebase Admin SDK
var useTestAuth = builder.Configuration["UseTestAuth"];

if (useTestAuth != "true")
{
    var firebaseJson = configuration["Firebase:CredentialsJson"];

    if (string.IsNullOrWhiteSpace(firebaseJson))
    {
        throw new Exception("❌ Firebase credentials not found.");
    }

    if (FirebaseApp.DefaultInstance == null)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(firebaseJson)
        });
    }
}

// ✅ 3. Services
builder.Services.AddScoped<FirebaseAuthService>();

// ✅ 4. PostgreSQL DB
builder.Services.AddDbContext<GiftyDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ 5. Auth Setup
#if DEBUG
if (builder.Configuration["UseTestAuth"] == "true")
{
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "https://securetoken.google.com/gifty-auth-71f71";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://securetoken.google.com/gifty-auth-71f71",
                ValidateAudience = true,
                ValidAudience = "gifty-auth-71f71",
                ValidateLifetime = true
            };
        });
}
#else
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://securetoken.google.com/gifty-auth-71f71";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/gifty-auth-71f71",
            ValidateAudience = true,
            ValidAudience = "gifty-auth-71f71",
            ValidateLifetime = true
        };
    });
#endif

builder.Services.AddAuthorization();

// ✅ 6. CORS
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN")?
                         .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ✅ Redis Setup
var isRunningInCi = Environment.GetEnvironmentVariable("CI") == "true";

if (!builder.Environment.IsDevelopment() && !isRunningInCi)
{
    // Production Redis (e.g. Azure)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var redisConnection = builder.Configuration["Redis"];
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new Exception("❌ Redis connection string not found in Azure App Settings (key: Redis).");
        }

        options.Configuration = redisConnection;
    });
}
else if (isRunningInCi)
{
    // ✅ CI fallback – use in-memory caching instead of Redis
    builder.Services.AddDistributedMemoryCache();
}
else
{
    // ✅ Local Redis (e.g. Docker or dev environment)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost:6379"; // Adjust if needed for Docker/Mac/WSL
    });
}

builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

var app = builder.Build();

// ✅ 7. Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GiftyDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();
