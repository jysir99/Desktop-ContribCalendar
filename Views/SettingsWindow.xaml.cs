using ContribCalendar.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace ContribCalendar.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        LoadIgnoredFoldersList();

        // 设置滑块初始值和事件
        ScaleSlider.Value = _viewModel.Settings.ScalePercent;
        ScaleLabel.Text = $"{_viewModel.Settings.ScalePercent}%";
        ScaleSlider.ValueChanged += ScaleSlider_ValueChanged;

        IgnoredFoldersListBox.SelectionChanged += (s, e) =>
        {
            RemoveIgnoreButton.IsEnabled = IgnoredFoldersListBox.SelectedItem != null;
        };

        UpdateStatus();
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int val = (int)e.NewValue;
        _viewModel.Settings.ScalePercent = val;
        if (ScaleLabel != null)
            ScaleLabel.Text = $"{val}%";
    }

    private void LoadIgnoredFoldersList()
    {
        IgnoredFoldersListBox.ItemsSource = null;
        IgnoredFoldersListBox.ItemsSource = _viewModel.Settings.IgnoredFolders;
    }

    private void UpdateStatus()
    {
        StatusPathText.Text = string.IsNullOrEmpty(_viewModel.Settings.ScanPath)
            ? "未设置"
            : _viewModel.Settings.ScanPath;

        StatusDbText.Text = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.db");
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择要扫描的目录" };
        if (dialog.ShowDialog() == true)
        {
            ScanPathTextBox.Text = dialog.FolderName;
            _viewModel.Settings.ScanPath = dialog.FolderName;
            UpdateStatus();
        }
    }

    private void AddIgnoreButton_Click(object sender, RoutedEventArgs e)
    {
        var pattern = NewIgnoreFolderTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(pattern))
        {
            _viewModel.Settings.IgnoredFolders.Add(pattern);
            LoadIgnoredFoldersList();
            NewIgnoreFolderTextBox.Clear();
        }
    }

    private void NewIgnoreFolderTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddIgnoreButton_Click(sender, e);
    }

    private void RemoveIgnoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (IgnoredFoldersListBox.SelectedItem is string pattern)
        {
            _viewModel.Settings.IgnoredFolders.Remove(pattern);
            LoadIgnoredFoldersList();
        }
    }

    private async void ScanNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.Settings.ScanPath))
        {
            MessageBox.Show("请先设置扫描路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!System.IO.Directory.Exists(_viewModel.Settings.ScanPath))
        {
            MessageBox.Show("扫描路径不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ScanNowButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        try
        {
            await _viewModel.PerformInitialScanAsync();
            MessageBox.Show("扫描完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"扫描失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ScanNowButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var path = ScanPathTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(path) && !System.IO.Directory.Exists(path))
        {
            MessageBox.Show("扫描路径不存在，请检查", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ScanNowButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        try
        {
            _viewModel.Settings.ScanPath = path;
            _viewModel.SaveSettings();
            UpdateStatus();

            if (!string.IsNullOrEmpty(path))
                await _viewModel.PerformInitialScanAsync();

            MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ScanNowButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
        }
    }
}
