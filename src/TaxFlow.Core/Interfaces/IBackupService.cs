namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Service for database backup and restore operations
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a compressed backup of the SQLite database
    /// </summary>
    Task<BackupResult> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores database from a backup file
    /// </summary>
    Task<RestoreResult> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available backup files
    /// </summary>
    Task<List<BackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes backup files older than specified days
    /// </summary>
    Task<int> DeleteOldBackupsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);
}

public class BackupResult
{
    public bool IsSuccess { get; set; }
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public long OriginalSize { get; set; }
    public double CompressionRatio { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public string? SafetyBackupPath { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long FileSize { get; set; }
    public string FormattedSize { get; set; } = string.Empty;
}
