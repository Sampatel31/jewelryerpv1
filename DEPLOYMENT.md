# Deployment Guide

## Prerequisites

| Requirement | Version |
|-------------|---------|
| Windows OS | Windows 10 / Windows Server 2019 or higher |
| .NET 8 Desktop Runtime | 8.0.x |
| Database | SQLite (bundled) **or** SQL Server 2019+ |
| RAM | 4 GB minimum (8 GB recommended) |
| Disk | 500 MB for application + logs |

---

## Installation Steps

### 1. Extract Release Package

Unzip the release archive to your target directory, e.g.:

```
C:\GoldSystem\
```

### 2. Configure the Database

Edit `appsettings.json` in the application directory:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=C:\\GoldSystem\\GoldSystem.db"
  }
}
```

To use SQL Server instead:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GoldSystem;Trusted_Connection=True;"
  }
}
```

### 3. Apply Database Migrations

Open a terminal in the application directory and run:

```bash
dotnet ef database update --project GoldSystem.Data --startup-project GoldSystem.WPF
```

Or, for a self-contained release, the application applies migrations automatically on startup.

### 4. Create the Admin User

1. Launch `GoldSystem.WPF.exe`.
2. Navigate to **Settings → Users**.
3. Click **Add User** and set the role to **Admin**.
4. Set a strong password (minimum 8 characters, upper/lower/digit/special).

### 5. Configure Company Settings

1. Navigate to **Settings → Company**.
2. Enter your company name, GSTIN, address, and bank details.
3. Click **Save**.

### 6. Start the Application

Double-click `GoldSystem.WPF.exe` or create a Windows shortcut.

---

## Post-Installation

### Automated Backups

1. Navigate to **Settings → Backup**.
2. Choose a backup directory (preferably on a different drive or network share).
3. Set the backup interval (daily recommended).
4. Click **Enable Auto-Backup**.

### Email Notifications (Optional)

Add SMTP settings to `appsettings.json`:

```json
{
  "Email": {
    "Host": "smtp.company.com",
    "Port": 587,
    "Username": "noreply@company.com",
    "Password": "your-password"
  }
}
```

### Enable 2FA for Admin Users

1. Navigate to **Settings → Security**.
2. Set **Two-Factor Method** to **Email** or **SMS**.
3. Enforce 2FA for admin and manager roles.

---

## Log Files

Application logs are written to `logs/` in the application directory:

```
logs/goldsystem-20260317.log
logs/goldsystem-20260318.log
...
```

Log files roll daily and are retained for 30 days by default.

---

## Upgrading

1. Stop the application.
2. Back up the database (`GoldSystem.db` or SQL Server database).
3. Extract the new release over the existing installation.
4. Run `dotnet ef database update` if schema migrations are included.
5. Restart the application.

---

## Uninstalling

1. Stop the application.
2. Delete the application directory.
3. Drop the database (SQLite: delete the `.db` file; SQL Server: `DROP DATABASE GoldSystem`).

---

## Troubleshooting

| Symptom | Solution |
|---------|----------|
| Application does not start | Ensure .NET 8 Desktop Runtime is installed |
| Database error on startup | Check the connection string in `appsettings.json` |
| Login fails after upgrade | Reset the admin password via database (hash with PBKDF2/SHA-256 + salt) |
| Slow performance | Check logs for operations > 1 000 ms; increase available RAM |
| Sync not working | Verify network connectivity between branches and check sync logs |
