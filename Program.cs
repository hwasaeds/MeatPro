using System.Threading.RateLimiting;
using MeatPro.Data;
using MeatPro.Models;
using MeatPro.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DefaultConnection is missing.");
}

var mysqlVersion = new MariaDbServerVersion(new Version(10, 4, 27));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, mysqlVersion, mysqlOptions => mysqlOptions.MaxBatchSize(1)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(limiter =>
{
    limiter.AddFixedWindowLimiter("Login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRawMaterialService, RawMaterialService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseTransactionService, PurchaseTransactionService>();
builder.Services.AddScoped<IProductionPlanService, ProductionPlanService>();
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
builder.Services.AddScoped<IProductionBatchService, ProductionBatchService>();
builder.Services.AddScoped<IFinishedGoodService, FinishedGoodService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<IAdministrationService, AdministrationService>();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<AlertViewFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<AlertViewFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await MySqlBootstrapper.EnsureDatabaseExistsAsync(connectionString);
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
