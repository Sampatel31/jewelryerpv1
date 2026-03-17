# Gold Jewellery Management System — Quick Start Guide

Get up and running in **under 5 minutes** — no installation required.

---

## Step 1 — Download

1. Go to the [Releases page](https://github.com/Sampatel31/jewelryerpv1/releases/latest)
2. Download `GoldJewelleryERP-vX.X.X-win-x64.zip`

## Step 2 — Extract

Extract the ZIP to any folder, for example:

```
C:\GoldERP\
```

## Step 3 — Launch

Double-click `GoldSystem.WPF.exe`.

> **No .NET installation required.** The executable is fully self-contained.

## Step 4 — First Login

The database is created automatically on the very first launch.

| Field    | Value          |
|----------|----------------|
| Username | `admin`        |
| Password | `Admin@123456` |

> ⚠️ **Important:** Change the default password immediately after login.
> Go to **Settings → Users → Change Password**.

## Step 5 — Initial Setup

1. **Company Details** — *Settings → Company* → enter your shop name, address, and GSTIN
2. **Add Inventory** — *Inventory → Add Item* → add your gold items with weight and purity
3. **Set Gold Rate** — *Gold Rate → Manual Entry* → enter today's gold rate
4. **Create First Bill** — *Billing → New Bill* → select a customer and items

---

## System Requirements

| Requirement | Minimum |
|-------------|---------|
| OS          | Windows 10 / 11 (64-bit) |
| RAM         | 4 GB |
| Disk Space  | 200 MB |
| .NET        | Not required (self-contained) |

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| App won't start | Right-click → Run as Administrator |
| Database error | Delete `GoldSystem.db` and restart (fresh database) |
| Login fails | Use default credentials: `admin` / `Admin@123456` |
| Antivirus blocks app | Add folder to antivirus exclusions |

---

## Next Steps

- [Full User Guide](docs/USER_GUIDE.md)
- [Release Notes](RELEASE_NOTES.md)
- [FAQ](docs/FAQ.md)
