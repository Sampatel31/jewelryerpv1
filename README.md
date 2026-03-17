# Gold Jewellery Management System v1.0.0

A multi-shop Gold Jewellery ERP built with **.NET 8** and **WPF**, using a clean, layered architecture.

---

## Overview

Complete desktop application for managing gold jewelry business operations including billing,
inventory, customers, reporting, security, and multi-shop synchronisation.

## Features

- **Billing & Invoice Management** – create, print, and export bills as PDF/Excel
- **Inventory Management** – stock tracking, item transfers, and low-stock alerts
- **Customer Management** – loyalty points, purchase history, and GSTIN validation
- **Comprehensive Reporting** – PDF/Excel export and GSTR-1 reports
- **Settings & Customisation** – company profile, tax rates, themes, and backup
- **Security & User Management** – 2FA (OTP), RBAC, and a full audit trail
- **AI Insights** – ML.NET-powered sales forecasting and anomaly detection
- **Multi-shop Sync** – near-real-time sync engine between branches
- **Gold Rate Engine** – live MCX rate scraping with manual override
- **Error Handling & Logging** – structured Serilog logging with performance tracking
- **In-process Caching** – `IMemoryCache`-backed caching service with TTL support

## Requirements

- .NET 8.0 SDK or higher
- SQL Server 2019+ or SQLite (configurable)
- Windows 10 or higher (WPF runtime)
- 4 GB RAM minimum; 8 GB recommended for AI features

## Installation

```bash
# 1. Clone the repository
git clone https://github.com/Sampatel31/jewelryerpv1.git
cd jewelryerpv1

# 2. Update the connection string
#    Edit GoldSystem.WPF/appsettings.json

# 3. Apply database migrations
dotnet ef database update --project GoldSystem.Data --startup-project GoldSystem.WPF

# 4. Run the application (Windows only)
dotnet run --project GoldSystem.WPF
```

## Quick Start

1. Launch the application – SQLite database is created automatically on first run.
2. Navigate to **Settings → Company** and fill in your company details.
3. Navigate to **Settings → Users** and create an admin account.
4. Add gold items via the **Inventory** module.
5. Create your first invoice in the **Billing** module.

## Solution Structure

```
GoldSystem.sln
├── GoldSystem.WPF           – WPF desktop application (startup project)
│   ├── Views/
│   ├── ViewModels/
│   ├── Services/            – NavigationService, ErrorHandlingService, CachingService, …
│   └── App.xaml.cs          – DI container configuration
├── GoldSystem.Core          – Business logic & domain models
│   ├── Services/            – GoldPriceCalculator, BillingInterfaces
│   ├── Models/              – BillingModels, Phase11–15 models, custom exceptions
│   └── Interfaces/          – IRepository<T>, ISettingsService, IPhase15Services, …
├── GoldSystem.Data          – Data access (EF Core + SQLite/SQL Server)
├── GoldSystem.AI            – ML.NET forecasting & anomaly detection
├── GoldSystem.Sync          – Multi-shop sync engine
├── GoldSystem.Reports       – QuestPDF / ClosedXML reporting engine
├── GoldSystem.RateEngine    – Live gold rate scraping
├── GoldSystem.Tests         – Core/Data/Sync/AI unit tests (xUnit)
└── GoldSystem.WPF.Tests     – WPF services & ViewModel unit tests (xUnit)
```

## NuGet Packages

| Project               | Key Packages |
|-----------------------|--------------|
| GoldSystem.WPF        | MaterialDesignThemes, CommunityToolkit.Mvvm, Serilog, Microsoft.Extensions.Hosting, Microsoft.Extensions.Caching.Memory |
| GoldSystem.Core       | MathNet.Numerics |
| GoldSystem.Data       | EF Core (SQLite + SQL Server) |
| GoldSystem.AI         | Microsoft.ML, Microsoft.ML.TimeSeries |
| GoldSystem.RateEngine | HtmlAgilityPack |
| GoldSystem.Reports    | QuestPDF, ClosedXML |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) or JetBrains Rider

## Getting Started

```bash
# Restore packages
dotnet restore GoldSystem.sln

# Build the solution
dotnet build GoldSystem.sln

# Run core tests (cross-platform)
dotnet test GoldSystem.Tests/GoldSystem.Tests.csproj

# Run WPF tests (Windows only)
dotnet test GoldSystem.WPF.Tests/GoldSystem.WPF.Tests.csproj
```

> **Note:** The WPF application targets `net8.0-windows` and must be run on a Windows machine.
> The solution can be restored and compiled on Linux/macOS using `EnableWindowsTargeting=true`.

## Project Dependencies (No Circular References)

```
WPF → Core, Data, AI, Sync, Reports, RateEngine
Data → Core
Sync → Core, Data
Reports → Core, Data
RateEngine → Core
AI → Core
Tests → Core, Data
WPF.Tests → WPF, Data
```

## Phase Roadmap

- [x] **Phase 1**  – Solution structure, NuGet packages, core infrastructure
- [x] **Phase 2**  – Entity models, EF Core migrations, repositories
- [x] **Phase 3**  – Business logic services
- [x] **Phase 4**  – WPF UI (MVVM views & view-models)
- [x] **Phase 5**  – AI price prediction, gold rate engine
- [x] **Phase 6**  – Multi-shop sync, reports
- [x] **Phase 7**  – Customer management & loyalty points
- [x] **Phase 8**  – Advanced billing (discounts, tax, PDF export)
- [x] **Phase 9**  – Inventory management & stock transfers
- [x] **Phase 10** – GSTR-1 reporting & Excel export
- [x] **Phase 11** – AI insights dashboard
- [x] **Phase 12** – Multi-branch sync status & conflict resolution
- [x] **Phase 13** – Settings & customisation (company, tax, theme, backup)
- [x] **Phase 14** – Security & user management (2FA, RBAC, audit trail)
- [x] **Phase 15** – Final polish: error handling, logging, caching, performance

## Support

- Email: support@company.com
- Phone: +91-XXXX-XXXXX

## License

Proprietary
