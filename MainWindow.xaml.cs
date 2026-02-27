using ContribCalendar.Services;
using ContribCalendar.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ContribCalendar;

public partial class MainWindow : Window
{
    private readonly DatabaseService _db;
    private readonly FileScannerService _scanner;
    private readonly FileWatcherService _watcher;
    private readonly MainViewModel _viewModel;
    private Controls.ContributionGrid? _contributionGrid;

    private const int WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;

    public MainWindow()
    {
        InitializeComponent();

        _db = new DatabaseService();
        _scanner = new FileScannerService(_db);
        _watcher = new FileWatcherService(_db, _scanner);
        _viewModel = new MainViewModel(_db, _scanner, _watcher);

        DataContext = _viewModel;
        _watcher.DataChanged += OnDataChanged;

        Loaded += async (s, e) =>
        {
            ApplyScale(_viewModel.Settings.ScalePercent);
            await PerformScanWithLoadingAsync();
            if (string.IsNullOrEmpty(_viewModel.Settings.ScanPath))
                OpenSettingsWindow();
        };

        Closing += (s, e) =>
        {
            _watcher.Dispose();
            TrayIcon.Dispose();
        };

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            handled = true;
            return new IntPtr(HTCLIENT);
        }
        return IntPtr.Zero;
    }

    public void ApplyScale(int scalePercent)
    {
        double scale = Math.Clamp(scalePercent, 50, 200) / 100.0;
        Width = 720 * scale;
        Height = 125 * scale;
    }

    public async Task PerformScanWithLoadingAsync()
    {
        ShowLoading();
        try
        {
            await _viewModel.PerformInitialScanAsync();

            if (_contributionGrid == null)
            {
                _contributionGrid = new Controls.ContributionGrid(_scanner, _db);
                ContentGrid.Children.Add(_contributionGrid);
            }
            else
            {
                _contributionGrid.UpdateDisplay();
            }
        }
        finally
        {
            HideLoading();
        }
    }

    private void ShowLoading() => LoadingOverlay.Visibility = Visibility.Visible;
    private void HideLoading() => LoadingOverlay.Visibility = Visibility.Collapsed;

    private void OnDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void OnDataChanged()
    {
        Dispatcher.Invoke(() => _contributionGrid?.UpdateDisplay());
    }

    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
    {
        if (IsVisible) Hide();
        else { Show(); Activate(); }
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e)
    {
        if (IsVisible) Hide();
        else { Show(); Activate(); }
    }

    public void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        AppContextMenu.PlacementTarget = sender as UIElement;
        AppContextMenu.IsOpen = true;
    }

    private void ContextMenu_Closed(object sender, RoutedEventArgs e) { }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await PerformScanWithLoadingAsync();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

    private void OpenSettingsWindow()
    {
        var settingsWindow = new Views.SettingsWindow(_viewModel);
        settingsWindow.Owner = this;
        settingsWindow.Closed += (s, e) =>
        {
            ApplyScale(_viewModel.Settings.ScalePercent);
            _contributionGrid?.UpdateDisplay();
        };
        settingsWindow.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
