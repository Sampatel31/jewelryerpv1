# Gold Jewellery Management System v1.0.0

[![Build & Release](https://github.com/Sampatel31/jewelryerpv1/actions/workflows/build-release.yml/badge.svg)](https://github.com/Sampatel31/jewelryerpv1/actions/workflows/build-release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-blue)](https://github.com/Sampatel31/jewelryerpv1/releases/latest)

A complete desktop ERP for gold jewellery businesses — built with **.NET 8** and **WPF**.
**No installation required.** Just download, extract, and run.

---

## 🚀 Quick Download

> **[⬇️ Download Latest Release (Windows)](https://github.com/Sampatel31/jewelryerpv1/releases/latest)**

1. Download `GoldJewelleryERP-vX.X.X-win-x64.zip`
2. Extract to any folder (e.g. `C:\GoldERP\`)
3. Double-click `GoldSystem.WPF.exe`
4. Log in with default credentials:

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `Admin@123456` |

> ⚠️ **Change the default password** after first login: *Settings → Users → Change Password*

The SQLite database is created **automatically** on first launch. No manual database setup needed.

---

## ✨ Features

- **Billing & Invoice Management** — create, print, and export bills as PDF/Excel; supports
  discounts, making charges, wastage, old gold exchange, and GST
- **Inventory Management** — stock tracking, HUID, inter-branch transfers, low-stock alerts
- **Customer Management** — loyalty points, purchase history, GSTIN validation
- **Comprehensive Reporting** — PDF/Excel export, GSTR-1 returns, audit trail export
- **Gold Rate Engine** — live MCX rate scraping with manual override and rate history
- **AI Insights** — ML.NET-powered sales forecasting, anomaly detection, and restock suggestions
- **Multi-shop Sync** — near-real-time sync engine between branches with conflict resolution
- **Security & RBAC** — 2FA OTP, role-based access control, full audit trail
- **Settings & Customisation** — company profile, tax rates, themes, backup/restore
- **Error Handling & Logging** — structured Serilog logging with daily file rotation
- **In-process Caching** — `IMemoryCache`-backed service with configurable TTL

---

## 💻 System Requirements

| Requirement | Value |
|-------------|-------|
| Operating System | Windows 10 / 11 (64-bit) |
| RAM | 4 GB minimum, 8 GB for AI features |
| Disk Space | 200 MB |
| .NET Runtime | **Not required** — self-contained executable |

---

## 📥 Installation (Developer / Source Build)

If you want to build from source:

```bash
# 1. Clone the repository
git clone https://github.com/Sampatel31/jewelryerpv1.git
cd jewelryerpv1

# 2. Restore packages
dotnet restore GoldSystem.sln

# 3. Build the solution
dotnet build GoldSystem.sln

# 4. Run tests
dotnet test GoldSystem.Tests/GoldSystem.Tests.csproj

# 5. Run the application (Windows only)
dotnet run --project GoldSystem.WPF
```

### Publish Self-Contained Executable

```bash
dotnet publish GoldSystem.WPF/GoldSystem.WPF.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  --output ./publish
```

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [Quick Start Guide](QUICKSTART.md) | Get running in 5 minutes |
| [User Guide](docs/USER_GUIDE.md) | Complete user manual for all modules |
| [Architecture](docs/ARCHITECTURE.md) | System design, project structure, tech stack |
| [FAQ](docs/FAQ.md) | Frequently asked questions |
| [Release Notes](RELEASE_NOTES.md) | v1.0.0 changelog and feature list |

---

## 🏗 Solution Structure

```
GoldSystem.sln
├── GoldSystem.WPF           – WPF desktop application (startup project)
│   ├── Views/               – XAML views (one per module)
│   ├── ViewModels/          – MVVM ViewModels (CommunityToolkit.Mvvm)
│   ├── Services/            – NavigationService, ErrorHandlingService, …
│   └── App.xaml.cs          – DI container + auto-database initialisation
├── GoldSystem.Core          – Business logic & domain models
│   ├── Models/              – BillingModels, SecurityModels, custom exceptions
│   └── Interfaces/          – IRepository<T>, IPasswordService, IPhase15Services
├── GoldSystem.Data          – Data access (EF Core 8 + SQLite / SQL Server)
│   ├── Entities/            – EF Core entity classes
│   ├── Migrations/          – Code-first migration history
│   ├── Repositories/        – Repository pattern
│   └── Services/            – BillingEngine, AuditLogger, …
├── GoldSystem.AI            – ML.NET forecasting & anomaly detection
├── GoldSystem.Sync          – Multi-shop sync engine
├── GoldSystem.Reports       – QuestPDF / ClosedXML reporting
├── GoldSystem.RateEngine    – Live MCX gold rate scraping
├── GoldSystem.Tests         – Core / Data / AI unit tests (xUnit, 100+ tests)
└── GoldSystem.WPF.Tests     – WPF services & ViewModel tests (xUnit)
```

---

## 📦 NuGet Packages

| Project | Key Packages |
|---------|-------------|
| GoldSystem.WPF | MaterialDesignThemes, CommunityToolkit.Mvvm, Serilog, Microsoft.Extensions.Hosting, Microsoft.Extensions.Caching.Memory |
| GoldSystem.Core | MathNet.Numerics |
| GoldSystem.Data | EF Core 8 (SQLite + SQL Server) |
| GoldSystem.AI | Microsoft.ML, Microsoft.ML.TimeSeries |
| GoldSystem.RateEngine | HtmlAgilityPack |
| GoldSystem.Reports | QuestPDF, ClosedXML |

---

## 🔄 CI/CD

The repository uses **GitHub Actions** (`.github/workflows/build-release.yml`):

1. **Build** — `dotnet build` on every push to `main`
2. **Test** — runs 100+ unit tests across `GoldSystem.Tests` and `GoldSystem.WPF.Tests`
3. **Publish** — builds a self-contained `win-x64` single-file executable
4. **Release** — creates a GitHub Release with the ZIP artifact on version tags (`v*.*.*`)

---

## 🏁 Phase Roadmap

- [x] **Phase 1** – Solution structure, NuGet packages, core infrastructure
- [x] **Phase 2** – Entity models, EF Core migrations, repositories
- [x] **Phase 3** – Business logic services
- [x] **Phase 4** – WPF UI (MVVM views & view-models)
- [x] **Phase 5** – AI price prediction, gold rate engine
- [x] **Phase 6** – Multi-shop sync, reports
- [x] **Phase 7** – Customer management & loyalty points
- [x] **Phase 8** – Advanced billing (discounts, tax, PDF export)
- [x] **Phase 9** – Inventory management & stock transfers
- [x] **Phase 10** – GSTR-1 reporting & Excel export
- [x] **Phase 11** – AI insights dashboard
- [x] **Phase 12** – Multi-branch sync status & conflict resolution
- [x] **Phase 13** – Settings & customisation (company, tax, theme, backup)
- [x] **Phase 14** – Security & user management (2FA, RBAC, audit trail)
- [x] **Phase 15** – Final polish: error handling, logging, caching, performance

---

## 🤝 Support

- Open a [GitHub Issue](https://github.com/Sampatel31/jewelryerpv1/issues)

## 📄 License

This project is licensed under the [MIT License](LICENSE).

