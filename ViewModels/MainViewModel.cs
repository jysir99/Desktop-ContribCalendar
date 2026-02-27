using ContribCalendar.Models;
using ContribCalendar.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;

namespace ContribCalendar.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _db;
    private readonly FileScannerService _scanner;
    private readonly FileWatcherService _watcher;

    private AppSettings _settings = new();

    public MainViewModel(DatabaseService db, FileScannerService scanner, FileWatcherService watcher)
    {
        _db = db;
        _scanner = scanner;
        _watcher = watcher;

        LoadSettings();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        var scanPath = _db.GetSetting("ScanPath");
        var currentYear = _db.GetSetting("CurrentYear");
        var scalePercent = _db.GetSetting("ScalePercent");
        var ignoredFolders = _db.GetIgnoredFolders();

        _settings = new AppSettings
        {
            ScanPath = scanPath ?? string.Empty,
            CurrentYear = int.TryParse(currentYear, out var year) ? year : DateTime.Now.Year,
            ScalePercent = int.TryParse(scalePercent, out var scale) ? Math.Clamp(scale, 50, 200) : 100,
            IgnoredFolders = ignoredFolders.Any() ? ignoredFolders : _settings.IgnoredFolders
        };

        // 如果有扫描路径，启动监控
        if (!string.IsNullOrEmpty(_settings.ScanPath) && Directory.Exists(_settings.ScanPath))
        {
            _watcher.StartWatching(_settings.ScanPath, _settings.IgnoredFolders);
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings()
    {
        _db.SaveSetting("ScanPath", _settings.ScanPath);
        _db.SaveSetting("CurrentYear", _settings.CurrentYear.ToString());
        _db.SaveSetting("ScalePercent", _settings.ScalePercent.ToString());
        _db.SetIgnoredFolders(_settings.IgnoredFolders);

        // 重启监控
        if (!string.IsNullOrEmpty(_settings.ScanPath) && Directory.Exists(_settings.ScanPath))
        {
            _watcher.StartWatching(_settings.ScanPath, _settings.IgnoredFolders);
        }
    }

    /// <summary>
    /// 执行初始扫描
    /// </summary>
    public async Task PerformInitialScanAsync()
    {
        if (!string.IsNullOrEmpty(_settings.ScanPath) && Directory.Exists(_settings.ScanPath))
        {
            await _scanner.ScanDirectoryAsync(_settings.ScanPath, _settings.IgnoredFolders);
        }
    }

    public AppSettings Settings => _settings;
    public FileScannerService Scanner => _scanner;
    public FileWatcherService Watcher => _watcher;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
