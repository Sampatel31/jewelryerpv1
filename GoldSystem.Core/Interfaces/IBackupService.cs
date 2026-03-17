using GoldSystem.Core.Models;

namespace GoldSystem.Core.Interfaces;

/// <summary>
/// Provides database backup and restore functionality with ZIP compression.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a backup of the database, compresses it to a ZIP file,
    /// and saves it under <paramref name="destinationFolder"/>.
    /// </summary>
    /// <returns>Full path to the created backup ZIP file.</returns>
    Task<string> BackupDatabaseAsync(
        string destinationFolder,
        string description = "",
        CancellationToken ct = default);

    /// <summary>
    /// Restores the database from the given ZIP backup file.
    /// The current database file is replaced after extraction.
    /// </summary>
    Task RestoreDatabaseAsync(string backupZipPath, CancellationToken ct = default);

    /// <summary>Returns metadata for recent backup files in the folder (newest first).</summary>
    Task<IReadOnlyList<BackupMetadata>> GetRecentBackupsAsync(
        string folder,
        int maxCount = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes old backup files, keeping only the <paramref name="keepCount"/> most recent ones.
    /// </summary>
    Task DeleteOldBackupsAsync(
        string folder,
        int keepCount = 5,
        CancellationToken ct = default);

    /// <summary>Returns the file size in bytes of the current database file.</summary>
    Task<long> GetDatabaseSizeAsync(CancellationToken ct = default);
}
