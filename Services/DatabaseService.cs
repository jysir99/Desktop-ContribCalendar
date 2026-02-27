using Dapper;
using ContribCalendar.Models;
using System.Data.SQLite;
using System.IO;

namespace ContribCalendar.Services;

/// <summary>
/// 数据库服务
/// </summary>
public class DatabaseService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public DatabaseService()
    {
        // 数据库存储在程序运行目录
        var appDataPath = AppDomain.CurrentDomain.BaseDirectory;

        _dbPath = Path.Combine(appDataPath, "data.db");
        _connectionString = $"Data Source={_dbPath};Version=3;";

        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        // 文件夹记录表
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS FolderRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FolderPath TEXT NOT NULL UNIQUE,
                FolderName TEXT NOT NULL,
                Date TEXT NOT NULL,
                FileCount INTEGER NOT NULL,
                LastModified INTEGER NOT NULL,
                ScanTime INTEGER NOT NULL
            )");

        // 设置表
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )");

        // 忽略文件夹列表
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS IgnoredFolders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Pattern TEXT NOT NULL
            )");
    }

    /// <summary>
    /// 获取所有文件夹记录
    /// </summary>
    public IEnumerable<FolderRecord> GetAllFolderRecords()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection.Query<FolderRecord>("SELECT * FROM FolderRecords");
    }

    /// <summary>
    /// 获取指定日期范围内的记录
    /// </summary>
    public IEnumerable<FolderRecord> GetRecordsByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection.Query<FolderRecord>(
            "SELECT * FROM FolderRecords WHERE Date >= @Start AND Date <= @End",
            new { Start = start.ToString("yyyy-MM-dd"), End = end.ToString("yyyy-MM-dd") });
    }

    /// <summary>
    /// 获取指定年份的记录
    /// </summary>
    public IEnumerable<FolderRecord> GetRecordsByYear(int year)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        return GetRecordsByDateRange(start, end);
    }

    /// <summary>
    /// 获取单条记录
    /// </summary>
    public FolderRecord? GetRecordByPath(string folderPath)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection.QueryFirstOrDefault<FolderRecord>(
            "SELECT * FROM FolderRecords WHERE FolderPath = @Path",
            new { Path = folderPath });
    }

    /// <summary>
    /// 插入或更新记录
    /// </summary>
    public void UpsertRecord(FolderRecord record)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        connection.Execute(@"
            INSERT OR REPLACE INTO FolderRecords
            (FolderPath, FolderName, Date, FileCount, LastModified, ScanTime)
            VALUES (@FolderPath, @FolderName, @Date, @FileCount, @LastModified, @ScanTime)",
            record);
    }

    /// <summary>
    /// 批量插入或更新记录
    /// </summary>
    public void UpsertRecords(IEnumerable<FolderRecord> records)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var record in records)
            {
                connection.Execute(@"
                    INSERT OR REPLACE INTO FolderRecords
                    (FolderPath, FolderName, Date, FileCount, LastModified, ScanTime)
                    VALUES (@FolderPath, @FolderName, @Date, @FileCount, @LastModified, @ScanTime)",
                    record, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 删除记录
    /// </summary>
    public void DeleteRecord(string folderPath)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        connection.Execute("DELETE FROM FolderRecords WHERE FolderPath = @Path",
            new { Path = folderPath });
    }

    /// <summary>
    /// 获取设置值
    /// </summary>
    public string? GetSetting(string key)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection.QueryFirstOrDefault<string>(
            "SELECT Value FROM Settings WHERE [Key] = @Key",
            new { Key = key });
    }

    /// <summary>
    /// 保存设置值
    /// </summary>
    public void SaveSetting(string key, string value)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        connection.Execute(
            "INSERT OR REPLACE INTO Settings ([Key], Value) VALUES (@Key, @Value)",
            new { Key = key, Value = value });
    }

    /// <summary>
    /// 获取忽略文件夹列表
    /// </summary>
    public List<string> GetIgnoredFolders()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection.Query<string>("SELECT Pattern FROM IgnoredFolders").ToList();
    }

    /// <summary>
    /// 添加忽略文件夹模式
    /// </summary>
    public void AddIgnoredFolder(string pattern)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        connection.Execute("INSERT INTO IgnoredFolders (Pattern) VALUES (@Pattern)",
            new { Pattern = pattern });
    }

    /// <summary>
    /// 移除忽略文件夹模式
    /// </summary>
    public void RemoveIgnoredFolder(string pattern)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        connection.Execute("DELETE FROM IgnoredFolders WHERE Pattern = @Pattern",
            new { Pattern = pattern });
    }

    /// <summary>
    /// 设置忽略文件夹列表
    /// </summary>
    public void SetIgnoredFolders(List<string> patterns)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            connection.Execute("DELETE FROM IgnoredFolders", transaction);

            foreach (var pattern in patterns)
            {
                connection.Execute("INSERT INTO IgnoredFolders (Pattern) VALUES (@Pattern)",
                    new { Pattern = pattern }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
