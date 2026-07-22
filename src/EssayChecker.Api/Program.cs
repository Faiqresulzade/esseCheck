using System.Text;
using System.Text.Json.Serialization;
using EssayChecker.Application.Settings;
using EssayChecker.Infrastructure;
using EssayChecker.Persistence;
using EssayChecker.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Global exception handling ---
builder.Services.AddExceptionHandler<EssayChecker.Api.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- Controllers & Swagger ---
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EssayCheck API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token daxil edin.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { scheme, Array.Empty<string>() }
    });
});

// --- Strongly-typed settings (fail-fast validation on startup) ---
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AppSettings>()
    .BindConfiguration(AppSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OpenRouterSettings>()
    .BindConfiguration(OpenRouterSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Qeyd: GooglePlaySettings qəsdən ValidateOnStart-sız saxlanılır — Play Console tam
// qurulana qədər (service account, məhsullar) tətbiqin qalan hissəsi bloklanmasın.
// Konfiqurasiya yalnız faktiki Google çağırışında (verify/RTDN) yoxlanılır.
builder.Services.AddOptions<GooglePlaySettings>()
    .BindConfiguration(GooglePlaySettings.SectionName);

// --- Layers ---
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure();

// --- Health check (monitorinq/load balancer üçün) ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EssayDbContext>("database");

// --- JWT Authentication ---
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
// "Cors:AllowedOrigins" boşdursa (mobil app-lər CORS-a tabe deyil) hər yerdən icazə verilir.
// Gələcəkdə veb dashboard əlavə olunsa, appsettings/env var ilə konkret domenlərə məhdudlaşdırıla bilər.
const string CorsPolicy = "DefaultCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// --- Reverse proxy dəstəyi (Nginx/IIS/Azure App Service arxasında düzgün sxem/host aşkarlanması üçün) ---
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Proxy-nin öz IP-si əvvəlcədən bilinmədiyi üçün (konteyner/PaaS) bütün proxy-lərə etibar edilir.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// --- Migrations avtomatik tətbiqi (CI/CD pipeline-ı hələ qurulmayıb — bu addım unudulmasın deyə) ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EssayDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration tətbiqi zamanı xəta baş verdi.");
        throw;
    }
}

app.UseExceptionHandler();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
