# 贡献日历 (ContribCalendar)

一个轻量级的 Windows 桌面小部件，以 GitHub 风格的贡献日历可视化您的本地文件夹活动。

![主界面](Screenshots/main.png)

## ✨ 特性

- 🎨 **GitHub 风格日历** - 熟悉的绿色方块贡献图，按月分组显示
- 📁 **本地文件夹追踪** - 自动扫描指定目录下的日期格式文件夹（如 `2026年2月27日`）
- 🖱️ **快速访问** - 点击任意日期直接打开对应的文件夹
- 🎯 **系统托盘** - 最小化到系统托盘，不占用任务栏空间
- 🔍 **实时监控** - 自动检测文件夹变化并更新显示
- ⚙️ **灵活配置** - 自定义扫描路径、忽略规则、显示大小（50%-200%）
- 🪟 **毛玻璃效果** - 现代化的半透明窗口设计
- 🚫 **禁止自动缩放** - 窗口贴边不会触发 Windows Snap

## 📸 截图

### 主界面
![主界面展示](Screenshots/main.png)

### 设置窗口
![设置界面](Screenshots/settings.png)

### 文件夹选择
![多文件夹选择](Screenshots/folder-picker.png)

### 系统托盘
![系统托盘菜单](Screenshots/tray.png)

## 🚀 快速开始

### 系统要求

- Windows 10/11
- .NET 8.0 Runtime

### 安装

1. 从 [Releases](../../releases) 下载最新版本
2. 解压到任意目录
3. 运行 `ContribCalendar.exe`

### 首次使用

1. 首次启动会自动打开设置窗口
2. 点击"浏览..."选择包含日期文件夹的根目录
3. 点击"立即扫描"开始扫描
4. 点击"保存"完成配置

## 📂 文件夹命名规则

程序会自动识别以下格式的文件夹：

- `2026年2月27日`
- `2026年2月27日_项目名称`
- `2025年1月5日_tools`

支持任意后缀，只要以 `年月日` 格式开头即可。

## ⚙️ 配置说明

### 显示大小
- 滑块调节：50% - 200%
- 实时预览，保存后立即生效

### 扫描路径
- 选择包含日期文件夹的根目录
- 支持递归扫描子目录

### 忽略文件夹
- 支持精确匹配：`node_modules`
- 支持通配符：`.*`、`temp*`
- 默认忽略常见开发目录（`.git`、`bin`、`obj` 等）

## 🎯 使用技巧

### 系统托盘操作
- **双击图标** - 显示/隐藏主窗口
- **右键菜单** - 刷新数据、打开设置、退出程序

### 日历交互
- **鼠标悬停** - 显示日期和贡献数量
- **点击格子** - 打开对应日期的文件夹
- **多个文件夹** - 弹出选择窗口，显示文件夹图标

### 颜色说明
- 🟦 **浅灰色** - 无贡献（0 个文件夹）
- 🟩 **浅绿色** - 1-2 个文件夹
- 🟩 **中绿色** - 3-5 个文件夹
- 🟩 **深绿色** - 6-9 个文件夹
- 🟩 **最深绿** - 10+ 个文件夹

## 🛠️ 技术栈

- **框架**: .NET 8.0 + WPF
- **数据库**: SQLite (Dapper)
- **系统托盘**: H.NotifyIcon.Wpf
- **UI 设计**: 毛玻璃效果 + 自定义控件

## 📊 数据存储

所有数据存储在本地 SQLite 数据库：
```
%LocalAppData%\ContribCalendar\data.db
```

包含：
- 文件夹扫描记录
- 用户设置
- 忽略规则

## 🔧 开发

### 构建项目

```bash
git clone https://github.com/yourusername/ContribCalendar.git
cd ContribCalendar
dotnet restore
dotnet build
```

### 运行

```bash
dotnet run --project ContribCalendar
```

### 项目结构

```
ContribCalendar/
├── Controls/           # 自定义控件
│   ├── ContributionGrid.xaml
│   └── LoadingOverlay.xaml
├── Converters/         # 值转换器
│   └── IntensityColorConverter.cs
├── Models/             # 数据模型
│   ├── AppSettings.cs
│   ├── ContribData.cs
│   └── FolderRecord.cs
├── Services/           # 业务逻辑
│   ├── DatabaseService.cs
│   ├── FileScannerService.cs
│   └── FileWatcherService.cs
├── Utils/              # 工具类
│   └── DirectoryNameParser.cs
├── ViewModels/         # 视图模型
│   └── MainViewModel.cs
├── Views/              # 窗口视图
│   ├── FolderPickerWindow.xaml
│   └── SettingsWindow.xaml
└── MainWindow.xaml     # 主窗口
```

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 贡献指南

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📝 更新日志

### v1.0.0 (2026-02-27)

- ✨ 初始版本发布
- 🎨 GitHub 风格贡献日历
- 📁 本地文件夹扫描与追踪
- 🖱️ 点击打开文件夹功能
- ⚙️ 完整的设置界面
- 🎯 系统托盘支持
- 🔍 实时文件监控

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- 灵感来源于 GitHub 的贡献日历
- 图标设计参考了 Windows 11 设计语言

## 📮 联系方式

- 提交 Issue: [GitHub Issues](../../issues)
- 邮箱: your.email@example.com

---

⭐ 如果这个项目对你有帮助，请给个 Star！
