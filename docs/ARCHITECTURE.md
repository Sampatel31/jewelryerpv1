# Architecture — Gold Jewellery Management System

---

## Overview

The Gold Jewellery Management System (GJMS) follows a **clean layered architecture** with strict
dependency rules. No circular references exist between projects.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GoldSystem.WPF                               │
│           (Presentation Layer — WPF MVVM Desktop App)               │
├──────────┬──────────┬──────────┬──────────┬──────────┬─────────────┤
│ Core     │ Data     │ AI       │ Sync     │ Reports  │ RateEngine  │
│ (Domain) │ (EF Core)│ (ML.NET) │ (Sync)   │ (PDF/XLS)│ (MCX)       │
└──────────┴──────────┴──────────┴──────────┴──────────┴─────────────┘
```

## Dependency Graph

```
WPF       → Core, Data, AI, Sync, Reports, RateEngine
Data      → Core
Sync      → Core, Data
Reports   → Core, Data
RateEngine→ Core
AI        → Core
Tests     → Core, Data, AI, Sync, RateEngine
WPF.Tests → WPF, Data
```

---

## Project Structure

### GoldSystem.WPF — Presentation Layer

```
GoldSystem.WPF/
├── App.xaml.cs            ← DI container, database initialisation
├── ShellWindow.xaml       ← Main application window (navigation host)
├── Views/                 ← XAML views (one per module)
├── ViewModels/            ← MVVM ViewModels (CommunityToolkit.Mvvm)
├── Services/              ← App-level services
│   ├── NavigationService  ← View routing
│   ├── ThemeService       ← Light/dark theme switching
│   ├── AuthenticationService ← Login, 2FA, token management
│   ├── PasswordService    ← PBKDF2/SHA-256 hashing
│   ├── RBACService        ← Role-based access control
│   ├── ErrorHandlingService ← Global exception handler
│   ├── LoggingService     ← Structured logging (Serilog)
│   ├── CachingService     ← IMemoryCache with TTL
│   └── PerformanceService ← Metrics and performance tracking
└── appsettings.json       ← Configuration (connection string, settings)
```

### GoldSystem.Core — Domain Layer

```
GoldSystem.Core/
├── Models/                ← Domain models and DTOs
│   ├── BillingModels.cs
│   ├── SecurityModels.cs
│   ├── Phase11–15 models
│   └── Exceptions.cs      ← Custom exception types
├── Interfaces/            ← Contracts for all services
│   ├── IRepository<T>
│   ├── IPasswordService
│   ├── IAuthenticationService
│   ├── ISettingsService
│   └── IPhase15Services.cs (IErrorHandlingService, ILoggingService, …)
└── Services/
    └── GoldPriceCalculator ← Pure business logic for gold pricing
```

### GoldSystem.Data — Data Access Layer

```
GoldSystem.Data/
├── GoldDbContext.cs       ← EF Core DbContext
├── GoldDbContextFactory.cs← Design-time factory for migrations
├── Entities/              ← EF Core entity classes
│   ├── Branch, User, Customer, Item
│   ├── Bill, BillItem, Payment
│   ├── GoldRate, Category, Vendor
│   ├── SyncQueue, AuditLog
│   └── OldGoldExchange
├── Configurations/        ← Fluent API entity configurations
├── Migrations/            ← EF Core migration history
├── Repositories/          ← Repository pattern implementations
│   ├── IRepository<T> / Repository<T>
│   └── Specialised repositories per entity
├── Services/
│   ├── BillingEngine      ← Core billing business logic
│   ├── BillingQueryService
│   ├── InventoryQueryService
│   └── AuditLogger
└── UnitOfWork.cs          ← Transaction management
```

### GoldSystem.AI — Machine Learning Layer

```
GoldSystem.AI/
└── Services/
    ├── SalesForecastService       ← ML.NET TimeSeries forecasting
    ├── SlowStockDetectorService   ← Identifies slow-moving items
    ├── RateTrendAnalyzerService   ← Gold rate trend analysis
    ├── RestockSuggestionsService  ← Reorder recommendations
    ├── AnomalyDetectorService     ← Unusual transaction detection
    └── ModelTrainingScheduler     ← Background model training
```

### GoldSystem.RateEngine — Gold Rate Layer

```
GoldSystem.RateEngine/
├── McxRateScraper         ← Scrapes MCX gold rates (HtmlAgilityPack)
├── RateBroadcaster        ← Broadcasts rate changes via events
├── RateRepository         ← Persists rate history
├── ManualRateEntryService ← Manual rate override
├── RateConfigurationService
├── RateSyncBackgroundService ← Periodic auto-refresh (IHostedService)
└── RateListenerService    ← Subscribes to rate change events
```

### GoldSystem.Reports — Reporting Layer

```
GoldSystem.Reports/
└── Services/
    ├── BillPdfService         ← QuestPDF invoice generation
    ├── ReportGenerationService← Report data preparation
    └── ReportExportService    ← Excel export (ClosedXML)
```

### GoldSystem.Sync — Multi-Shop Sync Layer

```
GoldSystem.Sync/
└── Services/
    ├── SyncEngine             ← Orchestrates sync between branches
    ├── ConflictResolver       ← Handles merge conflicts
    ├── SyncQueueInterceptor   ← EF Core interceptor for change tracking
    └── SyncPushService        ← Pushes changes to remote branches
```

---

## MVVM Pattern

All screens follow strict MVVM:

```
View (XAML)
  ↕ Data Binding
ViewModel (CommunityToolkit.Mvvm)
  ↕ Dependency Injection
Services / Repositories
  ↕
Database (EF Core)
```

- **Views** contain zero business logic
- **ViewModels** use `[ObservableProperty]` and `[RelayCommand]` source generators
- **Services** are injected via constructor DI
- **No code-behind** except minimal WPF lifecycle hooks

---

## Database Schema

### Key Tables

| Table              | Purpose |
|--------------------|---------|
| `Branches`         | Shop branches |
| `Users`            | System users |
| `Customers`        | Customer records |
| `Items`            | Inventory items |
| `Bills`            | Sales invoices |
| `BillItems`        | Line items on a bill |
| `Payments`         | Payment records |
| `OldGoldExchanges` | Customer old gold exchange |
| `GoldRates`        | Gold rate history |
| `Categories`       | Item categories (Ring, Chain, etc.) |
| `Vendors`          | Supplier records |
| `SyncQueue`        | Inter-branch sync queue |
| `AuditLogs`        | System audit trail |

### Database Technology

- **Primary:** SQLite (default, embedded, no server required)
- **Optional:** SQL Server 2019+ (configurable via `appsettings.json`)
- **ORM:** Entity Framework Core 8 with Fluent API configurations
- **Migrations:** Code-first EF Core migrations

---

## Security Architecture

```
Request → AuthenticationService
            ↓
          PBKDF2/SHA-256 password verification (100,000 iterations)
            ↓
          2FA OTP verification (optional)
            ↓
          JWT-style token issued (HMACSHA256)
            ↓
          AuthorizationService checks RBAC permissions
            ↓
          AuditService logs the action
```

### Password Storage

Passwords are stored as PBKDF2/SHA-256 hashes:
- 128-bit random salt
- 256-bit hash output
- 100,000 iterations
- Format: `base64(salt):base64(hash)`

No plain-text passwords are ever stored.

---

## Performance

| Feature              | Implementation |
|----------------------|----------------|
| Async UI             | All DB operations use `async/await` |
| Caching              | `IMemoryCache` with per-entity TTL |
| Lazy Loading         | Virtual scrolling in DataGrids |
| Background Tasks     | `IHostedService` for rate sync, ML training |
| Logging              | Serilog with file sink (async, buffered) |

### Cache TTL Configuration

| Cache Key  | TTL     |
|------------|---------|
| Bills      | 30 min  |
| Customers  | 60 min  |
| Inventory  | 15 min  |
| Gold Rates | 5 min   |

---

## Technology Stack

| Layer        | Technology |
|--------------|-----------|
| UI           | WPF (.NET 8-windows) |
| MVVM         | CommunityToolkit.Mvvm 8.3 |
| UI Theme     | MaterialDesignThemes 5.1 |
| Charts       | LiveChartsCore.SkiaSharpView.WPF |
| DI / Hosting | Microsoft.Extensions.Hosting 8 |
| Database     | EF Core 8 + SQLite |
| AI / ML      | ML.NET (Microsoft.ML, Microsoft.ML.TimeSeries) |
| Logging      | Serilog (file + console sinks) |
| PDF          | QuestPDF |
| Excel        | ClosedXML |
| HTTP/Scraping| HtmlAgilityPack |
| Caching      | Microsoft.Extensions.Caching.Memory |
| Barcode      | ZXing.Net |
| Tests        | xUnit 2, Moq, coverlet |
