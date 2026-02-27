using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ContribCalendar.Views;

public partial class FolderPickerWindow : Window
{
    public FolderPickerWindow(DateOnly date, List<string> folders)
    {
        InitializeComponent();
        DateLabel.Text = $"{date:MMM d, yyyy}  —  {folders.Count} 个文件夹";
        FolderList.ItemsSource = folders
            .Select(path => new FolderItem(System.IO.Path.GetFileName(path), path))
            .ToList();
    }

    private void FolderButton_Click(object sender, RoutedEventArgs e)
    {
        // 从 DataContext 取，因为 ControlTemplate 里 Tag binding 不可靠
        var btn = sender as FrameworkElement;
        var item = btn?.DataContext as FolderItem;
        if (item == null) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = item.Path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开文件夹: {ex.Message}", "错误");
        }
        Close();
    }

    public record FolderItem(string Name, string Path);
}
