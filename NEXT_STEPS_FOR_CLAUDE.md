# MeatPro Next Steps for Claude

Use this as the implementation script for continuing the MeatPro ERP project.

## Current State

MeatPro already has:
- ASP.NET Core MVC frontend shell
- Identity authentication
- MySQL/XAMPP database setup
- Seeded roles and sample data
- Dashboard page
- Shared module list pages
- Modern sidebar/topbar UI
- Login page
- Basic service and controller scaffolding
- **Raw Materials CRUD (COMPLETE)**: Full create/edit/details/delete/list with search, sorting, pagination, and validation

## Goal

Build the remaining full ERP functionality so each module has real create, edit, delete, details, filtering, sorting, pagination, and API-backed data management.

## Priority Order

### Phase 1: Core CRUD Frontend (In Progress)

#### Completed:
1. ✅ **Raw Materials** — Full CRUD with index/list, create, edit, details, delete pages; search, sorting, pagination; SKU uniqueness validation; stock-value calculations

#### Remaining in Phase 1:
2. Finished Goods
3. Suppliers
4. Products
5. Purchase Transactions
6. Production Plans
7. Work Orders
8. Production Batches

### Raw Materials CRUD Implementation Notes

**Files Created/Modified:**
- `ViewModels/RawMaterialsViewModels.cs` — Index, Details, Form view models
- `Services/RawMaterialsService.cs` — Service interface and implementation with paging, sorting, search, SKU validation
- `Controllers/RawMaterialsController.cs` — CRUD actions (Index, Create, Edit, Details, Delete)
- `Views/RawMaterials/Index.cshtml` — List with search, sort, pagination, and metrics
- `Views/RawMaterials/Create.cshtml` — Create form
- `Views/RawMaterials/Edit.cshtml` — Edit form
- `Views/RawMaterials/Details.cshtml` — Details view with status, audit dates
- `Views/RawMaterials/Delete.cshtml` — Delete confirmation
- `Views/RawMaterials/_Form.cshtml` — Shared form partial for Create/Edit
- `Program.cs` — Registered IRawMaterialService via DI
- `Controllers/InventoryController.cs` — Redirects legacy RawMaterials action to new controller
- `Views/Shared/_Layout.cshtml` — Updated sidebar to point to RawMaterials controller

**Pattern Used:**
- Index page: search placeholder in form, sort dropdown, pagination with page math; metrics cards showing total items, low stock count, total/stock value
- Details page: read-only card layout with full entity data plus audit dates (CreatedAtUtc, UpdatedAtUtc); status badge and action buttons
- Create/Edit: shared form partial with all required fields; validation errors shown per-field; success message in TempData
- Delete: confirmation view showing key details before permanent removal
- Service layer: builds view models from queries; handles pagination, sorting, search via LIKE queries
- All actions are async/await; validation is both server-side (DataAnnotations on form model) and database-level (unique SKU constraint)

### Remaining CRUD Modules

For each module (Finished Goods, Suppliers, Products, etc.):
- Create index page with search, filters, sorting, pagination
- Create details page
- Create create/edit form page
- Create delete confirmation page or modal
- Wire controller actions to service layer
- Use DTOs and form view models
- Use async/await everywhere
- Keep validation on server and client side
- Follow the Raw Materials pattern established above

### Phase 2: Real Module Behavior
Implement actual business logic:
- Stock in and stock out for raw materials
- Material release to production
- Finished goods adjustments
- Purchase receiving workflow
- Production output recording
- Batch traceability lookup
- Low stock alerts
- Expiration alerts
- Pending approval alerts
- Audit trail entries for all changes

### Phase 3: Reports
Build proper report pages with export actions:
- Inventory report
- Production report
- Procurement report
- Dashboard analytics report

Add:
- PDF export endpoints
- Excel export endpoints
- Date range filters
- Status filters
- Summary cards and charts

### Phase 4: Administration
Build admin screens for:
- User management
- Role management
- Permission assignment
- Audit log viewer
- System settings page

### Phase 5: REST API
Add RESTful API controllers for:
- Raw materials
- Finished goods
- Suppliers
- Products
- Purchase transactions
- Production plans
- Work orders
- Production batches

Expose:
- GET list with filters
- GET by id
- P✅ Raw Materials CRUD (COMPLETE)
2. Finished Goods CRUD (follow Raw Materials pattern)
3. Suppliers CRUD (follow Raw Materials pattern)
4. Products CRUD (follow Raw Materials pattern)
5. Purchase Transactions CRUD (with supplier foreign-key selection)
6. Production Plans CRUD (with product foreign-key selection)
7. Work Orders CRUD (with production-plan foreign-key selection)
8. Production Batches CRUD (with work-order and product foreign-key selection)
9. Reports with exports
10. Admin screens
11. REST API controllers
12Make forms cleaner and more consistent
- Add toasts or alerts for success and error messages

## Important Rules

- Follow SOLID principles
- Use repository pattern
- Use service layer pattern
- Use dependency injection
- Use DTOs and view models
- Use clean architecture boundaries where practical
- Keep code consistent with the current MeatPro styling
- Avoid adding unnecessary complexity
- Do not break existing login or dashboard behavior

## Suggested Implementation Sequence

1. Raw Materials CRUD
2. Suppliers CRUD
3. Purchase Transactions CRUD
4. Production Plans CRUD
5. Work Orders CRUD
6. Production Batches CRUD
7. Finished Goods CRUD
8. Reports with exports
9. Admin screens
10. REST API controllers
11. Final UI polish

## Acceptance Criteria
CRUD module complete only if:
- ✅ The module has working index/list, create, edit, details, delete pages
- ✅ Data is saved and loaded from the database
- ✅ Form validation works (server-side and client-side)
- ✅ Search/filter/sort/pagination work on the list page
- ✅ No compile errors exist
- ✅ `dotnet build` succeeds
- ✅ The app still starts successfully with XAMPP/MySQL
- ✅ Success/error messages are shown via TempData
- ✅ Audit dates (CreatedAtUtc, UpdatedAtUtc) are tracked and displayed
- ✅Implementation Notes for Next Module

**Key Points:**
- The database provider is MySQL through XAMPP, not SQL Server LocalDB.
- The app is already seeded with an admin account.
- Keep the current visual language: dark sidebar, clean cards, gold accents, modern SaaS style.
- Work incrementally and verify `dotnet build` after each module.
- All controller actions should be `[Authorize]` and async.
- Service methods should use `AsNoTracking()` for read queries to avoid change tracking overhead.
- Use LIKE queries in the service for search; the view model handles pagination math.
- For modules with foreign keys (e.g., Purchase Transactions → Supplier), use `.Include()` to load related entities and populate dropdowns in the form.
- Follow the exact folder and naming structure established by Raw Materials:
  - ViewModels: `[EntityName]sViewModels.cs` (e.g., `FinishedGoodsViewModels.cs`)
  - Services: `[EntityName]sService.cs` (e.g., `FinishedGoodsService.cs`)
  - Controllers: `[EntityName]sController.cs` (e.g., `FinishedGoodsController.cs`)
  - Views: `Views/[EntityNames]/` folder with `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `Delete.cshtml`, and `_Form.cshtml`
- Always run `dotnet build` to verify before moving to the next module.
- If compilation fails, stay on that module and fix it before proceeding

- The database provider is MySQL through XAMPP, not SQL Server LocalDB.
- The app is already seeded with an admin account.
- Keep the current visual language: dark sidebar, clean cards, gold accents, modern SaaS style.
- Work incrementally and verify after each module.
