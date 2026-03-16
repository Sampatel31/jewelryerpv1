# Gold Jewellery Management System (Phase 1)

A multi-shop Gold Jewellery ERP built with **.NET 8** and **WPF**, using a clean, layered architecture.

---

## Solution Structure

```
GoldSystem.sln
├── GoldSystem.WPF           – WPF desktop application (startup project)
│   ├── Views/
│   ├── ViewModels/
│   ├── Resources/
│   ├── Services/            – NavigationService
│   ├── App.xaml / App.xaml.cs
│   └── MainWindow.xaml
├── GoldSystem.Core          – Business logic & domain models
│   ├── Services/
│   ├── Models/
│   ├── Interfaces/          – IRepository<T>
│   └── Constants/           – AppConstants
├── GoldSystem.Data          – Data access (EF Core)
│   ├── Entities/
│   ├── Configurations/
│   ├── Repositories/
│   ├── Migrations/
│   └── GoldDbContext.cs
├── GoldSystem.AI            – ML.NET AI services
│   ├── Services/
│   ├── Models/
│   └── TrainingData/
├── GoldSystem.Sync          – Multi-shop sync engine
│   ├── Services/
│   ├── Models/
│   └── Interfaces/
├── GoldSystem.Reports       – Reporting engine
│   ├── Services/
│   ├── Templates/
│   └── Models/
├── GoldSystem.RateEngine    – Gold rate management
│   ├── Services/
│   ├── Models/
│   └── Interfaces/
└── GoldSystem.Tests         – xUnit tests
```

## NuGet Packages

| Project               | Packages |
|-----------------------|---------|
| GoldSystem.WPF        | MaterialDesignThemes, CommunityToolkit.Mvvm, LiveChartsCore.SkiaSharpView.WPF, ZXing.Net, Serilog.Sinks.File, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting |
| GoldSystem.Core       | MathNet.Numerics |
| GoldSystem.Data       | Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Tools |
| GoldSystem.AI         | Microsoft.ML, Microsoft.ML.TimeSeries |
| GoldSystem.RateEngine | HtmlAgilityPack |
| GoldSystem.Reports    | QuestPDF, ClosedXML |
| GoldSystem.Tests      | xUnit, Moq |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) or Rider

## Getting Started

```bash
# Clone the repository
git clone https://github.com/Sampatel31/jewelryerpv1.git
cd jewelryerpv1

# Restore packages
dotnet restore GoldSystem.sln

# Build the solution
dotnet build GoldSystem.sln

# Run tests
dotnet test GoldSystem.Tests/GoldSystem.Tests.csproj
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
```

## Phase Roadmap

- [x] **Phase 1** – Solution structure, NuGet packages, core infrastructure
- [ ] **Phase 2** – Entity models, EF Core migrations, repositories
- [ ] **Phase 3** – Business logic services
- [ ] **Phase 4** – WPF UI (MVVM views & view-models)
- [ ] **Phase 5** – AI price prediction, gold rate engine
- [ ] **Phase 6** – Multi-shop sync, reports
