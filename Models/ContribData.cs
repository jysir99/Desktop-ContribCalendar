namespace ContribCalendar.Models;

/// <summary>
/// 贡献数据模型
/// </summary>
public class ContribData
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 文件数量
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// 获取强度等级 (0-4)，用于颜色映射
    /// </summary>
    public int Intensity => FileCount switch
    {
        0 => 0,
        <= 2 => 1,
        <= 5 => 2,
        <= 9 => 3,
        _ => 4
    };
}
