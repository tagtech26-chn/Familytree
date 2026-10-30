using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FamilyConnect.Application.Abstractions;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using FamilyConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("FamilyConnect")
    ?? throw new InvalidOperationException("ConnectionStrings:FamilyConnect is required.");
builder.Services.AddDbContext<FamilyConnectDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FamilyGroupService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IOtpSender, DevelopmentOtpSender>();

var jwtKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configure Jwt:SigningKey using user-secrets.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = ClaimTypes.NameIdentifier
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("otp", opt => {
        opt.PermitLimit = 5; opt.Window = TimeSpan.FromMinutes(15); opt.QueueLimit = 0;
        opt.AutoReplenishment = true;
    });
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "FamilyConnect.API" }));
app.Run();

public partial class Program { }
