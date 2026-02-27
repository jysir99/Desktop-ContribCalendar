namespace ContribCalendar.Models;

/// <summary>
/// 文件夹记录模型
/// </summary>
public class FolderRecord
{
    /// <summary>
    /// 数据库ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 文件夹完整路径
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 解析后的日期 (yyyy-MM-dd 格式)
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 文件数量
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// 文件夹最后修改时间 (Unix 时间戳)
    /// </summary>
    public long LastModified { get; set; }

    /// <summary>
    /// 扫描时间 (Unix 时间戳)
    /// </summary>
    public long ScanTime { get; set; }
}
