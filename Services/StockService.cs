using MeatPro.Data;
using MeatPro.Models;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IStockService
{
    Task<(bool Success, string Message)> StockInRawMaterialAsync(int rawMaterialId, decimal quantity, string? referenceNumber, string? notes, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> StockOutRawMaterialAsync(int rawMaterialId, decimal quantity, string? referenceNumber, string? notes, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReleaseToProductionAsync(int rawMaterialId, decimal quantity, string workOrderNumber, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AdjustFinishedGoodAsync(int finishedGoodId, decimal newQuantity, string? reason, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RecordProductionOutputAsync(int finishedGoodId, decimal quantity, string batchNumber, string performedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(int count = 20, CancellationToken cancellationToken = default);
}

public sealed class StockService : IStockService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public StockService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<(bool Success, string Message)> StockInRawMaterialAsync(int rawMaterialId, decimal quantity, string? referenceNumber, string? notes, string performedBy, CancellationToken cancellationToken = default)
    {
        var material = await _context.RawMaterials.FirstOrDefaultAsync(x => x.Id == rawMaterialId, cancellationToken);
        if (material is null) return (false, "Raw material not found.");
        if (quantity <= 0) return (false, "Quantity must be positive.");

        var oldStock = material.CurrentStock;
        material.CurrentStock += quantity;
        material.UpdatedAtUtc = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            ItemType = "Raw Material",
            ItemName = material.Name,
            MovementType = InventoryMovementType.StockIn,
            Quantity = quantity,
            UnitCost = material.UnitCost,
            ReferenceNumber = referenceNumber ?? string.Empty,
            Notes = notes ?? string.Empty,
            MovementDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            IsApproved = true
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditTrail.LogAsync("Inventory", "StockIn", "RawMaterial", rawMaterialId.ToString(), performedBy,
            new { CurrentStock = oldStock }, new { CurrentStock = material.CurrentStock }, cancellationToken: cancellationToken);

        return (true, $"{quantity:N2} {material.UnitOfMeasure} of {material.Name} added to stock.");
    }

    public async Task<(bool Success, string Message)> StockOutRawMaterialAsync(int rawMaterialId, decimal quantity, string? referenceNumber, string? notes, string performedBy, CancellationToken cancellationToken = default)
    {
        var material = await _context.RawMaterials.FirstOrDefaultAsync(x => x.Id == rawMaterialId, cancellationToken);
        if (material is null) return (false, "Raw material not found.");
        if (quantity <= 0) return (false, "Quantity must be positive.");
        if (material.CurrentStock < quantity) return (false, $"Insufficient stock. Available: {material.CurrentStock:N2} {material.UnitOfMeasure}.");

        var oldStock = material.CurrentStock;
        material.CurrentStock -= quantity;
        material.UpdatedAtUtc = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            ItemType = "Raw Material",
            ItemName = material.Name,
            MovementType = InventoryMovementType.StockOut,
            Quantity = quantity,
            UnitCost = material.UnitCost,
            ReferenceNumber = referenceNumber ?? string.Empty,
            Notes = notes ?? string.Empty,
            MovementDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            IsApproved = true
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditTrail.LogAsync("Inventory", "StockOut", "RawMaterial", rawMaterialId.ToString(), performedBy,
            new { CurrentStock = oldStock }, new { CurrentStock = material.CurrentStock }, cancellationToken: cancellationToken);

        return (true, $"{quantity:N2} {material.UnitOfMeasure} of {material.Name} removed from stock.");
    }

    public async Task<(bool Success, string Message)> ReleaseToProductionAsync(int rawMaterialId, decimal quantity, string workOrderNumber, string performedBy, CancellationToken cancellationToken = default)
    {
        var material = await _context.RawMaterials.FirstOrDefaultAsync(x => x.Id == rawMaterialId, cancellationToken);
        if (material is null) return (false, "Raw material not found.");
        if (quantity <= 0) return (false, "Quantity must be positive.");
        if (material.CurrentStock < quantity) return (false, $"Insufficient stock. Available: {material.CurrentStock:N2} {material.UnitOfMeasure}.");

        var oldStock = material.CurrentStock;
        material.CurrentStock -= quantity;
        material.UpdatedAtUtc = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            ItemType = "Raw Material",
            ItemName = material.Name,
            MovementType = InventoryMovementType.ReleaseToProduction,
            Quantity = quantity,
            UnitCost = material.UnitCost,
            ReferenceNumber = workOrderNumber,
            Notes = $"Released to work order {workOrderNumber}",
            MovementDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            IsApproved = true
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditTrail.LogAsync("Production", "ReleaseToProduction", "RawMaterial", rawMaterialId.ToString(), performedBy,
            new { CurrentStock = oldStock }, new { CurrentStock = material.CurrentStock }, cancellationToken: cancellationToken);

        return (true, $"{quantity:N2} {material.UnitOfMeasure} of {material.Name} released to {workOrderNumber}.");
    }

    public async Task<(bool Success, string Message)> AdjustFinishedGoodAsync(int finishedGoodId, decimal newQuantity, string? reason, string performedBy, CancellationToken cancellationToken = default)
    {
        var fg = await _context.FinishedGoods.FirstOrDefaultAsync(x => x.Id == finishedGoodId, cancellationToken);
        if (fg is null) return (false, "Finished good not found.");
        if (newQuantity < 0) return (false, "Stock cannot be negative.");

        var oldStock = fg.CurrentStock;
        var difference = newQuantity - oldStock;

        fg.CurrentStock = newQuantity;
        fg.UpdatedAtUtc = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            ItemType = "Finished Good",
            ItemName = fg.Name,
            MovementType = InventoryMovementType.Adjustment,
            Quantity = Math.Abs(difference),
            UnitCost = fg.UnitPrice,
            ReferenceNumber = "ADJUST",
            Notes = reason ?? $"Adjusted from {oldStock:N0} to {newQuantity:N0}",
            MovementDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            IsApproved = true
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditTrail.LogAsync("Inventory", "Adjustment", "FinishedGood", finishedGoodId.ToString(), performedBy,
            new { CurrentStock = oldStock }, new { CurrentStock = fg.CurrentStock }, cancellationToken: cancellationToken);

        return (true, $"Finished good {fg.Name} adjusted from {oldStock:N0} to {newQuantity:N0}.");
    }

    public async Task<(bool Success, string Message)> RecordProductionOutputAsync(int finishedGoodId, decimal quantity, string batchNumber, string performedBy, CancellationToken cancellationToken = default)
    {
        var fg = await _context.FinishedGoods.FirstOrDefaultAsync(x => x.Id == finishedGoodId, cancellationToken);
        if (fg is null) return (false, "Finished good not found.");
        if (quantity <= 0) return (false, "Quantity must be positive.");

        var oldStock = fg.CurrentStock;
        fg.CurrentStock += quantity;
        fg.UpdatedAtUtc = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            ItemType = "Finished Good",
            ItemName = fg.Name,
            MovementType = InventoryMovementType.StockIn,
            Quantity = quantity,
            UnitCost = fg.UnitPrice,
            ReferenceNumber = batchNumber,
            Notes = $"Production output from batch {batchNumber}",
            MovementDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            IsApproved = true
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditTrail.LogAsync("Production", "ProductionOutput", "FinishedGood", finishedGoodId.ToString(), performedBy,
            new { CurrentStock = oldStock }, new { CurrentStock = fg.CurrentStock }, cancellationToken: cancellationToken);

        return (true, $"{quantity:N0} units of {fg.Name} recorded from batch {batchNumber}.");
    }

    public async Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        return await _context.StockMovements.AsNoTracking()
            .OrderByDescending(x => x.MovementDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
