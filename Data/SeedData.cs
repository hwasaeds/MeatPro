using MeatPro.Models;
using Npgsql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Data;

public static class SeedData
{
    private static async Task<T> GetOrThrow<T>(Task<T?> task, string name) where T : class
    {
        return await task ?? throw new InvalidOperationException($"Required entity '{name}' not found in database. Drop and re-seed.");
    }

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

        var prodEmail = "production@meatpro.local";
        var prod = await userManager.FindByEmailAsync(prodEmail);
        if (prod is null)
        {
            prod = new ApplicationUser
            {
                UserName = prodEmail,
                Email = prodEmail,
                EmailConfirmed = true,
                FullName = "Juan dela Cruz",
                Department = "Production"
            };

            await userManager.CreateAsync(prod, "Prod@1234");
            await userManager.AddToRoleAsync(prod, AppRoles.ProductionManager);
        }

        var invEmail = "inventory@meatpro.local";
        var inv = await userManager.FindByEmailAsync(invEmail);
        if (inv is null)
        {
            inv = new ApplicationUser
            {
                UserName = invEmail,
                Email = invEmail,
                EmailConfirmed = true,
                FullName = "Maria Santos",
                Department = "Warehouse"
            };

            await userManager.CreateAsync(inv, "Inv@1234");
            await userManager.AddToRoleAsync(inv, AppRoles.InventoryPersonnel);
        }

        var procEmail = "procurement@meatpro.local";
        var proc = await userManager.FindByEmailAsync(procEmail);
        if (proc is null)
        {
            proc = new ApplicationUser
            {
                UserName = procEmail,
                Email = procEmail,
                EmailConfirmed = true,
                FullName = "Pedro Reyes",
                Department = "Procurement"
            };

            await userManager.CreateAsync(proc, "Proc@1234");
            await userManager.AddToRoleAsync(proc, AppRoles.ProcurementManager);
        }

        // ── Product Categories ──────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.ProductCategories, async () =>
        {
            context.AddRange(
                new ProductCategory { Name = "Fresh Cuts", Description = "Fresh retail cuts" },
                new ProductCategory { Name = "Processed", Description = "Cooked and packaged meat products" },
                new ProductCategory { Name = "Offal & Variety Meats", Description = "Organ meats and specialty cuts" },
                new ProductCategory { Name = "Marinated & Ready-to-Cook", Description = "Pre-seasoned meat products" },
                new ProductCategory { Name = "By-Products", Description = "Bones, hides, and rendering products" }
            );
            await context.SaveChangesAsync();
        });

        // ── Products (10) ───────────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.Products, async () =>
        {
            var catFresh = await GetOrThrow(context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "Fresh Cuts"), "Fresh Cuts");
            var catProcessed = await GetOrThrow(context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "Processed"), "Processed");
            var catOffal = await GetOrThrow(context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "Offal & Variety Meats"), "Offal & Variety Meats");
            var catMarinated = await GetOrThrow(context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "Marinated & Ready-to-Cook"), "Marinated & Ready-to-Cook");

            context.AddRange(
                new Product { Name = "Premium Beef Sausage", SKU = "PRD-001", ProductCategoryId = catProcessed.Id, Description = "Smoked sausage pack 500g", UnitOfMeasure = "pack", StandardYield = 0.92m, ReorderLevel = 120, SellingPrice = 5.95m },
                new Product { Name = "Chicken Fillet", SKU = "PRD-002", ProductCategoryId = catFresh.Id, Description = "Retail chicken fillet skinless", UnitOfMeasure = "kg", StandardYield = 0.96m, ReorderLevel = 100, SellingPrice = 7.30m },
                new Product { Name = "Beef Tenderloin Steak", SKU = "PRD-003", ProductCategoryId = catFresh.Id, Description = "Premium beef tenderloin cut", UnitOfMeasure = "kg", StandardYield = 0.85m, ReorderLevel = 50, SellingPrice = 18.50m },
                new Product { Name = "Pork Belly", SKU = "PRD-004", ProductCategoryId = catFresh.Id, Description = "Fresh pork belly slab", UnitOfMeasure = "kg", StandardYield = 0.90m, ReorderLevel = 80, SellingPrice = 9.25m },
                new Product { Name = "Chicken Nuggets", SKU = "PRD-005", ProductCategoryId = catProcessed.Id, Description = "Breaded chicken nuggets 1kg", UnitOfMeasure = "pack", StandardYield = 0.94m, ReorderLevel = 90, SellingPrice = 4.80m },
                new Product { Name = "Beef Liver", SKU = "PRD-006", ProductCategoryId = catOffal.Id, Description = "Fresh beef liver tray", UnitOfMeasure = "kg", StandardYield = 0.98m, ReorderLevel = 30, SellingPrice = 4.50m },
                new Product { Name = "Honey Glazed Ham", SKU = "PRD-007", ProductCategoryId = catProcessed.Id, Description = "Pre-cooked honey glazed ham", UnitOfMeasure = "kg", StandardYield = 0.88m, ReorderLevel = 40, SellingPrice = 12.00m },
                new Product { Name = "Chicken Satay Skewers", SKU = "PRD-008", ProductCategoryId = catMarinated.Id, Description = "Marinated chicken satay 6-pack", UnitOfMeasure = "pack", StandardYield = 0.95m, ReorderLevel = 60, SellingPrice = 6.50m },
                new Product { Name = "Pork Schnitzel", SKU = "PRD-009", ProductCategoryId = catMarinated.Id, Description = "Breaded pork schnitzel ready-to-cook", UnitOfMeasure = "pack", StandardYield = 0.93m, ReorderLevel = 70, SellingPrice = 7.75m },
                new Product { Name = "Bone-In Ribeye", SKU = "PRD-010", ProductCategoryId = catFresh.Id, Description = "Bone-in ribeye steak 300g", UnitOfMeasure = "piece", StandardYield = 0.80m, ReorderLevel = 45, SellingPrice = 14.95m }
            );
            await context.SaveChangesAsync();
        });

        // ── Raw Materials (12) ──────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.RawMaterials, async () =>
        {
            context.AddRange(
                new RawMaterial { Name = "Beef Trim", SKU = "RM-001", CurrentStock = 460, ReorderLevel = 180, UnitCost = 4.30m, UnitOfMeasure = "kg", Location = "Cold Room A", SupplierName = "North Farm Ltd" },
                new RawMaterial { Name = "Chicken Breast", SKU = "RM-002", CurrentStock = 260, ReorderLevel = 150, UnitCost = 3.10m, UnitOfMeasure = "kg", Location = "Cold Room B", SupplierName = "Green Fields Poultry" },
                new RawMaterial { Name = "Spice Mix", SKU = "RM-003", CurrentStock = 34, ReorderLevel = 50, UnitCost = 10.25m, UnitOfMeasure = "kg", Location = "Dry Store", SupplierName = "SpiceHub" },
                new RawMaterial { Name = "Pork Belly Block", SKU = "RM-004", CurrentStock = 320, ReorderLevel = 100, UnitCost = 5.80m, UnitOfMeasure = "kg", Location = "Cold Room A", SupplierName = "PorkSource Inc" },
                new RawMaterial { Name = "Breadcrumbs", SKU = "RM-005", CurrentStock = 85, ReorderLevel = 60, UnitCost = 2.50m, UnitOfMeasure = "kg", Location = "Dry Store", SupplierName = "BakeSupply Co" },
                new RawMaterial { Name = "Packaging Film", SKU = "RM-006", CurrentStock = 12, ReorderLevel = 20, UnitCost = 45.00m, UnitOfMeasure = "roll", Location = "Warehouse B", SupplierName = "PackPro Ltd" },
                new RawMaterial { Name = "Honey Glaze", SKU = "RM-007", CurrentStock = 40, ReorderLevel = 25, UnitCost = 8.90m, UnitOfMeasure = "litre", Location = "Cold Room B", SupplierName = "FlavorCraft Inc" },
                new RawMaterial { Name = "Chicken Thigh", SKU = "RM-008", CurrentStock = 180, ReorderLevel = 120, UnitCost = 2.80m, UnitOfMeasure = "kg", Location = "Cold Room B", SupplierName = "Green Fields Poultry" },
                new RawMaterial { Name = "Natural Casing", SKU = "RM-009", CurrentStock = 200, ReorderLevel = 100, UnitCost = 3.40m, UnitOfMeasure = "metre", Location = "Cold Room B", SupplierName = "North Farm Ltd" },
                new RawMaterial { Name = "Beef Ribs", SKU = "RM-010", CurrentStock = 150, ReorderLevel = 60, UnitCost = 6.20m, UnitOfMeasure = "kg", Location = "Cold Room A", SupplierName = "North Farm Ltd" },
                new RawMaterial { Name = "Salt & Cure Mix", SKU = "RM-011", CurrentStock = 22, ReorderLevel = 30, UnitCost = 5.75m, UnitOfMeasure = "kg", Location = "Dry Store", SupplierName = "SpiceHub" },
                new RawMaterial { Name = "BBQ Sauce", SKU = "RM-012", CurrentStock = 60, ReorderLevel = 40, UnitCost = 4.20m, UnitOfMeasure = "litre", Location = "Dry Store", SupplierName = "FlavorCraft Inc" }
            );
            await context.SaveChangesAsync();
        });

        // ── Finished Goods (8) ──────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.FinishedGoods, async () =>
        {
            context.AddRange(
                new FinishedGood { Name = "Smoked Beef Sausage", SKU = "FG-001", BatchNumber = "BAT-1001", CurrentStock = 210, ReorderLevel = 80, UnitPrice = 6.95m, StorageLocation = "FG A1", ExpirationDate = DateTime.UtcNow.AddDays(30) },
                new FinishedGood { Name = "Chicken Fillet Pack", SKU = "FG-002", BatchNumber = "BAT-1002", CurrentStock = 96, ReorderLevel = 60, UnitPrice = 7.80m, StorageLocation = "FG B2", ExpirationDate = DateTime.UtcNow.AddDays(18) },
                new FinishedGood { Name = "Beef Tenderloin Pack", SKU = "FG-003", BatchNumber = "BAT-1003", CurrentStock = 45, ReorderLevel = 20, UnitPrice = 19.50m, StorageLocation = "FG A3", ExpirationDate = DateTime.UtcNow.AddDays(14) },
                new FinishedGood { Name = "Pork Belly Slab", SKU = "FG-004", BatchNumber = "BAT-1004", CurrentStock = 78, ReorderLevel = 30, UnitPrice = 10.50m, StorageLocation = "FG A2", ExpirationDate = DateTime.UtcNow.AddDays(21) },
                new FinishedGood { Name = "Breaded Chicken Nuggets", SKU = "FG-005", BatchNumber = "BAT-1005", CurrentStock = 132, ReorderLevel = 50, UnitPrice = 5.50m, StorageLocation = "FG C1", ExpirationDate = DateTime.UtcNow.AddDays(120) },
                new FinishedGood { Name = "Honey Glazed Ham", SKU = "FG-006", BatchNumber = "BAT-1006", CurrentStock = 28, ReorderLevel = 15, UnitPrice = 13.25m, StorageLocation = "FG B1", ExpirationDate = DateTime.UtcNow.AddDays(60) },
                new FinishedGood { Name = "Chicken Satay 6-Pack", SKU = "FG-007", BatchNumber = "BAT-1007", CurrentStock = 55, ReorderLevel = 25, UnitPrice = 7.20m, StorageLocation = "FG C2", ExpirationDate = DateTime.UtcNow.AddDays(10) },
                new FinishedGood { Name = "Bone-In Ribeye 300g", SKU = "FG-008", BatchNumber = "BAT-1008", CurrentStock = 62, ReorderLevel = 20, UnitPrice = 16.50m, StorageLocation = "FG A4", ExpirationDate = DateTime.UtcNow.AddDays(7) }
            );
            await context.SaveChangesAsync();
        });

        // ── Suppliers (6) ───────────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.Suppliers, async () =>
        {
            context.AddRange(
                new Supplier { Name = "North Farm Ltd", ContactPerson = "Daniel White", Phone = "+1 555 200 101", Email = "sales@northfarm.local", Address = "Industrial Zone 5" },
                new Supplier { Name = "Green Fields Poultry", ContactPerson = "Mina Clark", Phone = "+1 555 200 202", Email = "orders@greenfields.local", Address = "Farm District 11" },
                new Supplier { Name = "PorkSource Inc", ContactPerson = "Robert Chen", Phone = "+1 555 200 303", Email = "orders@porkSource.local", Address = "Meatpacking Row 8" },
                new Supplier { Name = "SpiceHub", ContactPerson = "Lisa Park", Phone = "+1 555 200 404", Email = "sales@spicehub.local", Address = "Industrial Zone 3" },
                new Supplier { Name = "FlavorCraft Inc", ContactPerson = "Tom Briggs", Phone = "+1 555 200 505", Email = "info@flavorcraft.local", Address = "Food Tech Park 12" },
                new Supplier { Name = "BakeSupply Co", ContactPerson = "Emma Watson", Phone = "+1 555 200 606", Email = "orders@bakesupply.local", Address = "Grain District 2" }
            );
            await context.SaveChangesAsync();
        });

        // ── Production Plans (4) ────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.ProductionPlans, async () =>
        {
            var prod1 = await GetOrThrow(context.Products.FirstOrDefaultAsync(p => p.SKU == "PRD-001"), "PRD-001");
            var prod5 = await GetOrThrow(context.Products.FirstOrDefaultAsync(p => p.SKU == "PRD-005"), "PRD-005");
            var prod7 = await GetOrThrow(context.Products.FirstOrDefaultAsync(p => p.SKU == "PRD-007"), "PRD-007");
            var prod8 = await GetOrThrow(context.Products.FirstOrDefaultAsync(p => p.SKU == "PRD-008"), "PRD-008");

            context.AddRange(
                new ProductionPlan { PlanCode = "PLAN-2026-001", ProductId = prod1.Id, PlannedQuantity = 500, StartDate = DateTime.UtcNow.Date.AddDays(-3), EndDate = DateTime.UtcNow.Date.AddDays(4), Status = ProductionPlanStatus.InProgress, CreatedByUser = adminEmail, Notes = "Priority plan for retail contract" },
                new ProductionPlan { PlanCode = "PLAN-2026-002", ProductId = prod5.Id, PlannedQuantity = 1000, StartDate = DateTime.UtcNow.Date.AddDays(-1), EndDate = DateTime.UtcNow.Date.AddDays(9), Status = ProductionPlanStatus.Approved, CreatedByUser = adminEmail, Notes = "High-volume nugget run for Q3" },
                new ProductionPlan { PlanCode = "PLAN-2026-003", ProductId = prod7.Id, PlannedQuantity = 200, StartDate = DateTime.UtcNow.Date.AddDays(5), EndDate = DateTime.UtcNow.Date.AddDays(12), Status = ProductionPlanStatus.Draft, CreatedByUser = prodEmail, Notes = "Holiday ham batch — pending ingredient confirmation" },
                new ProductionPlan { PlanCode = "PLAN-2026-004", ProductId = prod8.Id, PlannedQuantity = 600, StartDate = DateTime.UtcNow.Date.AddDays(-7), EndDate = DateTime.UtcNow.Date, Status = ProductionPlanStatus.Completed, CreatedByUser = prodEmail, Notes = "Completed satay run for food service" }
            );
            await context.SaveChangesAsync();
        });

        // ── Work Orders (6) ─────────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.WorkOrders, async () =>
        {
            var plan1 = await GetOrThrow(context.ProductionPlans.FirstOrDefaultAsync(p => p.PlanCode == "PLAN-2026-001"), "PLAN-2026-001");
            var plan2 = await GetOrThrow(context.ProductionPlans.FirstOrDefaultAsync(p => p.PlanCode == "PLAN-2026-002"), "PLAN-2026-002");
            var plan4 = await GetOrThrow(context.ProductionPlans.FirstOrDefaultAsync(p => p.PlanCode == "PLAN-2026-004"), "PLAN-2026-004");

            context.AddRange(
                new WorkOrder { WorkOrderNumber = "WO-2026-001", ProductionPlanId = plan1.Id, ScheduledDate = DateTime.UtcNow.Date.AddDays(-2), Quantity = 250, Status = WorkOrderStatus.InProgress, AssignedTo = "Production Line 1", OutputQuantity = 180, Notes = "First run in progress — sausage links" },
                new WorkOrder { WorkOrderNumber = "WO-2026-002", ProductionPlanId = plan1.Id, ScheduledDate = DateTime.UtcNow.Date.AddDays(1), Quantity = 250, Status = WorkOrderStatus.Planned, AssignedTo = "Production Line 1", OutputQuantity = 0, Notes = "Second run — awaiting casing delivery" },
                new WorkOrder { WorkOrderNumber = "WO-2026-003", ProductionPlanId = plan2.Id, ScheduledDate = DateTime.UtcNow.Date, Quantity = 500, Status = WorkOrderStatus.Draft, AssignedTo = "Production Line 2", OutputQuantity = 0, Notes = "First nugget batch — calibrating breading line" },
                new WorkOrder { WorkOrderNumber = "WO-2026-004", ProductionPlanId = plan4.Id, ScheduledDate = DateTime.UtcNow.Date.AddDays(-6), Quantity = 300, Status = WorkOrderStatus.Completed, AssignedTo = "Production Line 3", OutputQuantity = 285, Notes = "Satay run completed ahead of schedule" },
                new WorkOrder { WorkOrderNumber = "WO-2026-005", ProductionPlanId = plan4.Id, ScheduledDate = DateTime.UtcNow.Date.AddDays(-4), Quantity = 300, Status = WorkOrderStatus.Completed, AssignedTo = "Production Line 3", OutputQuantity = 290, Notes = "Second satay run — all quality checks passed" },
                new WorkOrder { WorkOrderNumber = "WO-2026-006", ProductionPlanId = plan2.Id, ScheduledDate = DateTime.UtcNow.Date.AddDays(3), Quantity = 500, Status = WorkOrderStatus.Planned, AssignedTo = "Production Line 2", OutputQuantity = 0, Notes = "Second nugget batch — freezer space confirmed" }
            );
            await context.SaveChangesAsync();
        });

        // ── Production Batches (4) ──────────────────────────────────────
        await SeedIfEmptyAsync(context, context.ProductionBatches, async () =>
        {
            var prod1 = await GetOrThrow(context.Products.FirstOrDefaultAsync(p => p.SKU == "PRD-001"), "PRD-001");
            var prod8 = await GetOrThrow(context.Products.FirstOrDefaultAsync(p => p.SKU == "PRD-008"), "PRD-008");
            var wo1 = await GetOrThrow(context.WorkOrders.FirstOrDefaultAsync(w => w.WorkOrderNumber == "WO-2026-001"), "WO-2026-001");
            var wo4 = await GetOrThrow(context.WorkOrders.FirstOrDefaultAsync(w => w.WorkOrderNumber == "WO-2026-004"), "WO-2026-004");
            var wo5 = await GetOrThrow(context.WorkOrders.FirstOrDefaultAsync(w => w.WorkOrderNumber == "WO-2026-005"), "WO-2026-005");

            context.AddRange(
                new ProductionBatch { BatchNumber = "BATCH-2026-0001", WorkOrderId = wo1.Id, ProductId = prod1.Id, TraceabilityCode = "TRACE-2026-0001", RawMaterialConsumed = 220, ProducedQuantity = 180, ProducedAt = DateTime.UtcNow.AddHours(-6), ExpirationDate = DateTime.UtcNow.AddDays(45), Notes = "Sausage batch — grade A quality" },
                new ProductionBatch { BatchNumber = "BATCH-2026-0002", WorkOrderId = wo4.Id, ProductId = prod8.Id, TraceabilityCode = "TRACE-2026-0002", RawMaterialConsumed = 200, ProducedQuantity = 285, ProducedAt = DateTime.UtcNow.AddDays(-6), ExpirationDate = DateTime.UtcNow.AddDays(14), Notes = "Satay batch — food service spec" },
                new ProductionBatch { BatchNumber = "BATCH-2026-0003", WorkOrderId = wo5.Id, ProductId = prod8.Id, TraceabilityCode = "TRACE-2026-0003", RawMaterialConsumed = 195, ProducedQuantity = 290, ProducedAt = DateTime.UtcNow.AddDays(-4), ExpirationDate = DateTime.UtcNow.AddDays(14), Notes = "Satay batch — retail spec" },
                new ProductionBatch { BatchNumber = "BATCH-2026-0004", WorkOrderId = wo1.Id, ProductId = prod1.Id, TraceabilityCode = "TRACE-2026-0004", RawMaterialConsumed = 40, ProducedQuantity = 0, ProducedAt = DateTime.UtcNow.AddHours(-2), ExpirationDate = DateTime.UtcNow.AddDays(44), Notes = "Sausage batch — QC on hold" }
            );
            await context.SaveChangesAsync();
        });

        // ── Purchase Transactions (5) ───────────────────────────────────
        await SeedIfEmptyAsync(context, context.PurchaseTransactions, async () =>
        {
            var sup1 = await GetOrThrow(context.Suppliers.FirstOrDefaultAsync(s => s.Name == "North Farm Ltd"), "North Farm Ltd");
            var sup2 = await GetOrThrow(context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Green Fields Poultry"), "Green Fields Poultry");
            var sup3 = await GetOrThrow(context.Suppliers.FirstOrDefaultAsync(s => s.Name == "PorkSource Inc"), "PorkSource Inc");
            var sup4 = await GetOrThrow(context.Suppliers.FirstOrDefaultAsync(s => s.Name == "SpiceHub"), "SpiceHub");
            var sup5 = await GetOrThrow(context.Suppliers.FirstOrDefaultAsync(s => s.Name == "FlavorCraft Inc"), "FlavorCraft Inc");

            context.AddRange(
                new PurchaseTransaction { PurchaseNumber = "PO-2026-0001", SupplierId = sup1.Id, PurchasedOn = DateTime.UtcNow.AddDays(-5), Status = PurchaseStatus.Received, TotalAmount = 4250m, ReceivedOn = DateTime.UtcNow.AddDays(-3), Notes = "Weekly beef procurement" },
                new PurchaseTransaction { PurchaseNumber = "PO-2026-0002", SupplierId = sup2.Id, PurchasedOn = DateTime.UtcNow.AddDays(-4), Status = PurchaseStatus.Received, TotalAmount = 3100m, ReceivedOn = DateTime.UtcNow.AddDays(-2), Notes = "Chicken breast order" },
                new PurchaseTransaction { PurchaseNumber = "PO-2026-0003", SupplierId = sup3.Id, PurchasedOn = DateTime.UtcNow.AddDays(-2), Status = PurchaseStatus.PartiallyReceived, TotalAmount = 5800m, ReceivedOn = DateTime.UtcNow.AddDays(-1), Notes = "Pork belly blocks — partial delivery" },
                new PurchaseTransaction { PurchaseNumber = "PO-2026-0004", SupplierId = sup4.Id, PurchasedOn = DateTime.UtcNow.AddDays(-1), Status = PurchaseStatus.Ordered, TotalAmount = 1025m, ReceivedOn = null, Notes = "Spice mix and salt cure restock" },
                new PurchaseTransaction { PurchaseNumber = "PO-2026-0005", SupplierId = sup5.Id, PurchasedOn = DateTime.UtcNow, Status = PurchaseStatus.Draft, TotalAmount = 1680m, ReceivedOn = null, Notes = "Honey glaze & BBQ sauce — awaiting approval" }
            );
            await context.SaveChangesAsync();
        });

        // ── Stock Movements (10) ────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.StockMovements, async () =>
        {
            var raw1 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-001");
            var raw2 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-002");
            var raw3 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-003");
            var raw5 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-005");
            var raw6 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-006");
            var raw7 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-007");
            var raw8 = await context.RawMaterials.FirstAsync(r => r.SKU == "RM-008");
            var fg2 = await context.FinishedGoods.FirstAsync(f => f.SKU == "FG-002");
            var fg4 = await context.FinishedGoods.FirstAsync(f => f.SKU == "FG-004");
            var po1 = await context.PurchaseTransactions.FirstAsync(p => p.PurchaseNumber == "PO-2026-0001");
            var po2 = await context.PurchaseTransactions.FirstAsync(p => p.PurchaseNumber == "PO-2026-0002");
            var po5 = await context.PurchaseTransactions.FirstAsync(p => p.PurchaseNumber == "PO-2026-0005");
            var wo1 = await context.WorkOrders.FirstAsync(w => w.WorkOrderNumber == "WO-2026-001");
            var wo3 = await context.WorkOrders.FirstAsync(w => w.WorkOrderNumber == "WO-2026-003");

            context.AddRange(
                new StockMovement { ItemName = raw1.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.StockIn, Quantity = 800, UnitCost = 4.2m, ReferenceNumber = po1.PurchaseNumber, Notes = "Goods received — beef trim", MovementDate = DateTime.UtcNow.AddDays(-3), PerformedBy = adminEmail },
                new StockMovement { ItemName = raw2.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.StockIn, Quantity = 500, UnitCost = 3.0m, ReferenceNumber = po2.PurchaseNumber, Notes = "Goods received — chicken breast", MovementDate = DateTime.UtcNow.AddDays(-2), PerformedBy = invEmail },
                new StockMovement { ItemName = raw3.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.ReleaseToProduction, Quantity = 12, UnitCost = 10.2m, ReferenceNumber = wo1.WorkOrderNumber, Notes = "Materials released for sausage run", MovementDate = DateTime.UtcNow.AddHours(-8), PerformedBy = prodEmail },
                new StockMovement { ItemName = raw1.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.ReleaseToProduction, Quantity = 220, UnitCost = 4.3m, ReferenceNumber = wo1.WorkOrderNumber, Notes = "Beef trim to Line 1", MovementDate = DateTime.UtcNow.AddHours(-8), PerformedBy = prodEmail },
                new StockMovement { ItemName = raw8.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.StockIn, Quantity = 300, UnitCost = 2.7m, ReferenceNumber = po2.PurchaseNumber, Notes = "Chicken thigh received", MovementDate = DateTime.UtcNow.AddDays(-2), PerformedBy = invEmail },
                new StockMovement { ItemName = raw5.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.ReleaseToProduction, Quantity = 50, UnitCost = 2.5m, ReferenceNumber = wo3.WorkOrderNumber, Notes = "Breadcrumbs to Line 2", MovementDate = DateTime.UtcNow.AddHours(-4), PerformedBy = prodEmail },
                new StockMovement { ItemName = raw7.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.StockIn, Quantity = 100, UnitCost = 8.5m, ReferenceNumber = po5.PurchaseNumber, Notes = "Honey glaze stocked", MovementDate = DateTime.UtcNow, PerformedBy = invEmail },
                new StockMovement { ItemName = fg4.Name, ItemType = "Finished Good", MovementType = InventoryMovementType.StockOut, Quantity = 20, UnitCost = 10.5m, ReferenceNumber = "SALE-001", Notes = "Wholesale order — Pork Belly", MovementDate = DateTime.UtcNow.AddDays(-1), PerformedBy = invEmail },
                new StockMovement { ItemName = fg2.Name, ItemType = "Finished Good", MovementType = InventoryMovementType.StockOut, Quantity = 30, UnitCost = 7.8m, ReferenceNumber = "SALE-002", Notes = "Retail store delivery", MovementDate = DateTime.UtcNow.AddHours(-12), PerformedBy = invEmail },
                new StockMovement { ItemName = raw6.Name, ItemType = "Raw Material", MovementType = InventoryMovementType.Adjustment, Quantity = -3, UnitCost = 45.0m, ReferenceNumber = "ADJ-001", Notes = "Damaged roll written off", MovementDate = DateTime.UtcNow.AddDays(-5), PerformedBy = adminEmail }
            );
            await context.SaveChangesAsync();
        });

        // ── Notifications (6) ───────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.Notifications, async () =>
        {
            context.AddRange(
                new NotificationItem { Title = "Low Stock Alert", Message = "Spice Mix is below reorder level (34 / 50 kg).", Type = NotificationType.Warning, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) },
                new NotificationItem { Title = "Low Stock Alert", Message = "Packaging Film is below reorder level (12 / 20 rolls).", Type = NotificationType.Warning, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) },
                new NotificationItem { Title = "Low Stock Alert", Message = "Salt & Cure Mix is below reorder level (22 / 30 kg).", Type = NotificationType.Warning, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) },
                new NotificationItem { Title = "Pending Work Order", Message = "WO-2026-001 (Sausage) is currently in progress.", Type = NotificationType.Info, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) },
                new NotificationItem { Title = "Production Plan Approved", Message = "PLAN-2026-002 (Chicken Nuggets) has been approved.", Type = NotificationType.Success, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) },
                new NotificationItem { Title = "Expiring Soon", Message = "FG-008 (Bone-In Ribeye) expires in 7 days — prioritize sale.", Type = NotificationType.Danger, ExpiresAtUtc = DateTime.UtcNow.AddDays(8) }
            );
            await context.SaveChangesAsync();
        });

        // ── Audit Logs (5) ──────────────────────────────────────────────
        await SeedIfEmptyAsync(context, context.AuditLogs, async () =>
        {
            context.AddRange(
                new AuditLog { Module = "Products", Action = "Create", EntityName = "Product", EntityId = "PRD-001", Username = adminEmail, NewValues = "{\"Name\":\"Premium Beef Sausage\",\"SKU\":\"PRD-001\"}", IpAddress = "127.0.0.1" },
                new AuditLog { Module = "Raw Materials", Action = "Stock In", EntityName = "RawMaterial", EntityId = "RM-001", Username = invEmail, NewValues = "{\"Quantity\":800,\"Reference\":\"PO-2026-0001\"}", IpAddress = "127.0.0.1" },
                new AuditLog { Module = "Production", Action = "Create", EntityName = "ProductionPlan", EntityId = "PLAN-2026-001", Username = adminEmail, NewValues = "{\"PlanCode\":\"PLAN-2026-001\",\"Status\":\"InProgress\"}", IpAddress = "127.0.0.1" },
                new AuditLog { Module = "Procurement", Action = "Create", EntityName = "PurchaseTransaction", EntityId = "PO-2026-0003", Username = procEmail, NewValues = "{\"PurchaseNumber\":\"PO-2026-0003\",\"TotalAmount\":5800}", IpAddress = "127.0.0.1" },
                new AuditLog { Module = "Inventory", Action = "Adjustment", EntityName = "RawMaterial", EntityId = "RM-006", Username = adminEmail, NewValues = "{\"Delta\":-3,\"Reason\":\"Damaged\"}", IpAddress = "127.0.0.1" }
            );
            await context.SaveChangesAsync();
        });
    }

    private static async Task SeedIfEmptyAsync<T>(ApplicationDbContext context, DbSet<T> dbSet, Func<Task> seedAction) where T : class
    {
        if (await dbSet.AnyAsync())
        {
            return;
        }

        await seedAction();
    }
}

public static class PostgresBootstrapper
{
    public static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The PostgreSQL connection string must include a database name.");
        }

        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        checkCmd.Parameters.AddWithValue("@name", databaseName);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists is null)
        {
            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await createCmd.ExecuteNonQueryAsync();
        }
    }
}
