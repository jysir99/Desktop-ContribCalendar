using ContribCalendar.Models;
using ContribCalendar.Services;
using ContribCalendar.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Diagnostics;

namespace ContribCalendar.Controls;

/// <summary>
/// ContributionGrid.xaml 的交互逻辑
/// </summary>
public partial class ContributionGrid : UserControl
{
    private readonly FileScannerService _scanner;
    private readonly DatabaseService _db;
    private int _currentYear = DateTime.Now.Year;
    private const int CellSize = 10;
    private const int CellSpacing = 2;
    private const int MonthGap = 12; // 月间隙
    private readonly IntensityColorConverter _colorConverter = new();

    public ContributionGrid(FileScannerService scanner, DatabaseService db)
    {
        InitializeComponent();
        _scanner = scanner;
        _db = db;
        UpdateDisplay();
    }

    /// <summary>
    /// 顶部栏拖动事件
    /// </summary>
    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var mainWindow = Window.GetWindow(this);
        if (mainWindow != null && e.LeftButton == MouseButtonState.Pressed)
        {
            mainWindow.DragMove();
        }
    }

    /// <summary>
    /// 刷新按钮点击
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                await mainWindow.PerformScanWithLoadingAsync();
            }
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 菜单按钮点击
    /// </summary>
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this) as MainWindow;
        if (mainWindow != null)
        {
            mainWindow.MenuButton_Click(sender, e);
        }
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    public void UpdateDisplay()
    {
        // 更新年份下拉框
        UpdateYearComboBox();

        // 获取该年的贡献数据
        var contribData = _scanner.GetContribDataByYear(_currentYear);

        // 创建网格数据
        var gridData = CreateGridData(contribData);

        // 填充贡献网格
        FillContributionsGrid(gridData);

        // 添加月份标签
        AddMonthLabels(gridData);
    }

    /// <summary>
    /// 更新年份列表
    /// </summary>
    private void UpdateYearComboBox()
    {
        var availableYears = _scanner.GetAvailableYears();
        var currentYear = DateTime.Now.Year;

        // 显示近5年或所有可用年份
        var yearsToShow = availableYears
            .Where(y => y >= currentYear - 5 && y <= currentYear)
            .OrderByDescending(y => y)
            .ToList();

        // 如果当前年份不在列表中，添加它
        if (!yearsToShow.Contains(_currentYear))
        {
            yearsToShow.Add(_currentYear);
            yearsToShow = yearsToShow.OrderByDescending(y => y).ToList();
        }

        YearListPanel.Children.Clear();

        foreach (var year in yearsToShow)
        {
            var yearText = new TextBlock
            {
                Text = year.ToString(),
                Tag = year
            };

            // 应用样式：当前选中的年份使用 SelectedYearTextStyle
            if (year == _currentYear)
            {
                yearText.Style = Resources["SelectedYearTextStyle"] as Style;
            }
            else
            {
                yearText.Style = Resources["YearTextStyle"] as Style;
            }

            // 使用鼠标左键点击事件
            yearText.MouseLeftButtonDown += (s, e) =>
            {
                if (yearText.Tag is int selectedYear)
                {
                    _currentYear = selectedYear;
                    UpdateDisplay();
                    e.Handled = true;
                }
            };

            YearListPanel.Children.Add(yearText);
        }
    }

    /// <summary>
    /// 创建网格数据 - 从周一开始
    /// </summary>
    private List<CellData> CreateGridData(List<ContribData> contribData)
    {
        var gridData = new List<CellData>();

        var startDate = new DateOnly(_currentYear, 1, 1);
        var endDate = new DateOnly(_currentYear, 12, 31);

        // 找到包含1月1日的那周的周一
        var firstMonday = startDate;
        while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            firstMonday = firstMonday.AddDays(-1);

        // 找到包含12月31日的那周的周日
        var lastSunday = endDate;
        while (lastSunday.DayOfWeek != DayOfWeek.Sunday)
            lastSunday = lastSunday.AddDays(1);

        var totalDays = (int)(lastSunday.DayNumber - firstMonday.DayNumber) + 1;
        var totalWeeks = Math.Min(totalDays / 7, 53);

        // row 0 = 周一, row 1 = 周二, ..., row 6 = 周日
        for (int row = 0; row < 7; row++)
        {
            for (int week = 0; week < totalWeeks; week++)
            {
                var currentDate = firstMonday.AddDays(week * 7 + row);

                if (currentDate.Year == _currentYear)
                {
                    var data = contribData?.FirstOrDefault(d => d.Date == currentDate);
                    var fileCount = data?.FileCount ?? 0;

                    gridData.Add(new CellData
                    {
                        Intensity = fileCount switch
                        {
                            0 => 0,
                            <= 2 => 1,
                            <= 5 => 2,
                            <= 9 => 3,
                            _ => 4
                        },
                        ToolTip = $"{currentDate:MMM d, yyyy}: {fileCount} contribution{(fileCount == 1 ? "" : "s")}",
                        Date = currentDate,
                        Week = week,
                        DayOfWeek = row,
                        Month = currentDate.Month
                    });
                }
                else
                {
                    gridData.Add(new CellData
                    {
                        Intensity = -1,
                        Date = currentDate,
                        Week = week,
                        DayOfWeek = row,
                        Month = currentDate.Month
                    });
                }
            }
        }

        return gridData;
    }

    /// <summary>
    /// 填充贡献网格 - GitHub 原始样式
    /// </summary>
    private void FillContributionsGrid(List<CellData> gridData)
    {
        ContributionsGrid.Children.Clear();
        ContributionsGrid.RowDefinitions.Clear();
        ContributionsGrid.ColumnDefinitions.Clear();

        if (gridData.Count == 0) return;

        int totalWeeks = gridData.Max(c => c.Week) + 1;

        // 创建7行
        for (int row = 0; row < 7; row++)
            ContributionsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize + CellSpacing) });

        // 创建列
        for (int week = 0; week < totalWeeks; week++)
            ContributionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize + CellSpacing) });

        // 填充单元格
        foreach (var cellData in gridData)
        {
            var cell = new Border
            {
                Width = CellSize,
                Height = CellSize,
                CornerRadius = new CornerRadius(2),
                ToolTip = cellData.ToolTip,
                Tag = cellData.Date
            };
            cell.Background = _colorConverter.Convert(cellData.Intensity, null, null, null) as Brush ?? Brushes.Transparent;

            Grid.SetRow(cell, cellData.DayOfWeek);
            Grid.SetColumn(cell, cellData.Week);

            if (cellData.Intensity > 0)
            {
                cell.Cursor = Cursors.Hand;
                var date = cellData.Date;
                cell.MouseLeftButtonDown += (s, e) =>
                {
                    OnDayClick(date);
                    e.Handled = true;
                };
            }

            ContributionsGrid.Children.Add(cell);
        }
    }

    /// <summary>
    /// 单元格点击事件处理
    /// </summary>
    private void Cell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border cell && cell.Tag is DateOnly date)
        {
            OnDayClick(date);
        }
    }

    /// <summary>
    /// 点击有贡献的日期
    /// </summary>
    private void OnDayClick(DateOnly date)
    {
        try
        {
            var folders = GetFoldersForDate(date);
            if (folders.Count == 0) return;

            if (folders.Count == 1)
            {
                OpenFolder(folders[0]);
            }
            else
            {
                var picker = new Views.FolderPickerWindow(date, folders)
                {
                    Owner = Window.GetWindow(this)
                };
                picker.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开文件夹失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 获取指定日期的文件夹列表
    /// </summary>
    private List<string> GetFoldersForDate(DateOnly date)
    {
        var dateString = date.ToString("yyyy-MM-dd");
        var records = _db.GetRecordsByYear(_currentYear);
        return records
            .Where(r => r.Date == dateString)
            .Select(r => r.FolderPath)
            .ToList();
    }

    /// <summary>
    /// 打开文件夹
    /// </summary>
    private void OpenFolder(string folderPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = folderPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开文件夹:\n{folderPath}\n\n{ex.Message}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 添加月份标签
    /// </summary>
    private void AddMonthLabels(List<CellData> gridData)
    {
        MonthsPanel.Children.Clear();
        if (gridData.Count == 0) return;

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var cellWidth = CellSize + CellSpacing;

        // 找出每个月第一次出现的周
        var monthFirstWeek = new Dictionary<int, int>();
        foreach (var cell in gridData.Where(c => c.Intensity >= 0).OrderBy(c => c.Week))
        {
            if (!monthFirstWeek.ContainsKey(cell.Month))
                monthFirstWeek[cell.Month] = cell.Week;
        }

        // 添加月份标签
        foreach (var kvp in monthFirstWeek.OrderBy(x => x.Key))
        {
            var label = new TextBlock
            {
                Text = months[kvp.Key - 1],
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 105, 110))
            };

            Canvas.SetLeft(label, kvp.Value * cellWidth);
            Canvas.SetTop(label, 0);
            MonthsPanel.Children.Add(label);
        }
    }

    /// <summary>
    /// 单元格数据
    /// </summary>
    private class CellData
    {
        public int Intensity { get; set; }
        public string? ToolTip { get; set; }
        public DateOnly Date { get; set; }
        public int Week { get; set; }
        public int DayOfWeek { get; set; }
        public int Month { get; set; }
    }
}
