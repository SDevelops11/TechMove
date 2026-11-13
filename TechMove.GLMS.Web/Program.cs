using TechMove.GLMS.Web.Services.ApiClients;

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC Controllers & Views
builder.Services.AddControllersWithViews();

// 2. Configure Backend Web API Base Address
// Resolves local development endpoint (http://localhost:5000) or Docker container service name (http://glms-backend-api:8080)
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("ApiSettings__BaseUrl")
    ?? "http://localhost:5000";

if (!apiBaseUrl.EndsWith("/"))
{
    apiBaseUrl += "/";
}

var apiUri = new Uri(apiBaseUrl);

// 3. Register Typed HttpClients for Frontend Service Layer (Decoupled SOA Architecture)
// Zero SQL dependencies in MVC: All operations communicate with TechMove.GLMS.API
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS-WebClient/2.0");
});

builder.Services.AddHttpClient<IContractApiClient, ContractApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS-WebClient/2.0");
});

builder.Services.AddHttpClient<IClientApiClient, ClientApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS-WebClient/2.0");
});

builder.Services.AddHttpClient<IServiceRequestApiClient, ServiceRequestApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS-WebClient/2.0");
});

builder.Services.AddHttpClient<ICurrencyApiClient, CurrencyApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "TechMove-GLMS-WebClient/2.0");
});

var app = builder.Build();

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
