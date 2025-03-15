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

// ✅ 1. Add Firebase Admin SDK (For Server-Side Operations)
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("firebase-service-account.json")
});

// ✅ 2. Add Services
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddDbContext<GiftyDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ 3. Enable Authentication with Firebase JWT
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

// ✅ 4. Enable CORS (So Frontend Can Access API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin() // Allow frontend requests (update for security if needed)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ 5. Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication(); // Enable Firebase JWT Authentication
app.UseAuthorization();

app.MapControllers();

app.Run();
