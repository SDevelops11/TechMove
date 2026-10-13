using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Web.Data;
using TechMove.GLMS.Web.Factories;
using TechMove.GLMS.Web.Observers;
using TechMove.GLMS.Web.Repositories;
using TechMove.GLMS.Web.Services;
using TechMove.GLMS.Web.Strategies;

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC Controllers & Views
builder.Services.AddControllersWithViews();

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
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS/1.0");
});

// 5. Register Data Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();

// 6. Register Design Pattern 1: Factory Pattern
builder.Services.AddSingleton<IContractNumberGenerator, ContractNumberGenerator>();
builder.Services.AddScoped<IContractFactory, ContractFactory>();

// 7. Register Design Pattern 2: Observer Pattern (4 Departmental Observers)
builder.Services.AddScoped<IContractObserver, OperationsDepartmentObserver>();
builder.Services.AddScoped<IContractObserver, FinanceDepartmentObserver>();
builder.Services.AddScoped<IContractObserver, ClientServicesObserver>();
builder.Services.AddScoped<IContractObserver, ComplianceDepartmentObserver>();
builder.Services.AddScoped<IContractNotifier, ContractNotifier>();

// 8. Register Design Pattern 3: Strategy Pattern (Currency Conversion Algorithms)
builder.Services.AddScoped<ICurrencyConverter, UsdToZarConverter>();
builder.Services.AddScoped<ICurrencyConverter, EurToZarConverter>();
builder.Services.AddScoped<ICurrencyConverter, GbpToZarConverter>();
builder.Services.AddScoped<ICurrencyConverter, ApiBasedConverter>();
builder.Services.AddScoped<ICurrencyConversionContext, CurrencyConversionContext>();

// 9. Register Storage & Business Services
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();

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

// HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
