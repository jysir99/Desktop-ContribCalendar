using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ContribCalendar.Converters;

/// <summary>
/// 强度到颜色的转换器
/// </summary>
public class IntensityColorConverter : IValueConverter
{
    // 更高对比度的颜色 - 适配毛玻璃背景
    private static readonly SolidColorBrush[] Colors =
    [
        new SolidColorBrush(Color.FromRgb(235, 237, 240)), // 0 - 白灰色，无贡献
        new SolidColorBrush(Color.FromRgb(155, 233, 168)), // 1 - 浅绿
        new SolidColorBrush(Color.FromRgb(64, 196, 99)),   // 2 - 中绿
        new SolidColorBrush(Color.FromRgb(48, 161, 78)),   // 3 - 深绿
        new SolidColorBrush(Color.FromRgb(33, 110, 57))    // 4 - 最深绿
    ];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intensity)
        {
            // -1 表示无数据，返回透明
            if (intensity < 0)
            {
                return Brushes.Transparent;
            }
            if (intensity >= 0 && intensity < Colors.Length)
            {
                return Colors[intensity];
            }
        }
        return Colors[0];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
