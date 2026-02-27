using ContribCalendar.Models;
using ContribCalendar.Services;
using ContribCalendar.Utils;
using System.IO;

namespace ContribCalendar.Services;

/// <summary>
/// 文件扫描服务
/// </summary>
public class FileScannerService
{
    private readonly DatabaseService _db;

    public FileScannerService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// 扫描目录，增量更新数据库
    /// </summary>
    public async Task ScanDirectoryAsync(string path, List<string> ignoredFolders, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
            return;

        await Task.Run(() =>
        {
            ScanDirectoryInternal(path, ignoredFolders, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// 内部扫描实现
    /// </summary>
    private void ScanDirectoryInternal(string path, List<string> ignoredFolders, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var recordsToUpsert = new List<FolderRecord>();
        var pathsInDb = _db.GetAllFolderRecords().ToDictionary(r => r.FolderPath);

        // 递归扫描所有子目录
        ScanRecursive(path, ignoredFolders, recordsToUpsert, pathsInDb, now, cancellationToken);

        // 批量更新数据库
        if (recordsToUpsert.Count > 0)
        {
            _db.UpsertRecords(recordsToUpsert);
        }

        // 检查已删除的文件夹
        var scannedPaths = new HashSet<string>(recordsToUpsert.Select(r => r.FolderPath));
        foreach (var dbPath in pathsInDb.Keys)
        {
            if (!scannedPaths.Contains(dbPath) && !Directory.Exists(dbPath))
            {
                _db.DeleteRecord(dbPath);
            }
        }
    }

    /// <summary>
    /// 递归扫描目录
    /// </summary>
    private void ScanRecursive(
        string currentPath,
        List<string> ignoredFolders,
        List<FolderRecord> recordsToUpsert,
        Dictionary<string, FolderRecord> pathsInDb,
        long scanTime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 检查当前目录是否应该忽略
        var dirName = Path.GetFileName(currentPath);
        if (DirectoryNameParser.ShouldIgnore(dirName, ignoredFolders))
        {
            return;
        }

        // 检查是否是日期格式文件夹
        var date = DirectoryNameParser.TryParseDate(dirName);

        try
        {
            // 获取当前目录信息
            var dirInfo = new DirectoryInfo(currentPath);
            var lastModified = new DateTimeOffset(dirInfo.LastWriteTimeUtc).ToUnixTimeMilliseconds();

            if (date.HasValue)
            {
                // 是日期文件夹，统计文件数
                var fileCount = CountFiles(currentPath, ignoredFolders);

                // 检查是否需要更新
                var needsUpdate = true;
                if (pathsInDb.TryGetValue(currentPath, out var existingRecord))
                {
                    // 如果文件数和修改时间都相同，则不需要更新
                    needsUpdate = existingRecord.FileCount != fileCount ||
                                  existingRecord.LastModified != lastModified;
                }

                if (needsUpdate)
                {
                    recordsToUpsert.Add(new FolderRecord
                    {
                        FolderPath = currentPath,
                        FolderName = dirName,
                        Date = date.Value.ToString("yyyy-MM-dd"),
                        FileCount = fileCount,
                        LastModified = lastModified,
                        ScanTime = scanTime
                    });
                }
            }

            // 递归扫描子目录
            foreach (var subDir in Directory.EnumerateDirectories(currentPath))
            {
                ScanRecursive(subDir, ignoredFolders, recordsToUpsert, pathsInDb, scanTime, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 无访问权限，跳过
        }
        catch (Exception)
        {
            // 其他错误，跳过
        }
    }

    /// <summary>
    /// 统计目录中的文件数量（递归，但忽略特定文件夹）
    /// </summary>
    private int CountFiles(string path, List<string> ignoredFolders)
    {
        var count = 0;

        try
        {
            // 统计当前目录下的文件
            count += Directory.EnumerateFiles(path).Count();

            // 递归统计子目录
            foreach (var subDir in Directory.EnumerateDirectories(path))
            {
                var dirName = Path.GetFileName(subDir);
                if (!DirectoryNameParser.ShouldIgnore(dirName, ignoredFolders))
                {
                    count += CountFiles(subDir, ignoredFolders);
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return count;
    }

    /// <summary>
    /// 获取指定年份的贡献数据
    /// </summary>
    public List<ContribData> GetContribDataByYear(int year)
    {
        var records = _db.GetRecordsByYear(year);
        var contribData = new List<ContribData>();

        // 按日期分组合并数据
        var grouped = records.GroupBy(r => r.Date);

        foreach (var group in grouped)
        {
            if (DateOnly.TryParse(group.Key, out var date))
            {
                contribData.Add(new ContribData
                {
                    Date = date,
                    FileCount = group.Sum(r => r.FileCount)
                });
            }
        }

        return contribData;
    }

    /// <summary>
    /// 获取所有年份列表
    /// </summary>
    public List<int> GetAvailableYears()
    {
        var records = _db.GetAllFolderRecords();
        return records
            .Select(r => int.TryParse(r.Date.Substring(0, 4), out var year) ? year : 0)
            .Where(y => y > 0)
            .Distinct()
            .OrderBy(y => y)
            .ToList();
    }
}
