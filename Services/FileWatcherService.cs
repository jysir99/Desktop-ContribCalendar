using ContribCalendar.Models;
using ContribCalendar.Services;
using ContribCalendar.Utils;
using System.IO;

namespace ContribCalendar.Services;

/// <summary>
/// 文件变化监控服务
/// </summary>
public class FileWatcherService : IDisposable
{
    private readonly DatabaseService _db;
    private readonly FileScannerService _scanner;
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private readonly object _lock = new();
    private string? _currentPath;
    private List<string> _ignoredFolders = new();

    public event Action? DataChanged;

    public FileWatcherService(DatabaseService db, FileScannerService scanner)
    {
        _db = db;
        _scanner = scanner;
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void StartWatching(string path, List<string> ignoredFolders)
    {
        StopWatching();

        if (!Directory.Exists(path))
            return;

        _currentPath = path;
        _ignoredFolders = ignoredFolders;

        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Changed += OnChanged;
        _watcher.Error += OnError;

        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void StopWatching()
    {
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnChanged;
                _watcher.Deleted -= OnChanged;
                _watcher.Renamed -= OnRenamed;
                _watcher.Changed -= OnChanged;
                _watcher.Error -= OnError;
                _watcher.Dispose();
                _watcher = null;
            }

            _currentPath = null;
        }
    }

    /// <summary>
    /// 文件系统变化事件处理（防抖）
    /// </summary>
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldProcessEvent(e.FullPath))
        {
            ScheduleUpdate();
        }
    }

    /// <summary>
    /// 重命名事件处理
    /// </summary>
    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldProcessEvent(e.FullPath) || ShouldProcessEvent(e.OldFullPath))
        {
            ScheduleUpdate();
        }
    }

    /// <summary>
    /// 错误事件处理
    /// </summary>
    private void OnError(object sender, ErrorEventArgs e)
    {
        // 记录错误或重启监控
        if (_currentPath != null && _watcher != null)
        {
            try
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.EnableRaisingEvents = true;
            }
            catch
            {
                // 忽略错误
            }
        }
    }

    /// <summary>
    /// 判断是否应该处理该事件
    /// </summary>
    private bool ShouldProcessEvent(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var dirName = Path.GetFileName(path);

        // 如果是日期格式文件夹，需要处理
        if (DirectoryNameParser.IsDateFolder(dirName))
            return true;

        // 检查是否在忽略列表中
        if (DirectoryNameParser.ShouldIgnore(dirName, _ignoredFolders))
            return false;

        return true;
    }

    /// <summary>
    /// 调度更新（防抖，延迟500ms执行）
    /// </summary>
    private void ScheduleUpdate()
    {
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                TriggerUpdate();
            }, null, 500, Timeout.Infinite);
        }
    }

    /// <summary>
    /// 触发更新
    /// </summary>
    private void TriggerUpdate()
    {
        if (_currentPath == null)
            return;

        try
        {
            // 异步扫描更新
            Task.Run(async () =>
            {
                await _scanner.ScanDirectoryAsync(_currentPath, _ignoredFolders);
                DataChanged?.Invoke();
            });
        }
        catch
        {
            // 忽略错误
        }
    }

    public void Dispose()
    {
        StopWatching();
        _debounceTimer?.Dispose();
    }
}
