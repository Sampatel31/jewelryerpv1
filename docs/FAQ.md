# FAQ — Gold Jewellery Management System

---

## General Questions

**Q: Does the application require .NET to be installed?**

No. The download from the Releases page is a **self-contained** Windows executable. All required
.NET 8 runtime files are bundled inside the single `.exe` file. Just download, extract, and run.

---

**Q: What operating systems are supported?**

Windows 10 and Windows 11 (64-bit) only. The WPF framework requires Windows.

---

**Q: Where is the database stored?**

The SQLite database file (`GoldSystem.db`) is stored in the same folder as the executable.
You can back it up by simply copying this file.

---

**Q: Can I use SQL Server instead of SQLite?**

Yes. Edit `appsettings.json` and change the connection string under `ConnectionStrings:Default`
to point to your SQL Server instance. Then run `dotnet ef database update` to create the schema.

---

**Q: What are the default login credentials?**

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `Admin@123456` |

Change these immediately after first login.

---

## Billing Questions

**Q: How is the gold value calculated?**

```
Item Value = Weight (g) × Gold Rate (₹/g) × Purity Factor
```

Purity factors: 24K = 1.0, 22K = 0.9167, 18K = 0.75, 14K = 0.585

---

**Q: Can I apply a discount on a bill?**

Yes. In the billing screen, enter a flat amount or percentage discount before saving.

---

**Q: Can I process partial payments?**

Yes. Select **Multiple** as the payment mode and split across Cash, Card, UPI, etc.

---

**Q: How do I handle old gold exchange?**

In the billing screen, click **Add Old Gold**, enter the weight and purity of the exchanged
gold, and the system deducts the exchange value from the bill total.

---

## Inventory Questions

**Q: What is HUID?**

HUID (Hallmark Unique Identification) is a 6-character alphanumeric code assigned by BIS
(Bureau of Indian Standards) to each hallmarked jewellery item. It is mandatory for 22K and
higher purity items sold in India.

---

**Q: How do I transfer stock between branches?**

Go to **Inventory → Stock Transfer**, select the source branch, destination branch, items,
and quantities, then click **Transfer**. The sync engine will push the transfer to the
destination branch.

---

**Q: What triggers a low-stock alert?**

When an item's current quantity falls below its configured **Minimum Stock Level**, it
appears highlighted on the Inventory screen and as an alert on the Dashboard.

---

## Security Questions

**Q: How are passwords stored?**

Passwords are stored as PBKDF2/SHA-256 hashes with a 128-bit random salt and 100,000
iterations. No plain-text passwords are ever stored.

---

**Q: How does 2FA work?**

The system uses TOTP (Time-based One-Time Password). After enabling 2FA for a user account,
scan the QR code with Google Authenticator, Microsoft Authenticator, or any TOTP app.
On each login, you will be prompted for the current 6-digit code.

---

**Q: Can I restrict what a user can see or do?**

Yes. The system supports three built-in roles (Admin, Manager, Operator) with different
permission levels. Custom roles with granular permissions can also be created in
**Settings → Security → Roles**.

---

## Reporting Questions

**Q: What reports are available?**

- Daily Sales Report (PDF/Excel)
- Customer Statement (PDF/Excel)
- Inventory Valuation Report (PDF/Excel)
- GSTR-1 Export (Excel)
- Audit Trail (PDF/Excel)
- AI Sales Forecast (PDF/Excel)

---

**Q: Where are generated reports saved?**

By default, reports are saved to the `Reports/` folder next to the executable. You can
change this in **Settings → Advanced → Report Output Path**.

---

## AI Features

**Q: How accurate is the sales forecast?**

Accuracy improves with more historical data. A minimum of 30 days of transaction data is
recommended. With 6+ months of data, forecasts are typically within 10–15% of actual sales.

---

**Q: What counts as an "anomaly"?**

The anomaly detector flags transactions that deviate significantly from historical patterns —
for example, an unusually large bill, a sale at an unusual time, or an inventory change that
doesn't match billing records.

---

## Technical Questions

**Q: Where are log files stored?**

Log files are in the `logs/` folder next to the executable, named `goldsystem-YYYYMMDD.log`.
Logs rotate daily and are retained for 30 days.

---

**Q: How do I perform a backup?**

Go to **Settings → Backup** and click **Backup Now**. You can also schedule automatic daily
or weekly backups. Backups are ZIP archives stored in the `Backups/` folder.

---

**Q: The application is slow — what can I do?**

1. Ensure you have at least 4 GB of RAM (8 GB recommended if using AI features)
2. Run the application on an SSD if possible
3. Reduce the cache TTL settings if memory is limited
4. Close other memory-intensive applications
5. Check the log files for performance warnings

---

**Q: How do I report a bug or request a feature?**

Open a GitHub Issue: [https://github.com/Sampatel31/jewelryerpv1/issues](https://github.com/Sampatel31/jewelryerpv1/issues)
