using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.IO;
using System.IO.Compression;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Backs up and restores the SQLite database file using ZIP compression.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly string _dbPath;

    public BackupService(string? dbPath = null)
    {
        _dbPath = dbPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoldSystem.db");
    }

    // ── Backup ────────────────────────────────────────────────────────────────

    public async Task<string> BackupDatabaseAsync(
        string destinationFolder,
        string description = "",
        CancellationToken ct = default)
    {
        if (!File.Exists(_dbPath))
            throw new FileNotFoundException("Database file not found.", _dbPath);

        Directory.CreateDirectory(destinationFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var zipFileName = $"Backup_{timestamp}.zip";
        var zipFilePath = Path.Combine(destinationFolder, zipFileName);

        // Copy DB to a temp file first (WAL-safe snapshot)
        var tempDb = Path.Combine(Path.GetTempPath(), $"goldsystem_snap_{timestamp}.db");
        try
        {
            File.Copy(_dbPath, tempDb, overwrite: true);

            await Task.Run(() =>
            {
                using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
                archive.CreateEntryFromFile(tempDb, "GoldSystem.db", CompressionLevel.Optimal);

                // Write manifest
                var manifest = $"Backup created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nDescription: {description}\nSource: {_dbPath}";
                var entry = archive.CreateEntry("manifest.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(manifest);
            }, ct);

            return zipFilePath;
        }
        finally
        {
            if (File.Exists(tempDb))
                File.Delete(tempDb);
        }
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    public async Task RestoreDatabaseAsync(string backupZipPath, CancellationToken ct = default)
    {
        if (!File.Exists(backupZipPath))
            throw new FileNotFoundException("Backup file not found.", backupZipPath);

        var extractDir = Path.Combine(Path.GetTempPath(), $"goldsystem_restore_{DateTime.Now:yyyyMMddHHmmss}");
        try
        {
            await Task.Run(() => ZipFile.ExtractToDirectory(backupZipPath, extractDir, overwriteFiles: true), ct);

            var extractedDb = Path.Combine(extractDir, "GoldSystem.db");
            if (!File.Exists(extractedDb))
                throw new InvalidOperationException("Backup archive does not contain a valid database file.");

            // Replace current DB
            File.Copy(extractedDb, _dbPath, overwrite: true);
        }
        finally
        {
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
    }

    // ── Recent Backups ────────────────────────────────────────────────────────

    public Task<IReadOnlyList<BackupMetadata>> GetRecentBackupsAsync(
        string folder,
        int maxCount = 5,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(folder))
            return Task.FromResult<IReadOnlyList<BackupMetadata>>(Array.Empty<BackupMetadata>());

        var files = Directory.GetFiles(folder, "Backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .Take(maxCount)
            .Select(f => new BackupMetadata(f.FullName, f.LastWriteTime, f.Length, string.Empty))
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupMetadata>>(files);
    }

    // ── Delete Old ────────────────────────────────────────────────────────────

    public Task DeleteOldBackupsAsync(
        string folder,
        int keepCount = 5,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(folder))
            return Task.CompletedTask;

        var toDelete = Directory.GetFiles(folder, "Backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .Skip(keepCount);

        foreach (var file in toDelete)
        {
            try { file.Delete(); }
            catch { /* best-effort */ }
        }

        return Task.CompletedTask;
    }

    // ── DB Size ───────────────────────────────────────────────────────────────

    public Task<long> GetDatabaseSizeAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_dbPath))
            return Task.FromResult(0L);

        return Task.FromResult(new FileInfo(_dbPath).Length);
    }
}
