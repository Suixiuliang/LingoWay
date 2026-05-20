using Microsoft.Extensions.DependencyInjection;
using LingoWay.Views;

namespace LingoWay
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App()
        {
            InitializeComponent();

            // 设置深色模式
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 使用AppShell管理导航
            return new Window(new AppShell());
        }
    }
}