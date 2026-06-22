using MeatPro.Models;
using MySqlConnector;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@meatpro.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                Department = "Administration"
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRolesAsync(admin, AppRoles.All);
        }

        if (await context.Products.AnyAsync())
        {
            return;
        }

        var categoryFresh = new ProductCategory { Name = "Fresh Cuts", Description = "Fresh retail cuts" };
        var categoryProcessed = new ProductCategory { Name = "Processed", Description = "Cooked and packaged meat products" };

        var product1 = new Product { Name = "Premium Beef Sausage", SKU = "PRD-001", ProductCategory = categoryProcessed, Description = "Smoked sausage pack", UnitOfMeasure = "pack", StandardYield = 0.92m, ReorderLevel = 120, SellingPrice = 5.95m };
        var product2 = new Product { Name = "Chicken Fillet", SKU = "PRD-002", ProductCategory = categoryFresh, Description = "Retail chicken fillet", UnitOfMeasure = "kg", StandardYield = 0.96m, ReorderLevel = 100, SellingPrice = 7.30m };

        var raw1 = new RawMaterial { Name = "Beef Trim", SKU = "RM-001", CurrentStock = 460, ReorderLevel = 180, UnitCost = 4.30m, UnitOfMeasure = "kg", Location = "Cold Room A", SupplierName = "North Farm Ltd" };
        var raw2 = new RawMaterial { Name = "Chicken Breast", SKU = "RM-002", CurrentStock = 260, ReorderLevel = 150, UnitCost = 3.10m, UnitOfMeasure = "kg", Location = "Cold Room B", SupplierName = "Green Fields Poultry" };
        var raw3 = new RawMaterial { Name = "Spice Mix", SKU = "RM-003", CurrentStock = 34, ReorderLevel = 50, UnitCost = 10.25m, UnitOfMeasure = "kg", Location = "Dry Store", SupplierName = "SpiceHub" };

        var finished1 = new FinishedGood { Name = "Smoked Beef Sausage", SKU = "FG-001", BatchNumber = "BAT-1001", CurrentStock = 210, ReorderLevel = 80, UnitPrice = 6.95m, StorageLocation = "FG A1", ExpirationDate = DateTime.UtcNow.AddDays(30) };
        var finished2 = new FinishedGood { Name = "Chicken Fillet Pack", SKU = "FG-002", BatchNumber = "BAT-1002", CurrentStock = 96, ReorderLevel = 60, UnitPrice = 7.80m, StorageLocation = "FG B2", ExpirationDate = DateTime.UtcNow.AddDays(18) };

        var supplier1 = new Supplier { Name = "North Farm Ltd", ContactPerson = "Daniel White", Phone = "+1 555 200 101", Email = "sales@northfarm.local", Address = "Industrial Zone 5" };
        var supplier2 = new Supplier { Name = "Green Fields Poultry", ContactPerson = "Mina Clark", Phone = "+1 555 200 202", Email = "orders@greenfields.local", Address = "Farm District 11" };

        var plan1 = new ProductionPlan
        {
            PlanCode = "PLAN-2026-001",
            Product = product1,
            PlannedQuantity = 500,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            Status = ProductionPlanStatus.InProgress,
            CreatedByUser = adminEmail,
            Notes = "Priority plan for retail contract"
        };

        var workOrder1 = new WorkOrder
        {
            WorkOrderNumber = "WO-2026-001",
            ProductionPlan = plan1,
            ScheduledDate = DateTime.UtcNow.Date.AddDays(1),
            Quantity = 500,
            Status = WorkOrderStatus.InProgress,
            AssignedTo = "Production Line 1",
            OutputQuantity = 180,
            Notes = "First run in progress"
        };

        var batch1 = new ProductionBatch
        {
            BatchNumber = "BATCH-2026-0001",
            WorkOrder = workOrder1,
            Product = product1,
            TraceabilityCode = "TRACE-2026-0001",
            RawMaterialConsumed = 220,
            ProducedQuantity = 180,
            ProducedAt = DateTime.UtcNow.AddHours(-6),
            ExpirationDate = DateTime.UtcNow.AddDays(45),
            Notes = "Traceable output batch"
        };

        var purchase1 = new PurchaseTransaction
        {
            PurchaseNumber = "PO-2026-0001",
            Supplier = supplier1,
            PurchasedOn = DateTime.UtcNow.AddDays(-3),
            Status = PurchaseStatus.Received,
            TotalAmount = 4250m,
            ReceivedOn = DateTime.UtcNow.AddDays(-2),
            Notes = "Weekly beef procurement"
        };

        var stockMovements = new List<StockMovement>
        {
            new() { ItemName = raw1.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.StockIn, Quantity = 800, UnitCost = 4.2m, ReferenceNumber = purchase1.PurchaseNumber, Notes = "Goods received", MovementDate = DateTime.UtcNow.AddDays(-2), PerformedBy = adminEmail },
            new() { ItemName = raw3.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.ReleaseToProduction, Quantity = 12, UnitCost = 10.2m, ReferenceNumber = workOrder1.WorkOrderNumber, Notes = "Materials released", MovementDate = DateTime.UtcNow.AddHours(-8), PerformedBy = adminEmail }
        };

        var notifications = new List<NotificationItem>
        {
            new() { Title = "Low Stock Alert", Message = "Spice Mix is below reorder level.", Type = NotificationType.Warning, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) },
            new() { Title = "Pending Work Order", Message = "WO-2026-001 is currently in progress.", Type = NotificationType.Info, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) }
        };

        context.AddRange(categoryFresh, categoryProcessed);
        context.AddRange(product1, product2);
        context.AddRange(raw1, raw2, raw3);
        context.AddRange(finished1, finished2);
        context.AddRange(supplier1, supplier2);
        context.AddRange(plan1);
        context.AddRange(workOrder1);
        context.AddRange(batch1);
        context.AddRange(purchase1);
        context.AddRange(stockMovements);
        context.AddRange(notifications);

        await context.SaveChangesAsync();
    }
}

public static class MySqlBootstrapper
{
    public static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The MySQL connection string must include a database name.");
        }

        builder.Database = string.Empty;

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await command.ExecuteNonQueryAsync();
    }
}