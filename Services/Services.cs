using System.Text.Json;
using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> BuildAsync(int days = 30, CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task<ModulePageViewModel> BuildRawMaterialsPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildFinishedGoodsPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildStockMovementsPageAsync(CancellationToken cancellationToken = default);
}

public interface IProductionService
{
    Task<ModulePageViewModel> BuildPlansPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildWorkOrdersPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildTraceabilityPageAsync(CancellationToken cancellationToken = default);
}

public interface IProcurementService
{
    Task<ModulePageViewModel> BuildSuppliersPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildPurchasesPageAsync(CancellationToken cancellationToken = default);
}

public interface IAdministrationService
{
    Task<ModulePageViewModel> BuildUsersPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildRolesPageAsync(CancellationToken cancellationToken = default);
    Task<ModulePageViewModel> BuildSettingsPageAsync(CancellationToken cancellationToken = default);
    Task<UserIndexViewModel> BuildUserIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserDetailsViewModel?> GetUserDetailsAsync(string id, CancellationToken cancellationToken = default);
    Task<UserFormViewModel?> GetUserEditAsync(string id, CancellationToken cancellationToken = default);
    Task<UserDetailsViewModel?> GetUserDeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> IsUserNameInUseAsync(string userName, string? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsEmailInUseAsync(string email, string? excludeId = null, CancellationToken cancellationToken = default);
    Task<string> CreateUserAsync(UserFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(string id, UserFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleIndexViewModel> BuildRoleIndexAsync(string? search, CancellationToken cancellationToken = default);
    Task<RoleDetailsViewModel?> GetRoleDetailsAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleFormViewModel?> GetRoleEditAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleDetailsViewModel?> GetRoleDeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> IsRoleNameInUseAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default);
    Task<string> CreateRoleAsync(RoleFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(string id, RoleFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> BuildAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var dateFrom = days > 0 ? DateTime.UtcNow.AddDays(-days) : (DateTime?)null;

        var rawMaterials = await _context.RawMaterials.AsNoTracking().ToListAsync(cancellationToken);
        var workOrders = await _context.WorkOrders.AsNoTracking().ToListAsync(cancellationToken);
        var finishedGoods = await _context.FinishedGoods.AsNoTracking().ToListAsync(cancellationToken);
        var suppliers = await _context.Suppliers.AsNoTracking().ToListAsync(cancellationToken);

        var batches = dateFrom.HasValue
            ? await _context.ProductionBatches.AsNoTracking().Where(b => b.ProducedAt >= dateFrom.Value).ToListAsync(cancellationToken)
            : await _context.ProductionBatches.AsNoTracking().ToListAsync(cancellationToken);

        var movements = dateFrom.HasValue
            ? await _context.StockMovements.AsNoTracking().Where(m => m.MovementDate >= dateFrom.Value).ToListAsync(cancellationToken)
            : await _context.StockMovements.AsNoTracking().ToListAsync(cancellationToken);

        var products = await _context.Products.AsNoTracking().ToListAsync(cancellationToken);

        var purchases = dateFrom.HasValue
            ? await _context.PurchaseTransactions.AsNoTracking().Include(x => x.Supplier).Where(p => p.PurchasedOn >= dateFrom.Value).ToListAsync(cancellationToken)
            : await _context.PurchaseTransactions.AsNoTracking().Include(x => x.Supplier).ToListAsync(cancellationToken);

        var periodLabel = days switch { 7 => "7 days", 30 => "30 days", 90 => "90 days", 365 => "1 year", _ => "All time" };

        var dashboard = new DashboardViewModel
        {
            SelectedPeriodDays = days,
            TotalRawMaterials = rawMaterials.Count,
            LowStockMaterials = rawMaterials.Count(x => x.CurrentStock <= x.ReorderLevel),
            ActiveWorkOrders = workOrders.Count(x => x.Status is WorkOrderStatus.Planned or WorkOrderStatus.InProgress or WorkOrderStatus.OnHold),
            FinishedGoodsCount = finishedGoods.Sum(x => (int)x.CurrentStock),
            MonthlyProductionOutput = batches.Where(x => x.ProducedAt >= DateTime.UtcNow.AddDays(-30)).Sum(x => x.ProducedQuantity),
            TotalSuppliers = suppliers.Count,
            Alerts = rawMaterials.Where(x => x.CurrentStock <= x.ReorderLevel).Select(x => new AlertItemViewModel { Title = "Low Stock Alert", Message = $"{x.Name} is below reorder level.", Tone = "warning" }).Take(3).ToList()
        };

        dashboard.MetricCards = new[]
        {
            new MetricCardViewModel { Title = "Total Raw Materials", Value = dashboard.TotalRawMaterials.ToString(), Caption = "Tracked ingredients", Icon = "bi-box-seam", Tone = "primary" },
            new MetricCardViewModel { Title = "Low Stock Materials", Value = dashboard.LowStockMaterials.ToString(), Caption = "Reorder soon", Icon = "bi-exclamation-triangle", Tone = "warning" },
            new MetricCardViewModel { Title = "Active Work Orders", Value = dashboard.ActiveWorkOrders.ToString(), Caption = "In progress", Icon = "bi-clipboard2-check", Tone = "success" },
            new MetricCardViewModel { Title = "Finished Goods Count", Value = dashboard.FinishedGoodsCount.ToString(), Caption = "Units on hand", Icon = "bi-bag-check", Tone = "secondary" },
            new MetricCardViewModel { Title = "Production Output", Value = dashboard.MonthlyProductionOutput.ToString("N0"), Caption = periodLabel, Icon = "bi-graph-up-arrow", Tone = "primary" },
            new MetricCardViewModel { Title = "Total Suppliers", Value = dashboard.TotalSuppliers.ToString(), Caption = "Vendor base", Icon = "bi-truck", Tone = "success" }
        };

        dashboard.ProductionOutputTrend = new ChartSeriesViewModel
        {
            Labels = Enumerable.Range(0, 6).Select(i => DateTime.UtcNow.AddMonths(-5 + i).ToString("MMM")).ToList(),
            Values = Enumerable.Range(0, 6).Select(i => batches.Where(x => x.ProducedAt.Month == DateTime.UtcNow.AddMonths(-5 + i).Month).Sum(x => x.ProducedQuantity)).ToList()
        };

        dashboard.InventoryMovementChart = new ChartSeriesViewModel
        {
            Labels = new[] { "Stock In", "Stock Out", "Adjustment", "Release" }.ToList(),
            Values = new decimal[]
            {
                movements.Count(x => x.MovementType == InventoryMovementType.StockIn),
                movements.Count(x => x.MovementType == InventoryMovementType.StockOut),
                movements.Count(x => x.MovementType == InventoryMovementType.Adjustment),
                movements.Count(x => x.MovementType == InventoryMovementType.ReleaseToProduction)
            }.ToList()
        };

        dashboard.TopProducedProducts = new ChartSeriesViewModel
        {
            Labels = products.Take(5).Select(x => x.Name).ToList(),
            Values = products.Take(5).Select(x => batches.Where(b => b.ProductId == x.Id).Sum(b => b.ProducedQuantity)).ToList()
        };

        dashboard.MaterialConsumption = new ChartSeriesViewModel
        {
            Labels = rawMaterials.Take(5).Select(x => x.Name).ToList(),
            Values = rawMaterials.Take(5).Select(x => Math.Round(x.CurrentStock * 0.65m, 0)).ToList()
        };

        dashboard.RecentActivities = purchases.Take(3).Select(x => new ActivityItemViewModel
        {
            Title = "Recent Purchase",
            Description = $"{x.PurchaseNumber} from {x.Supplier?.Name ?? "Unknown supplier"}",
            TimeAgo = x.PurchasedOn.ToString("dd MMM yyyy"),
            Badge = x.Status.ToString(),
            BadgeTone = x.Status == PurchaseStatus.Received ? "success" : "warning"
        }).Concat(workOrders.Take(3).Select(x => new ActivityItemViewModel
        {
            Title = "Work Order Update",
            Description = $"{x.WorkOrderNumber} for {x.Quantity:N0} units",
            TimeAgo = x.ScheduledDate.ToString("dd MMM yyyy"),
            Badge = x.Status.ToString(),
            BadgeTone = x.Status == WorkOrderStatus.InProgress ? "primary" : "secondary"
        })).Concat(movements.Take(3).Select(x => new ActivityItemViewModel
        {
            Title = "Stock Movement",
            Description = $"{x.ItemName} - {x.MovementType}",
            TimeAgo = x.MovementDate.ToString("dd MMM yyyy"),
            Badge = x.MovementType.ToString(),
            BadgeTone = x.MovementType == InventoryMovementType.StockIn ? "success" : "danger"
        })).ToList();

        return dashboard;
    }
}

public sealed class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;

    public InventoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ModulePageViewModel> BuildRawMaterialsPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.RawMaterials.AsNoTracking().OrderBy(x => x.Name).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Raw Material Inventory",
            Subtitle = "Manage stock in, stock out, low stock alerts, and movement logs.",
            SearchPlaceholder = "Search raw materials",
            PrimaryActionText = "New Material",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Materials", Value = items.Count.ToString(), Caption = "Inventory items", Icon = "bi-box-seam", Tone = "primary" },
                new MetricCardViewModel { Title = "Low Stock", Value = items.Count(x => x.CurrentStock <= x.ReorderLevel).ToString(), Caption = "Needs reorder", Icon = "bi-exclamation-triangle", Tone = "warning" },
                new MetricCardViewModel { Title = "Total Stock", Value = items.Sum(x => x.CurrentStock).ToString("N0"), Caption = "On hand", Icon = "bi-stack", Tone = "success" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "SKU" },
                new ModuleColumnViewModel { Header = "Name" },
                new ModuleColumnViewModel { Header = "UoM" },
                new ModuleColumnViewModel { Header = "Stock" },
                new ModuleColumnViewModel { Header = "Reorder" },
                new ModuleColumnViewModel { Header = "Location" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.SKU, x.Name, x.UnitOfMeasure, x.CurrentStock.ToString("N0"), x.ReorderLevel.ToString("N0"), x.Location },
                Badge = x.CurrentStock <= x.ReorderLevel ? "Low stock" : "Healthy",
                BadgeTone = x.CurrentStock <= x.ReorderLevel ? "warning" : "success"
            }).ToList(),
            TotalItems = items.Count
        };
    }

    public async Task<ModulePageViewModel> BuildFinishedGoodsPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.FinishedGoods.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Finished Goods Inventory",
            Subtitle = "Track output, expiration windows, and stock adjustments.",
            SearchPlaceholder = "Search finished goods",
            PrimaryActionText = "Adjust Stock",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Finished Goods", Value = items.Count.ToString(), Caption = "Tracked products", Icon = "bi-bag-check", Tone = "primary" },
                new MetricCardViewModel { Title = "Units On Hand", Value = items.Sum(x => x.CurrentStock).ToString("N0"), Caption = "Current stock", Icon = "bi-archive", Tone = "success" },
                new MetricCardViewModel { Title = "Expiring Soon", Value = items.Count(x => x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14)).ToString(), Caption = "Next 14 days", Icon = "bi-clock-history", Tone = "warning" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Name" },
                new ModuleColumnViewModel { Header = "Batch" },
                new ModuleColumnViewModel { Header = "Qty" },
                new ModuleColumnViewModel { Header = "Location" },
                new ModuleColumnViewModel { Header = "Expires" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.Name, x.BatchNumber, x.CurrentStock.ToString("N0"), x.StorageLocation, x.ExpirationDate?.ToString("yyyy-MM-dd") ?? "N/A" },
                Badge = x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14) ? "Expiring" : "In stock",
                BadgeTone = x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14) ? "warning" : "success"
            }).ToList(),
            TotalItems = items.Count
        };
    }

    public async Task<ModulePageViewModel> BuildStockMovementsPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.StockMovements.AsNoTracking().OrderByDescending(x => x.MovementDate).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Stock Movements",
            Subtitle = "Review stock in, stock out, releases, and adjustments.",
            SearchPlaceholder = "Search movements",
            PrimaryActionText = "Record Movement",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Movements", Value = items.Count.ToString(), Caption = "Recent entries", Icon = "bi-arrow-left-right", Tone = "primary" },
                new MetricCardViewModel { Title = "Stock In", Value = items.Count(x => x.MovementType == InventoryMovementType.StockIn).ToString(), Caption = "Inbound", Icon = "bi-arrow-down-left-circle", Tone = "success" },
                new MetricCardViewModel { Title = "Stock Out", Value = items.Count(x => x.MovementType == InventoryMovementType.StockOut || x.MovementType == InventoryMovementType.ReleaseToProduction).ToString(), Caption = "Outbound", Icon = "bi-arrow-up-right-circle", Tone = "danger" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Item" },
                new ModuleColumnViewModel { Header = "Type" },
                new ModuleColumnViewModel { Header = "Qty" },
                new ModuleColumnViewModel { Header = "Reference" },
                new ModuleColumnViewModel { Header = "Date" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.ItemName, x.MovementType.ToString(), x.Quantity.ToString("N0"), x.ReferenceNumber, x.MovementDate.ToString("yyyy-MM-dd HH:mm") },
                Badge = x.MovementType.ToString(),
                BadgeTone = x.MovementType == InventoryMovementType.StockIn ? "success" : x.MovementType == InventoryMovementType.Adjustment ? "warning" : "danger"
            }).ToList(),
            TotalItems = items.Count
        };
    }
}

public sealed class ProductionService : IProductionService
{
    private readonly ApplicationDbContext _context;

    public ProductionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ModulePageViewModel> BuildPlansPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.ProductionPlans.AsNoTracking().Include(x => x.Product).OrderByDescending(x => x.StartDate).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Production Plans",
            Subtitle = "Create and review production schedules.",
            SearchPlaceholder = "Search production plans",
            PrimaryActionText = "New Plan",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Plans", Value = items.Count.ToString(), Caption = "Scheduled", Icon = "bi-kanban", Tone = "primary" },
                new MetricCardViewModel { Title = "Approved", Value = items.Count(x => x.Status is ProductionPlanStatus.Approved or ProductionPlanStatus.InProgress).ToString(), Caption = "Ready to run", Icon = "bi-check2-circle", Tone = "success" },
                new MetricCardViewModel { Title = "Drafts", Value = items.Count(x => x.Status == ProductionPlanStatus.Draft).ToString(), Caption = "Pending review", Icon = "bi-file-earmark-text", Tone = "warning" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Plan #" },
                new ModuleColumnViewModel { Header = "Product" },
                new ModuleColumnViewModel { Header = "Quantity" },
                new ModuleColumnViewModel { Header = "Start" },
                new ModuleColumnViewModel { Header = "End" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.PlanCode, x.Product?.Name ?? "Unknown", x.PlannedQuantity.ToString("N0"), x.StartDate.ToString("yyyy-MM-dd"), x.EndDate.ToString("yyyy-MM-dd") },
                Badge = x.Status.ToString(),
                BadgeTone = x.Status == ProductionPlanStatus.InProgress ? "primary" : x.Status == ProductionPlanStatus.Approved ? "success" : "warning"
            }).ToList(),
            TotalItems = items.Count
        };
    }

    public async Task<ModulePageViewModel> BuildWorkOrdersPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.WorkOrders.AsNoTracking().Include(x => x.ProductionPlan).ThenInclude(x => x!.Product).OrderByDescending(x => x.ScheduledDate).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Work Orders",
            Subtitle = "Track execution, assignment, and output.",
            SearchPlaceholder = "Search work orders",
            PrimaryActionText = "New Work Order",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Work Orders", Value = items.Count.ToString(), Caption = "Tracked", Icon = "bi-clipboard2-check", Tone = "primary" },
                new MetricCardViewModel { Title = "Open", Value = items.Count(x => x.Status is WorkOrderStatus.Draft or WorkOrderStatus.Planned or WorkOrderStatus.InProgress or WorkOrderStatus.OnHold).ToString(), Caption = "Active flow", Icon = "bi-play-circle", Tone = "success" },
                new MetricCardViewModel { Title = "Closed", Value = items.Count(x => x.Status == WorkOrderStatus.Completed).ToString(), Caption = "Completed", Icon = "bi-check2-all", Tone = "secondary" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Order #" },
                new ModuleColumnViewModel { Header = "Product" },
                new ModuleColumnViewModel { Header = "Qty" },
                new ModuleColumnViewModel { Header = "Schedule" },
                new ModuleColumnViewModel { Header = "Assigned To" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.WorkOrderNumber, x.ProductionPlan?.Product?.Name ?? "Unknown", x.Quantity.ToString("N0"), x.ScheduledDate.ToString("yyyy-MM-dd"), x.AssignedTo },
                Badge = x.Status.ToString(),
                BadgeTone = x.Status == WorkOrderStatus.InProgress ? "primary" : x.Status == WorkOrderStatus.Completed ? "success" : "warning"
            }).ToList(),
            TotalItems = items.Count
        };
    }

    public async Task<ModulePageViewModel> BuildTraceabilityPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.ProductionBatches.AsNoTracking().Include(x => x.Product).Include(x => x.WorkOrder).OrderByDescending(x => x.ProducedAt).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Batch Traceability",
            Subtitle = "Trace production output back to batches and work orders.",
            SearchPlaceholder = "Search batches",
            PrimaryActionText = "Register Batch",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Batches", Value = items.Count.ToString(), Caption = "Recorded", Icon = "bi-qr-code-scan", Tone = "primary" },
                new MetricCardViewModel { Title = "Active Trace", Value = items.Count(x => x.ExpirationDate is null || x.ExpirationDate > DateTime.UtcNow).ToString(), Caption = "Live batches", Icon = "bi-diagram-3", Tone = "success" },
                new MetricCardViewModel { Title = "Expiring", Value = items.Count(x => x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14)).ToString(), Caption = "Next 14 days", Icon = "bi-exclamation-circle", Tone = "warning" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Batch #" },
                new ModuleColumnViewModel { Header = "Product" },
                new ModuleColumnViewModel { Header = "Output" },
                new ModuleColumnViewModel { Header = "Produced" },
                new ModuleColumnViewModel { Header = "Trace Code" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.BatchNumber, x.Product?.Name ?? "Unknown", x.ProducedQuantity.ToString("N0"), x.ProducedAt.ToString("yyyy-MM-dd"), x.TraceabilityCode },
                Badge = x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14) ? "Expiring" : "Traceable",
                BadgeTone = x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14) ? "warning" : "success"
            }).ToList(),
            TotalItems = items.Count
        };
    }
}

public sealed class ProcurementService : IProcurementService
{
    private readonly ApplicationDbContext _context;

    public ProcurementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ModulePageViewModel> BuildSuppliersPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.Suppliers.AsNoTracking().OrderBy(x => x.Name).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Suppliers",
            Subtitle = "Manage vendor profiles, lead times, and service scores.",
            SearchPlaceholder = "Search suppliers",
            PrimaryActionText = "New Supplier",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Suppliers", Value = items.Count.ToString(), Caption = "Active vendors", Icon = "bi-truck", Tone = "primary" },
                new MetricCardViewModel { Title = "Average Lead Time", Value = items.Any() ? $"{items.Average(x => x.Phone.Length):N0} days" : "0 days", Caption = "Planning input", Icon = "bi-hourglass-split", Tone = "warning" },
                new MetricCardViewModel { Title = "Top Rated", Value = "4.8", Caption = "Service score", Icon = "bi-star", Tone = "success" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Name" },
                new ModuleColumnViewModel { Header = "Contact" },
                new ModuleColumnViewModel { Header = "Phone" },
                new ModuleColumnViewModel { Header = "Email" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.Name, x.ContactPerson, x.Phone, x.Email },
                Badge = x.IsActive ? "Active" : "Inactive",
                BadgeTone = x.IsActive ? "success" : "secondary"
            }).ToList(),
            TotalItems = items.Count
        };
    }

    public async Task<ModulePageViewModel> BuildPurchasesPageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.PurchaseTransactions.AsNoTracking().Include(x => x.Supplier).OrderByDescending(x => x.PurchasedOn).Take(12).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Purchase Transactions",
            Subtitle = "Monitor purchasing, receiving, and supplier spend.",
            SearchPlaceholder = "Search purchases",
            PrimaryActionText = "New Purchase",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Purchases", Value = items.Count.ToString(), Caption = "Logged transactions", Icon = "bi-receipt", Tone = "primary" },
                new MetricCardViewModel { Title = "Received", Value = items.Count(x => x.Status == PurchaseStatus.Received).ToString(), Caption = "Closed orders", Icon = "bi-inbox-fill", Tone = "success" },
                new MetricCardViewModel { Title = "Spend", Value = items.Sum(x => x.TotalAmount).ToString("C"), Caption = "Total procurement", Icon = "bi-cash-coin", Tone = "warning" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Transaction #" },
                new ModuleColumnViewModel { Header = "Supplier" },
                new ModuleColumnViewModel { Header = "Date" },
                new ModuleColumnViewModel { Header = "Amount" }
            },
            Rows = items.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.PurchaseNumber, x.Supplier?.Name ?? "Unknown", x.PurchasedOn.ToString("yyyy-MM-dd"), x.TotalAmount.ToString("C") },
                Badge = x.Status.ToString(),
                BadgeTone = x.Status == PurchaseStatus.Received ? "success" : x.Status == PurchaseStatus.Ordered ? "warning" : "secondary"
            }).ToList(),
            TotalItems = items.Count
        };
    }
}

public sealed class AdministrationService : IAdministrationService
{
    private const int DefaultPageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public AdministrationService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<ModulePageViewModel> BuildUsersPageAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.OrderBy(x => x.Email).Take(12).ToListAsync(cancellationToken);
        var rows = new List<ModuleRowViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new ModuleRowViewModel
            {
                Cells = new[] { user.UserName ?? string.Empty, user.Email ?? string.Empty, user.FullName, string.Join(", ", roles) },
                Badge = user.LockoutEnabled ? "Locked" : "Active",
                BadgeTone = user.LockoutEnabled ? "warning" : "success"
            });
        }

        return new ModulePageViewModel
        {
            Title = "User Management",
            Subtitle = "Manage operators, roles, and access control.",
            SearchPlaceholder = "Search users",
            PrimaryActionText = "Invite User",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Users", Value = users.Count.ToString(), Caption = "System accounts", Icon = "bi-people", Tone = "primary" },
                new MetricCardViewModel { Title = "Roles", Value = (await _roleManager.Roles.CountAsync(cancellationToken)).ToString(), Caption = "Configured roles", Icon = "bi-shield-lock", Tone = "warning" },
                new MetricCardViewModel { Title = "Admins", Value = users.Count(x => x.Email != null && x.Email.Contains("admin", StringComparison.OrdinalIgnoreCase)).ToString(), Caption = "Privileged access", Icon = "bi-person-badge", Tone = "success" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Username" },
                new ModuleColumnViewModel { Header = "Email" },
                new ModuleColumnViewModel { Header = "Full Name" },
                new ModuleColumnViewModel { Header = "Roles" }
            },
            Rows = rows,
            TotalItems = users.Count
        };
    }

    public async Task<ModulePageViewModel> BuildRolesPageAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return new ModulePageViewModel
        {
            Title = "Roles & Permissions",
            Subtitle = "Configure RBAC for ERP operations.",
            SearchPlaceholder = "Search roles",
            PrimaryActionText = "New Role",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Roles", Value = roles.Count.ToString(), Caption = "Permission sets", Icon = "bi-shield-check", Tone = "primary" },
                new MetricCardViewModel { Title = "Core", Value = AppRoles.All.Length.ToString(), Caption = "Seeded roles", Icon = "bi-award", Tone = "warning" },
                new MetricCardViewModel { Title = "Extensible", Value = "Yes", Caption = "Custom policies", Icon = "bi-sliders", Tone = "success" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Role" },
                new ModuleColumnViewModel { Header = "Normalized Name" }
            },
            Rows = roles.Select(x => new ModuleRowViewModel
            {
                Cells = new[] { x.Name ?? string.Empty, x.NormalizedName ?? string.Empty },
                Badge = "Role",
                BadgeTone = "primary"
            }).ToList(),
            TotalItems = roles.Count
        };
    }

    public Task<ModulePageViewModel> BuildSettingsPageAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ModulePageViewModel
        {
            Title = "System Settings",
            Subtitle = "Configure ERP defaults, alert thresholds, and workflow behavior.",
            SearchPlaceholder = "Search settings",
            PrimaryActionText = "Save Changes",
            Metrics = new[]
            {
                new MetricCardViewModel { Title = "Audit Trail", Value = "On", Caption = "Track all changes", Icon = "bi-journal-text", Tone = "success" },
                new MetricCardViewModel { Title = "Alerts", Value = "Enabled", Caption = "Low stock & expiry", Icon = "bi-bell", Tone = "warning" },
                new MetricCardViewModel { Title = "Approvals", Value = "Role-based", Caption = "Workflow control", Icon = "bi-diagram-3", Tone = "primary" }
            },
            Columns = new[]
            {
                new ModuleColumnViewModel { Header = "Setting" },
                new ModuleColumnViewModel { Header = "Value" }
            },
            Rows = new[]
            {
                new ModuleRowViewModel { Cells = new[] { "Company Name", "MeatPro Manufacturing ERP" }, Badge = "Brand", BadgeTone = "primary" },
                new ModuleRowViewModel { Cells = new[] { "Low Stock Threshold", "7 days" }, Badge = "Inventory", BadgeTone = "warning" },
                new ModuleRowViewModel { Cells = new[] { "Production Approval", "Required" }, Badge = "Workflow", BadgeTone = "success" }
            },
            TotalItems = 3
        });
    }

    public async Task<UserIndexViewModel> BuildUserIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = sort?.Trim().ToLowerInvariant() switch
        {
            "username" => "username",
            "username_desc" => "username_desc",
            "email" => "email",
            "email_desc" => "email_desc",
            "name" => "name",
            "name_desc" => "name_desc",
            _ => "username"
        };

        IQueryable<ApplicationUser> query = _userManager.Users;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                (x.UserName != null && EF.Functions.Like(x.UserName, $"%{normalizedSearch}%")) ||
                (x.Email != null && EF.Functions.Like(x.Email, $"%{normalizedSearch}%")) ||
                EF.Functions.Like(x.FullName, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Department, $"%{normalizedSearch}%"));
        }

        query = normalizedSort switch
        {
            "username" => query.OrderBy(x => x.UserName ?? string.Empty),
            "username_desc" => query.OrderByDescending(x => x.UserName ?? string.Empty),
            "email" => query.OrderBy(x => x.Email ?? string.Empty),
            "email_desc" => query.OrderByDescending(x => x.Email ?? string.Empty),
            "name" => query.OrderBy(x => x.FullName),
            "name_desc" => query.OrderByDescending(x => x.FullName),
            _ => query.OrderBy(x => x.UserName ?? string.Empty)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = new List<UserListItemViewModel>();
        foreach (var user in pageEntities)
        {
            var roles = await _userManager.GetRolesAsync(user);
            pageItems.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Department = user.Department,
                Roles = string.Join(", ", roles),
                IsLockedOut = await _userManager.IsLockedOutAsync(user)
            });
        }

        var now = DateTimeOffset.UtcNow;
        var lockedCount = await query.CountAsync(x => x.LockoutEnd != null && x.LockoutEnd >= now, cancellationToken);
        var activeCount = totalItems - lockedCount;

        return new UserIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveCount = activeCount,
            LockedCount = lockedCount
        };
    }

    public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var isLockedOut = await _userManager.IsLockedOutAsync(user);

        return new UserDetailsViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Department = user.Department,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToList(),
            IsLockedOut = isLockedOut
        };
    }

    public async Task<UserFormViewModel?> GetUserEditAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return null;

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync(cancellationToken);

        return new UserFormViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Department = user.Department,
            PhoneNumber = user.PhoneNumber,
            IsLockedOut = await _userManager.IsLockedOutAsync(user),
            AvailableRoles = allRoles.Select(r => new RoleOptionViewModel
            {
                Name = r.Name ?? string.Empty,
                IsSelected = userRoles.Contains(r.Name ?? string.Empty)
            }).ToList()
        };
    }

    public async Task<UserDetailsViewModel?> GetUserDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetUserDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsUserNameInUseAsync(string userName, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.Trim();
        return await _userManager.Users.AnyAsync(x => x.UserName == normalizedUserName && x.Id != (excludeId ?? string.Empty), cancellationToken);
    }

    public async Task<bool> IsEmailInUseAsync(string email, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        return await _userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail && x.Id != (excludeId ?? string.Empty), cancellationToken);
    }

    public async Task<string> CreateUserAsync(UserFormViewModel model, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            Department = model.Department.Trim(),
            PhoneNumber = model.PhoneNumber?.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (model.IsLockedOut)
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        foreach (var role in model.AvailableRoles.Where(r => r.IsSelected))
        {
            await _userManager.AddToRoleAsync(user, role.Name);
        }

        await _auditTrail.LogAsync("Admin", "Create", "User", user.Id, null,
            null, new { user.UserName, user.Email, user.FullName }, cancellationToken: cancellationToken);

        return user.Id;
    }

    public async Task<bool> UpdateUserAsync(string id, UserFormViewModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return false;

        var oldValues = new { user.UserName, user.Email, user.FullName, user.Department };

        user.UserName = model.UserName.Trim();
        user.Email = model.Email.Trim();
        user.FullName = model.FullName.Trim();
        user.Department = model.Department.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        if (model.IsLockedOut)
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var selectedRoles = model.AvailableRoles.Where(r => r.IsSelected).Select(r => r.Name).ToList();

        await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(selectedRoles));
        await _userManager.AddToRolesAsync(user, selectedRoles.Except(currentRoles));

        await _auditTrail.LogAsync("Admin", "Update", "User", id, null,
            oldValues, new { user.UserName, user.Email, user.FullName }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return false;

        var oldValues = new { user.UserName, user.Email };
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _auditTrail.LogAsync("Admin", "Delete", "User", id, null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<RoleIndexViewModel> BuildRoleIndexAsync(string? search, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search?.Trim() ?? string.Empty;

        IQueryable<IdentityRole> query = _roleManager.Roles;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x => x.Name != null && EF.Functions.Like(x.Name, $"%{normalizedSearch}%"));
        }

        query = query.OrderBy(x => x.Name);

        var roles = await query.ToListAsync(cancellationToken);

        var items = new List<RoleListItemViewModel>();
        foreach (var role in roles)
        {
            var userCount = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            items.Add(new RoleListItemViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = userCount.Count
            });
        }

        return new RoleIndexViewModel
        {
            Search = normalizedSearch,
            Items = items,
            TotalItems = roles.Count
        };
    }

    public async Task<RoleDetailsViewModel?> GetRoleDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return null;

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

        return new RoleDetailsViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Users = usersInRole.Select(x => $"{x.FullName} ({x.Email})").ToList(),
        };
    }

    public async Task<RoleFormViewModel?> GetRoleEditAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return null;

        return new RoleFormViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty
        };
    }

    public async Task<RoleDetailsViewModel?> GetRoleDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetRoleDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsRoleNameInUseAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return await _roleManager.Roles.AnyAsync(x => x.Name == normalizedName && x.Id != (excludeId ?? string.Empty), cancellationToken);
    }

    public async Task<string> CreateRoleAsync(RoleFormViewModel model, CancellationToken cancellationToken = default)
    {
        var role = new IdentityRole(model.Name.Trim());
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _auditTrail.LogAsync("Admin", "Create", "Role", role.Id, null,
            null, new { role.Name }, cancellationToken: cancellationToken);

        return role.Id;
    }

    public async Task<bool> UpdateRoleAsync(string id, RoleFormViewModel model, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return false;

        var oldName = role.Name;
        role.Name = model.Name.Trim();

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _auditTrail.LogAsync("Admin", "Update", "Role", id, null,
            new { Name = oldName }, new { role.Name }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return false;

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        if (usersInRole.Count > 0)
        {
            throw new InvalidOperationException($"Cannot delete role '{role.Name}' — it has {usersInRole.Count} user(s) assigned.");
        }

        var oldValues = new { role.Name };
        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _auditTrail.LogAsync("Admin", "Delete", "Role", id, null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }
}