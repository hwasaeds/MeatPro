using MeatPro.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<FinishedGood> FinishedGoods => Set<FinishedGood>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ProductionPlan> ProductionPlans => Set<ProductionPlan>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseTransaction> PurchaseTransactions => Set<PurchaseTransaction>();
    public DbSet<NotificationItem> Notifications => Set<NotificationItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.SKU).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SKU).HasMaxLength(60).IsRequired();
            entity.Property(x => x.StandardYield).HasPrecision(18, 2);
            entity.Property(x => x.ReorderLevel).HasPrecision(18, 2);
            entity.Property(x => x.SellingPrice).HasPrecision(18, 2);
        });

        builder.Entity<RawMaterial>(entity =>
        {
            entity.HasIndex(x => x.SKU).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SKU).HasMaxLength(60).IsRequired();
            entity.Property(x => x.CurrentStock).HasPrecision(18, 2);
            entity.Property(x => x.ReorderLevel).HasPrecision(18, 2);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
        });

        builder.Entity<FinishedGood>(entity =>
        {
            entity.HasIndex(x => x.SKU).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SKU).HasMaxLength(60).IsRequired();
            entity.Property(x => x.CurrentStock).HasPrecision(18, 2);
            entity.Property(x => x.ReorderLevel).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        builder.Entity<ProductionPlan>(entity =>
        {
            entity.HasIndex(x => x.PlanCode).IsUnique();
            entity.Property(x => x.PlanCode).HasMaxLength(60).IsRequired();
            entity.Property(x => x.PlannedQuantity).HasPrecision(18, 2);
        });

        builder.Entity<WorkOrder>(entity =>
        {
            entity.HasIndex(x => x.WorkOrderNumber).IsUnique();
            entity.Property(x => x.WorkOrderNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.OutputQuantity).HasPrecision(18, 2);
        });

        builder.Entity<ProductionBatch>(entity =>
        {
            entity.HasIndex(x => x.BatchNumber).IsUnique();
            entity.Property(x => x.BatchNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.TraceabilityCode).HasMaxLength(120);
            entity.Property(x => x.RawMaterialConsumed).HasPrecision(18, 2);
            entity.Property(x => x.ProducedQuantity).HasPrecision(18, 2);
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        builder.Entity<PurchaseTransaction>(entity =>
        {
            entity.HasIndex(x => x.PurchaseNumber).IsUnique();
            entity.Property(x => x.PurchaseNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        builder.Entity<NotificationItem>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Module).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(150).IsRequired();
        });
    }
}