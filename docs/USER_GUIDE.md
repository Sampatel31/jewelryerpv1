# User Guide — Gold Jewellery Management System v1.0.0

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Dashboard](#2-dashboard)
3. [Billing & Invoicing](#3-billing--invoicing)
4. [Inventory Management](#4-inventory-management)
5. [Customer Management](#5-customer-management)
6. [Gold Rate Management](#6-gold-rate-management)
7. [Vendor Management](#7-vendor-management)
8. [Reporting](#8-reporting)
9. [AI Insights](#9-ai-insights)
10. [Multi-Shop Sync](#10-multi-shop-sync)
11. [Settings](#11-settings)
12. [Security & User Management](#12-security--user-management)
13. [Audit Log](#13-audit-log)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Getting Started

### First Launch

1. Download and extract the ZIP from the [Releases page](https://github.com/Sampatel31/jewelryerpv1/releases/latest)
2. Double-click `GoldSystem.WPF.exe`
3. The SQLite database is created automatically — no setup required
4. Log in with the default credentials:

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `Admin@123456` |

> **Security Note:** Change the default password immediately after first login via *Settings → Users → Change Password*.

### Initial Configuration Checklist

- [ ] Change the admin password
- [ ] Fill in company details (*Settings → Company*)
- [ ] Configure tax rates (*Settings → Tax*)
- [ ] Add branch information (*Settings → Branches*)
- [ ] Set up user accounts (*Settings → Users*)
- [ ] Add inventory items (*Inventory → Add Item*)
- [ ] Enter today's gold rate (*Gold Rate → Manual Entry*)

---

## 2. Dashboard

The Dashboard provides an at-a-glance summary of your business:

| Widget               | Description |
|----------------------|-------------|
| Today's Revenue      | Total sales for the current day |
| Bills Created        | Number of invoices today |
| Low Stock Alerts     | Items below minimum stock level |
| Current Gold Rate    | Latest 22K / 24K rate |
| Revenue Chart        | 30-day revenue trend |
| Top Customers        | Highest-value customers by purchase |

### Refreshing Data

Click the **Refresh** button in the top-right corner to reload dashboard data.

---

## 3. Billing & Invoicing

### Creating a New Bill

1. Navigate to **Billing** in the left sidebar
2. Click **New Bill**
3. Select or search for a customer (or create a new one on-the-fly)
4. Add items from inventory using the item search box
5. For each item, adjust:
   - **Weight** (grams)
   - **Purity** (22K, 24K, 18K, etc.)
   - **Making Charges** (percentage or flat amount)
   - **Wastage** (percentage)
6. Review the calculated totals (gold value + making + wastage + GST)
7. Select **Payment Mode** (Cash / Card / UPI / Cheque / Multiple)
8. Click **Save & Print** to finalise

### Bill Calculations

```
Item Value  = Weight (g) × Gold Rate (per gram) × Purity Factor
Making      = Item Value × Making % (or flat amount)
Wastage     = Item Value × Wastage %
Sub Total   = Item Value + Making + Wastage
GST         = Sub Total × GST Rate (3% for gold)
Total       = Sub Total + GST
```

### Printing / Exporting

- **Print** — sends to the default Windows printer
- **PDF** — saves a PDF invoice to the selected folder
- **Excel** — exports bill data to an Excel spreadsheet

### Editing a Bill

Bills can be edited only if they are in **Draft** status. Finalised bills cannot be modified (audit integrity).

### Old Gold Exchange

When a customer exchanges old gold:
1. Click **Add Old Gold** in the bill
2. Enter weight and purity of the old gold
3. The system calculates the exchange value and deducts from the bill total

---

## 4. Inventory Management

### Adding an Item

1. Navigate to **Inventory**
2. Click **Add Item**
3. Fill in:
   - **Item Code** (auto-generated or custom)
   - **Name** and **Description**
   - **Category** (Ring, Chain, Bangle, etc.)
   - **Weight** (grams)
   - **Purity** (22K, 24K, 18K, etc.)
   - **Making Charges** (default for this item)
   - **HUID** (Hallmark Unique ID, mandatory for 22K+)
   - **Minimum Stock Level** (for low-stock alerts)
4. Click **Save**

### Stock Transfer (Between Branches)

1. Go to **Inventory → Stock Transfer**
2. Select **Source Branch** and **Destination Branch**
3. Choose items and quantities to transfer
4. Add transfer notes
5. Click **Transfer** — the receiving branch will see the transfer in their sync queue

### Low Stock Alerts

Items below their minimum stock level are highlighted in red on the Inventory screen and shown as alerts on the Dashboard.

---

## 5. Customer Management

### Adding a Customer

1. Navigate to **Customers**
2. Click **Add Customer**
3. Fill in:
   - Name, Phone, Email
   - Address
   - GSTIN (optional — validated automatically)
   - Date of Birth (for loyalty program)
4. Click **Save**

### Loyalty Points

| Action               | Points |
|----------------------|--------|
| Purchase (per ₹1000) | 10 points |
| Referral             | 100 points |
| Birthday bonus       | 50 points |

Points can be redeemed at ₹1 per point during billing.

### Customer Purchase History

Click on any customer → **Purchase History** to see all their bills, total spend, and loyalty points balance.

---

## 6. Gold Rate Management

### Automatic Rate (MCX)

The system automatically fetches gold rates from MCX every 15 minutes.
The rate is visible in the top status bar of the application.

### Manual Rate Entry

1. Navigate to **Gold Rate**
2. Click **Manual Entry**
3. Enter rates for each purity (24K, 22K, 18K, etc.)
4. Click **Apply**

### Rate History

Click **History** to view all historical rate changes with timestamps and source (Auto/Manual).

---

## 7. Vendor Management

### Adding a Vendor

1. Navigate to **Vendors**
2. Click **Add Vendor**
3. Fill in name, contact, address, GSTIN, and account details
4. Click **Save**

### Purchase Recording

Record purchases from vendors through **Vendors → New Purchase**. This automatically updates inventory.

---

## 8. Reporting

### Available Reports

| Report               | Format | Description |
|----------------------|--------|-------------|
| Daily Sales          | PDF/Excel | All bills for a date range |
| Customer Statement   | PDF/Excel | Transactions for a specific customer |
| Inventory Report     | PDF/Excel | Current stock with valuations |
| GSTR-1               | Excel  | GST return data in prescribed format |
| Audit Trail          | PDF/Excel | All system actions with timestamps |
| AI Sales Forecast    | PDF/Excel | ML.NET-generated sales predictions |

### Generating a Report

1. Navigate to **Reports**
2. Select report type
3. Set date range (if applicable)
4. Choose output format (PDF / Excel)
5. Click **Generate** — file will be saved to the Reports folder

---

## 9. AI Insights

The AI Insights module uses ML.NET to analyse your business data:

| Feature              | Description |
|----------------------|-------------|
| Sales Forecast       | Predicts next 30 days of revenue |
| Slow Stock Detection | Items that haven't moved in 60+ days |
| Anomaly Detection    | Unusual transactions that may need review |
| Restock Suggestions  | Items to reorder based on sales velocity |

> **Note:** AI features require at least 30 days of historical data for meaningful predictions.

---

## 10. Multi-Shop Sync

### Adding a Branch

1. Navigate to **Settings → Branches**
2. Click **Add Branch**
3. Enter branch details (code, name, address, GSTIN)
4. Enter the branch's SQL connection string
5. Click **Save**

### Sync Status

The Sync Status screen shows:
- Last sync time per branch
- Pending sync items
- Conflict log

### Conflict Resolution

When the same record is modified on two branches before syncing, a conflict is raised.
Navigate to **Sync → Conflicts** to review and resolve each conflict manually or automatically.

---

## 11. Settings

### Company Settings

Configure your shop's legal name, address, GSTIN, logo, and contact details.
These appear on all printed invoices.

### Tax Settings

Configure GST rates:
- CGST + SGST (intra-state)
- IGST (inter-state)
- Jewellery-specific rates (3% on gold)

### Theme Settings

Switch between:
- **Light Mode** (default)
- **Dark Mode**
- Custom colour schemes

### Backup & Restore

| Option | Description |
|--------|-------------|
| Manual Backup | Creates a `.zip` backup of the database immediately |
| Scheduled Backup | Daily/weekly automatic backup at a set time |
| Restore | Select a backup file to restore from |

> Backups are stored in the `Backups/` folder next to the executable.

---

## 12. Security & User Management

### User Roles

| Role     | Permissions |
|----------|-------------|
| Admin    | Full access to all modules |
| Manager  | Access to billing, inventory, reports; no user management |
| Operator | Billing only; read-only inventory |

### Adding a User

1. Navigate to **Settings → Users**
2. Click **Add User**
3. Enter username, display name, email, and role
4. Set a temporary password
5. Check **Force Password Change** to require the user to change it on login
6. Click **Save**

### Two-Factor Authentication (2FA)

2FA can be enabled per user:
1. Settings → Users → select user → **Enable 2FA**
2. Scan the QR code with Google Authenticator or similar
3. Enter the verification code to confirm setup

### Password Policy

Configurable in **Settings → Security**:
- Minimum length (default: 8)
- Require uppercase, lowercase, digits, special characters
- Maximum password age
- Prevent password reuse

---

## 13. Audit Log

Every action performed in the system is logged:

| Column         | Description |
|----------------|-------------|
| Timestamp      | Date and time of the action |
| User           | Who performed the action |
| Module         | Which module (Billing, Inventory, etc.) |
| Action         | What was done (Create, Update, Delete) |
| Entity         | Which record was affected |
| Old Value      | Previous value (for updates) |
| New Value      | New value |

### Exporting the Audit Trail

Click **Export → Excel** to download the full audit log.

---

## 14. Troubleshooting

| Problem | Solution |
|---------|----------|
| Application won't start | Right-click the `.exe` → Run as Administrator |
| "Database is locked" error | Close other instances of the app; restart |
| Login fails with default credentials | Delete `GoldSystem.db` and restart for a fresh database |
| Gold rate not updating | Check internet connection; use Manual Entry as a workaround |
| PDF export fails | Ensure the Reports folder exists next to the executable |
| Sync fails between branches | Verify network connectivity and connection strings |
| AI features show no data | Requires 30+ days of transaction history |

### Log Files

Application logs are stored in the `logs/` folder next to the executable.
Daily rotating files: `goldsystem-YYYYMMDD.log`

Logs include:
- Application startup and shutdown
- Database operations
- User actions
- Error stack traces

### Support

For issues not covered here, please open a GitHub Issue:
[https://github.com/Sampatel31/jewelryerpv1/issues](https://github.com/Sampatel31/jewelryerpv1/issues)
