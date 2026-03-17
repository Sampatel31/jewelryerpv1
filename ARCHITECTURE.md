# System Architecture

## Technology Stack

| Layer            | Technology |
|------------------|------------|
| UI               | WPF (.NET 8, MVVM via CommunityToolkit.Mvvm) |
| UI Theming       | Material Design In XAML Toolkit |
| Charts           | LiveChartsCore.SkiaSharpView.WPF |
| ORM              | Entity Framework Core 8 |
| Database         | SQLite (default) / SQL Server 2019+ |
| Logging          | Serilog (file sink, rolling daily) |
| Caching          | Microsoft.Extensions.Caching.Memory |
| DI Container     | Microsoft.Extensions.DependencyInjection (Generic Host) |
| AI / ML          | ML.NET (time-series, anomaly detection) |
| Reporting        | QuestPDF (PDF) + ClosedXML (Excel) |
| Barcode / QR     | ZXing.Net |
| Rate Scraping    | HtmlAgilityPack |
| Testing          | xUnit + Moq |

---

## Solution Projects

```
GoldSystem.WPF           – Startup project; hosts DI container and all Views/ViewModels
GoldSystem.Core          – Domain models, interfaces, and pure-C# business logic
GoldSystem.Data          – EF Core DbContext, entity configurations, repositories, migrations
GoldSystem.AI            – ML.NET model training and inference services
GoldSystem.Sync          – Multi-branch synchronisation engine
GoldSystem.Reports       – PDF and Excel report generation
GoldSystem.RateEngine    – Live gold rate scraping and broadcasting
GoldSystem.Tests         – xUnit tests for Core, Data, Sync, AI, RateEngine
GoldSystem.WPF.Tests     – xUnit tests for WPF services and ViewModels
```

### Dependency Graph (no circular references)

```
GoldSystem.WPF
  ├── GoldSystem.Core
  ├── GoldSystem.Data
  │     └── GoldSystem.Core
  ├── GoldSystem.AI
  │     └── GoldSystem.Core
  ├── GoldSystem.Sync
  │     ├── GoldSystem.Core
  │     └── GoldSystem.Data
  ├── GoldSystem.Reports
  │     ├── GoldSystem.Core
  │     └── GoldSystem.Data
  └── GoldSystem.RateEngine
        └── GoldSystem.Core
```

---

## Layered Architecture

```
┌──────────────────────────────────────┐
│          Presentation Layer          │
│  WPF Views + ViewModels (MVVM)       │
│  NavigationService, ThemeService     │
│  ErrorHandlingService, LoggingService│
│  CachingService, PerformanceService  │
└──────────────┬───────────────────────┘
               │ ICommand / data-binding
┌──────────────▼───────────────────────┐
│          Application Layer           │
│  BillingEngine, SettingsService      │
│  AuthenticationService, RBACService  │
│  BackupService, AuditService         │
└──────────────┬───────────────────────┘
               │ IRepository<T> / IUnitOfWork
┌──────────────▼───────────────────────┐
│           Domain Layer               │
│  GoldSystem.Core models & interfaces │
│  ValidationException, custom types  │
└──────────────┬───────────────────────┘
               │ EF Core
┌──────────────▼───────────────────────┐
│        Infrastructure Layer          │
│  GoldDbContext, Repositories         │
│  EF Core Migrations, SQLite/SQL Srv  │
└──────────────────────────────────────┘
```

---

## Key Design Patterns

| Pattern | Where Used |
|---------|-----------|
| MVVM | All WPF Views / ViewModels |
| Repository + Unit of Work | `GoldSystem.Data` |
| Dependency Injection | `App.xaml.cs` (Generic Host) |
| Observer / Event | `RateChangedEventPublisher`, `StatusIndicatorService` |
| Strategy | `IRateSource` (McxRateScraper vs ManualRateEntryService) |
| Factory | `IBillNumberGenerator` |
| Decorator / Pipeline | `GlobalExceptionHandlerMiddleware` (optional API layer) |

---

## Phase 15 Services

### ErrorHandlingService
Maps exceptions to user-friendly messages and writes structured logs.  
Exposes `ShowErrorDialog`, `ShowSuccessToast`, `ShowWarningToast`, `ShowInfoToast`,
and `ValidateInput` (throws `ValidationException` on failure).

### LoggingService
Thin wrapper around `ILogger<T>` with domain helpers:
`LogPerformance`, `LogUserAction`, `LogApiCall`.

### CachingService
In-memory cache backed by `IMemoryCache`.  
Tracks all active keys for atomic `ClearAsync()`.  
Default TTL: 1 hour; configurable per call.

### PerformanceService
Wraps any `Func<Task<T>>` or `Func<Task>` with a `Stopwatch`.  
Accumulates per-operation min/max/avg statistics accessible via `GetMetrics()`.

---

## Security Architecture (Phase 14)

- **Password hashing** – PBKDF2 / SHA-256 with random salt
- **2FA** – numeric OTP with configurable length and expiry
- **JWT tokens** – HMACSHA256-signed, ephemeral signing key (no embedded secret)
- **RBAC** – role-based permissions per `PermissionModule` × `PermissionAction`
- **Audit trail** – every user action logged with timestamp, IP, and entity reference
