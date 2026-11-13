using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TechMove.GLMS.API.Data;
using TechMove.GLMS.API.Factories;
using TechMove.GLMS.API.Observers;
using TechMove.GLMS.API.Repositories;
using TechMove.GLMS.API.Services;
using TechMove.GLMS.API.Strategies;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers with JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 2. Add In-Memory Caching for Currency API rates
builder.Services.AddMemoryCache();

// 3. Database Context (Entity Framework Core with SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=TechMoveGLMS_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 4. Register External API HttpClient with Resilience
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS-API/1.0");
});

// 5. Register Data Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();

// 6. Register Design Pattern 1: GoF Factory Pattern
builder.Services.AddSingleton<IContractNumberGenerator, ContractNumberGenerator>();
builder.Services.AddScoped<IContractFactory, ContractFactory>();

// 7. Register Design Pattern 2: GoF Observer Pattern (4 Departmental Observers)
builder.Services.AddScoped<IContractObserver, OperationsDepartmentObserver>();
builder.Services.AddScoped<IContractObserver, FinanceDepartmentObserver>();
builder.Services.AddScoped<IContractObserver, ClientServicesObserver>();
builder.Services.AddScoped<IContractObserver, ComplianceDepartmentObserver>();
builder.Services.AddScoped<IContractNotifier, ContractNotifier>();

// 8. Register Design Pattern 3: GoF Strategy Pattern (Currency Conversion Algorithms)
builder.Services.AddScoped<ICurrencyConverter, UsdToZarConverter>();
builder.Services.AddScoped<ICurrencyConverter, EurToZarConverter>();
builder.Services.AddScoped<ICurrencyConverter, GbpToZarConverter>();
builder.Services.AddScoped<ICurrencyConverter, ApiBasedConverter>();
builder.Services.AddScoped<ICurrencyConversionContext, CurrencyConversionContext>();

// 9. Register Storage, Authentication & Business Services
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 10. Configure JWT Bearer Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? "TechMove_Enterprise_GLMS_Super_Secret_Key_2026_Secure_Token_Must_Be_Long_Enough!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TechMove.GLMS.API";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "TechMove.GLMS.Clients";

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 11. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 12. Swagger / OpenAPI Configuration with JWT Bearer Definition
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TechMove Logistics — GLMS Web API",
        Version = "v1",
        Description = "Enterprise RESTful Web API for TechMove Global Logistics Management System (GLMS). " +
                      "Provides endpoints for Contracts, Service Requests, Currency Conversion, and JWT Authentication.",
        Contact = new OpenApiContact
        {
            Name = "TechMove Solutions Architecture Team",
            Email = "architecture@techmove.com"
        }
    });

    // Add JWT Bearer Authentication in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Ensure Database is Created and Seeded on application startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating or seeding the GLMS database.");
    }
}

// Enable Swagger in all environments (both Development and Docker Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechMove GLMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory integration testing
public partial class Program { }
