using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IReportService
{
    Task<InventoryReportViewModel> BuildInventoryReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<ProductionReportViewModel> BuildProductionReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<ProcurementReportViewModel> BuildProcurementReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<string> ExportInventoryCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<string> ExportProductionCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<string> ExportProcurementCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<SalesReportViewModel> BuildSalesReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
    Task<string> ExportSalesCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default);
}

public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryReportViewModel> BuildInventoryReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(14);

        var rawMaterials = await _context.RawMaterials.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var finishedGoods = await _context.FinishedGoods.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var movements = await _context.StockMovements.AsNoTracking().OrderByDescending(x => x.MovementDate).Take(100).ToListAsync(cancellationToken);

        var items = new List<InventoryReportItem>();

        foreach (var rm in rawMaterials)
        {
            var isLowStock = rm.CurrentStock <= rm.ReorderLevel;
            var isExpiring = rm.ExpirationDate is not null && rm.ExpirationDate <= cutoff;
            items.Add(new InventoryReportItem
            {
                Type = "Raw Material",
                Name = rm.Name,
                SKU = rm.SKU,
                CurrentStock = rm.CurrentStock,
                ReorderLevel = rm.ReorderLevel,
                UnitValue = rm.UnitCost,
                TotalValue = rm.CurrentStock * rm.UnitCost,
                StorageLocation = rm.Location,
                ExpirationDate = rm.ExpirationDate,
                Status = isExpiring ? "Expiring" : isLowStock ? "Low stock" : "Healthy"
            });
        }

        foreach (var fg in finishedGoods)
        {
            var isLowStock = fg.CurrentStock <= fg.ReorderLevel;
            var isExpiring = fg.ExpirationDate is not null && fg.ExpirationDate <= cutoff;
            items.Add(new InventoryReportItem
            {
                Type = "Finished Good",
                Name = fg.Name,
                SKU = fg.SKU,
                CurrentStock = fg.CurrentStock,
                ReorderLevel = fg.ReorderLevel,
                UnitValue = fg.UnitPrice,
                TotalValue = fg.CurrentStock * fg.UnitPrice,
                StorageLocation = fg.StorageLocation,
                ExpirationDate = fg.ExpirationDate,
                Status = isExpiring ? "Expiring" : isLowStock ? "Low stock" : "Healthy"
            });
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            items = items.Where(x => x.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return new InventoryReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            StatusFilter = statusFilter,
            TotalMaterials = items.Count,
            TotalStockValue = items.Sum(x => x.TotalValue),
            LowStockCount = items.Count(x => x.Status == "Low stock"),
            ExpiringCount = items.Count(x => x.Status == "Expiring"),
            Items = items.OrderBy(x => x.Type).ThenBy(x => x.Name).ToList()
        };
    }

    public async Task<ProductionReportViewModel> BuildProductionReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        IQueryable<ProductionBatch> query = _context.ProductionBatches.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.WorkOrder).ThenInclude(x => x!.ProductionPlan);

        if (dateFrom.HasValue)
            query = query.Where(x => x.ProducedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.ProducedAt <= dateTo.Value.AddDays(1));

        var batches = await query.OrderByDescending(x => x.ProducedAt).ToListAsync(cancellationToken);

        var items = batches.Select(x => new ProductionReportItem
        {
            BatchNumber = x.BatchNumber,
            ProductName = x.Product?.Name ?? "Unknown",
            WorkOrderNumber = x.WorkOrder?.WorkOrderNumber ?? "Unknown",
            PlanCode = x.WorkOrder?.ProductionPlan?.PlanCode ?? "Unknown",
            ProducedQuantity = x.ProducedQuantity,
            RawMaterialConsumed = x.RawMaterialConsumed,
            ProducedAt = x.ProducedAt,
            Status = x.ExpirationDate is not null && x.ExpirationDate <= DateTime.UtcNow.AddDays(14) ? "Expiring" : "Good"
        }).ToList();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            items = items.Where(x => x.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var workOrders = await _context.WorkOrders.AsNoTracking().ToListAsync(cancellationToken);

        return new ProductionReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            StatusFilter = statusFilter,
            TotalWorkOrders = workOrders.Count,
            CompletedOrders = workOrders.Count(x => x.Status == WorkOrderStatus.Completed),
            TotalProduced = items.Sum(x => x.ProducedQuantity),
            TotalConsumed = items.Sum(x => x.RawMaterialConsumed),
            Items = items
        };
    }

    public async Task<ProcurementReportViewModel> BuildProcurementReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        IQueryable<PurchaseTransaction> query = _context.PurchaseTransactions.AsNoTracking()
            .Include(x => x.Supplier);

        if (dateFrom.HasValue)
            query = query.Where(x => x.PurchasedOn >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.PurchasedOn <= dateTo.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<PurchaseStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var purchases = await query.OrderByDescending(x => x.PurchasedOn).ToListAsync(cancellationToken);

        var items = purchases.Select(x => new ProcurementReportItem
        {
            PurchaseNumber = x.PurchaseNumber,
            SupplierName = x.Supplier?.Name ?? "Unknown",
            PurchasedOn = x.PurchasedOn,
            TotalAmount = x.TotalAmount,
            Status = x.Status.ToString(),
            ReceivedOn = x.ReceivedOn
        }).ToList();

        return new ProcurementReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            StatusFilter = statusFilter,
            TotalPurchases = items.Count,
            TotalSpend = items.Sum(x => x.TotalAmount),
            PendingCount = items.Count(x => x.Status is "Draft" or "Ordered" or "PartiallyReceived"),
            ReceivedCount = items.Count(x => x.Status == "Received"),
            Items = items
        };
    }

    public async Task<string> ExportInventoryCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var report = await BuildInventoryReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        var lines = new List<string>
        {
            "Type,Name,SKU,Current Stock,Reorder Level,Unit Value,Total Value,Location,Expiration Date,Status"
        };
        lines.AddRange(report.Items.Select(x =>
            $"\"{x.Type}\",\"{x.Name}\",\"{x.SKU}\",{x.CurrentStock},{x.ReorderLevel},{x.UnitValue},{x.TotalValue},\"{x.StorageLocation}\",\"{x.ExpirationDate?.ToString("yyyy-MM-dd") ?? ""}\",\"{x.Status}\""));
        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> ExportProductionCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var report = await BuildProductionReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        var lines = new List<string>
        {
            "Batch Number,Product,Work Order,Plan Code,Produced Qty,Raw Material Consumed,Yield %,Produced At,Status"
        };
        lines.AddRange(report.Items.Select(x =>
            $"\"{x.BatchNumber}\",\"{x.ProductName}\",\"{x.WorkOrderNumber}\",\"{x.PlanCode}\",{x.ProducedQuantity},{x.RawMaterialConsumed},{x.Yield},\"{x.ProducedAt:yyyy-MM-dd}\",\"{x.Status}\""));
        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> ExportProcurementCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var report = await BuildProcurementReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        var lines = new List<string>
        {
            "Purchase Number,Supplier,Purchase Date,Total Amount,Status,Received On"
        };
        lines.AddRange(report.Items.Select(x =>
            $"\"{x.PurchaseNumber}\",\"{x.SupplierName}\",\"{x.PurchasedOn:yyyy-MM-dd}\",{x.TotalAmount},\"{x.Status}\",\"{x.ReceivedOn?.ToString("yyyy-MM-dd") ?? ""}\""));
        return string.Join(Environment.NewLine, lines);
    }

    public async Task<SalesReportViewModel> BuildSalesReportAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(14);

        var query = _context.FinishedGoods.AsNoTracking().Where(x => x.IsActive);

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= dateTo.Value.AddDays(1));

        var finishedGoods = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        var items = finishedGoods.Select(fg =>
        {
            var isLowStock = fg.CurrentStock <= fg.ReorderLevel;
            var isExpiring = fg.ExpirationDate is not null && fg.ExpirationDate <= cutoff;
            return new SalesReportItem
            {
                ProductName = fg.Name,
                SKU = fg.SKU,
                BatchNumber = fg.BatchNumber,
                CurrentStock = fg.CurrentStock,
                ReorderLevel = fg.ReorderLevel,
                UnitPrice = fg.UnitPrice,
                TotalValue = fg.CurrentStock * fg.UnitPrice,
                StorageLocation = fg.StorageLocation,
                ExpirationDate = fg.ExpirationDate,
                Status = isExpiring ? "Expiring" : isLowStock ? "Low stock" : "Available"
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            items = items.Where(x => x.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return new SalesReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            StatusFilter = statusFilter,
            TotalItems = items.Count,
            TotalPotentialRevenue = items.Sum(x => x.TotalValue),
            TotalStockQuantity = items.Sum(x => x.CurrentStock),
            LowStockCount = items.Count(x => x.Status == "Low stock"),
            ExpiringCount = items.Count(x => x.Status == "Expiring"),
            Items = items
        };
    }

    public async Task<string> ExportSalesCsvAsync(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var report = await BuildSalesReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        var lines = new List<string>
        {
            "Product,SKU,Batch,Stock,Reorder,Unit Price,Total Value,Location,Expiration,Status"
        };
        lines.AddRange(report.Items.Select(x =>
            $"\"{x.ProductName}\",\"{x.SKU}\",\"{x.BatchNumber}\",{x.CurrentStock},{x.ReorderLevel},{x.UnitPrice},{x.TotalValue},\"{x.StorageLocation}\",\"{x.ExpirationDate?.ToString("yyyy-MM-dd") ?? ""}\",\"{x.Status}\""));
        return string.Join(Environment.NewLine, lines);
    }
}
