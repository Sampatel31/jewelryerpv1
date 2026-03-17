# Release Notes — Gold Jewellery Management System

---

## v1.0.0 — Initial Release

**Release Date:** March 2024

### 🎉 What's New

This is the first production release of the Gold Jewellery Management System, a complete
desktop ERP application for gold jewellery businesses built on .NET 8 and WPF.

### ✨ Features

#### Core Modules
- **Billing & Invoicing** — create, edit, print, and export bills as PDF or Excel; supports
  discounts, making charges, wastage, and GST calculation
- **Inventory Management** — add/edit gold items with purity, weight, HUID; low-stock alerts;
  inter-branch stock transfers
- **Customer Management** — GSTIN validation, loyalty points, complete purchase history
- **Vendor Management** — vendor catalogue, purchase tracking

#### Reporting
- **PDF Export** — professional invoice PDFs via QuestPDF
- **Excel Export** — ClosedXML-based spreadsheet reports
- **GSTR-1 Report** — GST return data export
- **Dashboard Analytics** — revenue charts, top customers, inventory summary

#### Gold Rate Engine
- **Live MCX Scraping** — automatic gold rate updates from MCX
- **Manual Override** — enter custom rates per branch
- **Rate History** — log of all rate changes with timestamps

#### AI & Machine Learning
- **Sales Forecasting** — ML.NET TimeSeries forecasting
- **Anomaly Detection** — flag unusual transactions
- **Slow Stock Detection** — identify slow-moving inventory
- **Restock Suggestions** — AI-powered restocking recommendations

#### Security & Compliance
- **Two-Factor Authentication (2FA)** — OTP via TOTP
- **Role-Based Access Control (RBAC)** — Admin, Manager, Operator roles
- **Full Audit Trail** — every action logged with user, timestamp, old/new values
- **Password Policy** — configurable length, complexity requirements

#### Settings & Customisation
- **Company Profile** — name, address, logo, GSTIN
- **Tax Rates** — configurable GST/VAT rates
- **Themes** — light/dark mode, colour schemes
- **Backup & Restore** — manual and scheduled backups

#### Multi-Shop Sync
- **Branch Management** — add unlimited branches
- **Near-Real-Time Sync** — inventory and billing data synced across branches
- **Conflict Resolution** — automatic merge with manual override option

#### Performance & Reliability
- **Structured Logging** — Serilog with daily rotating log files (30-day retention)
- **In-Memory Caching** — `IMemoryCache` with configurable TTL
- **Async/Await** — non-blocking UI throughout
- **Error Handling** — global exception handling with user-friendly messages

### 🔑 Default Login

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `Admin@123456` |

### 💻 System Requirements

- Windows 10 / 11 (64-bit)
- 4 GB RAM minimum (8 GB recommended for AI features)
- 200 MB disk space
- **No .NET installation required** — self-contained executable

### 📦 Download

Download the self-contained Windows executable from the
[Releases page](https://github.com/Sampatel31/jewelryerpv1/releases/latest).

### 🛠 Technology Stack

| Component         | Technology |
|-------------------|-----------|
| UI Framework      | WPF (.NET 8) |
| MVVM Toolkit      | CommunityToolkit.Mvvm |
| Database          | SQLite (EF Core 8) |
| UI Theme          | MaterialDesignThemes |
| Charts            | LiveChartsCore |
| PDF Generation    | QuestPDF |
| Excel Export      | ClosedXML |
| AI / ML           | ML.NET (Microsoft.ML.TimeSeries) |
| Logging           | Serilog |
| Gold Rate Source  | MCX (HtmlAgilityPack) |

### 🐛 Known Issues

- The WPF application targets Windows only (by design)
- AI forecasting requires at least 30 days of historical data for accurate predictions
- Multi-shop sync requires network connectivity between branches

### 📖 Documentation

- [Quick Start Guide](QUICKSTART.md)
- [User Guide](docs/USER_GUIDE.md)
- [Architecture Overview](docs/ARCHITECTURE.md)
- [FAQ](docs/FAQ.md)
