// ==========================================================
// ADDED: DATABASE KE LIYE REQUIRED NAMESPACES
// ==========================================================
// BiitDbContext hamari generated database context class hai.
// EntityFrameworkCore se UseSqlServer method available hota hai.
// ==========================================================

using BIITPaperSettingAPI.Data;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ==========================================================
// ADDED: DATABASE CONNECTION STRING READ KARNA
// ==========================================================
// appsettings.json ke ConnectionStrings section se
// DefaultConnection read kar rahe hain.
// Missing ho to application clear error ke saath ruk jayegi.
// ==========================================================

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection connection string appsettings.json mein missing hai."
    );

// ==========================================================
// ADDED: ENTITY FRAMEWORK KO SQL SERVER KE SAATH REGISTER KARNA
// ==========================================================
// Is registration se controllers/services ko BiitDbContext
// constructor ke through mil sakega.
//
// Yeh existing database use karta hai.
// Database ya tables dobara create nahi karta.
// ==========================================================

if (args.Length == 2 && args[0] == "--provision-local-demo")
{
    await LocalDemoCredential.SetAsync(connectionString, args[1]);
    return;
}

builder.Services.AddDbContext<BiitDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// =====================================================
// STEP 1: VALIDATE SIGNED STAFF TOKENS
// =====================================================
// Server issuer, audience, signature aur expiry sab check karta hai.
var tokens = new StaffTokens(builder.Configuration);
builder.Services.AddSingleton(tokens);
builder.Services.AddScoped<StaffAccessService>();
builder.Services.AddScoped<SetupProcedures>();
builder.Services.AddScoped<PasswordHasher<string>>();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = tokens.Issuer,
        ValidateAudience = true, ValidAudience = tokens.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = tokens.Key,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
