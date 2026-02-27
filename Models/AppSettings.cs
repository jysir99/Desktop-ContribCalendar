namespace ContribCalendar.Models;

/// <summary>
/// 应用设置模型
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 要扫描的目录路径
    /// </summary>
    public string ScanPath { get; set; } = string.Empty;

    /// <summary>
    /// 要忽略的文件夹模式列表
    /// </summary>
    public List<string> IgnoredFolders { get; set; } = new()
    {
        "node_modules",
        ".git",
        "bin",
        "obj",
        ".vs",
        "debug",
        "release",
        "__pycache__",
        ".venv",
        "venv",
        "env"
    };

    /// <summary>
    /// 当前显示的年份
    /// </summary>
    public int CurrentYear { get; set; } = DateTime.Now.Year;

    /// <summary>
    /// 显示缩放比例 50~200，默认100
    /// </summary>
    public int ScalePercent { get; set; } = 100;
}
