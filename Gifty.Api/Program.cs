using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Gifty.Infrastructure;
using Gifty.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ✅ Load environment variables (Needed for Azure Connection String)
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>() // 🔐 required for local secrets
    .AddEnvironmentVariables();

// ✅ 1. Read Connection String
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") 
                       ?? configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("❌ No connection string found! Make sure to set 'DefaultConnection' in Azure or appsettings.json.");
}

// ✅ 2. Initialize Firebase Admin SDK
var firebaseJson = configuration["Firebase:CredentialsJson"];

if (string.IsNullOrWhiteSpace(firebaseJson))
{
    throw new Exception("❌ Firebase credentials not found. Make sure 'Firebase:CredentialsJson' is in user-secrets or env vars.");
}

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromJson(firebaseJson)
});

// ✅ 3. Add Services
builder.Services.AddScoped<FirebaseAuthService>();

// ✅ 4. Configure PostgreSQL Database
builder.Services.AddDbContext<GiftyDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ 5. Enable Authentication with Firebase JWT
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

builder.Services.AddAuthorization();

// ✅ 6. Enable CORS (Allow frontend to access API)
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

var app = builder.Build();

// ✅ 7. Configure Middleware
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

app.Run();
