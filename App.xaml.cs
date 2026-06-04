using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using LingoWay.Views;

namespace LingoWay
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App()
        {
            InitializeComponent();

            // 加载深色主题作为默认
            LoadTheme("Dark");
        }

        public void LoadTheme(string theme)
        {
            var dict = Microsoft.Maui.Controls.Application.Current?.Resources?.MergedDictionaries;
            if (dict == null) return;

            // 移除已有的主题颜色字典
            var toRemove = dict.Where(d => d.Source?.OriginalString?.Contains("DarkColors.xaml") == true ||
                                            d.Source?.OriginalString?.Contains("LightColors.xaml") == true).ToList();
            foreach (var d in toRemove)
                dict.Remove(d);

            // 根据主题创建新的资源字典并添加
            var themeUri = theme == "Dark"
                ? "Resources/Styles/DarkColors.xaml"
                : "Resources/Styles/LightColors.xaml";

            try
            {
                // 通过加载 XAML 文件来创建资源字典
                var assembly = typeof(App).Assembly;
                var resourceName = $"{assembly.GetName().Name}.{themeUri.Replace('/', '.')}";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var xaml = new StreamReader(stream).ReadToEnd();
                        var themeDict = new ResourceDictionary();
                        themeDict.LoadFromXaml(xaml);
                        dict.Add(themeDict);
                    }
                }
            }
            catch
            {
                // 如果加载失败，使用备用方案：直接在XAML中维护主题
                // 当前实现中，DarkColors.xaml 已在 App.xaml 中定义，
                // Light 主题需要单独处理
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}