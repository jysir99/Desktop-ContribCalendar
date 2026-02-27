using System.Text.RegularExpressions;

namespace ContribCalendar.Utils;

/// <summary>
/// 文件夹名称解析工具
/// </summary>
public static class DirectoryNameParser
{
    /// <summary>
    /// 匹配 "xxx年xx月xx日" 格式的正则表达式（允许后面有额外内容，如 _tools）
    /// </summary>
    private static readonly Regex DateFolderRegex = new(
        @"^(\d{4})年(\d{1,2})月(\d{1,2})日",
        RegexOptions.Compiled);

    /// <summary>
    /// 尝试从文件夹名称解析日期
    /// </summary>
    /// <param name="folderName">文件夹名称</param>
    /// <returns>解析成功返回日期，失败返回 null</returns>
    public static DateOnly? TryParseDate(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return null;

        var match = DateFolderRegex.Match(folderName);
        if (!match.Success)
            return null;

        try
        {
            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);

            // 验证日期有效性
            if (year < 1 || year > 9999 ||
                month < 1 || month > 12 ||
                day < 1 || day > DateTime.DaysInMonth(year, month))
                return null;

            return new DateOnly(year, month, day);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 检查文件夹名称是否匹配日期格式
    /// </summary>
    public static bool IsDateFolder(string folderName)
    {
        return TryParseDate(folderName).HasValue;
    }

    /// <summary>
    /// 检查文件夹是否应该被忽略
    /// </summary>
    public static bool ShouldIgnore(string folderName, List<string> ignoredPatterns)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return true;

        // 检查精确匹配
        if (ignoredPatterns.Contains(folderName))
            return true;

        // 检查通配符匹配
        foreach (var pattern in ignoredPatterns)
        {
            if (pattern.Contains('*'))
            {
                var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(folderName, regexPattern, RegexOptions.IgnoreCase))
                    return true;
            }
            else if (folderName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
