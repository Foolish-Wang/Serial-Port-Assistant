using System;
using System.Windows;
using Serial_Port_Assistant.ViewModels;

namespace Serial_Port_Assistant
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 设置全局异常处理
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 创建主窗口和 ViewModel
            var mainViewModel = new MainViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // 设置主窗口
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 应用程序退出时的清理工作
            try
            {
                if (MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用程序退出时发生错误: {ex.Message}");
            }

            base.OnExit(e);
        }

        /// <summary>
        /// UI 线程未处理异常
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBox.Show(
                    $"应用程序发生未处理的错误：\n\n{e.Exception.Message}\n\n详细信息：\n{e.Exception}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                e.Handled = true; // 标记异常已处理，防止应用程序崩溃
            }
            catch
            {
                // 如果连错误处理也失败了，就让应用程序正常崩溃
            }
        }

        /// <summary>
        /// 非 UI 线程未处理异常
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine($"非UI线程未处理异常: {exception}");
                    
                    // 尝试在 UI 线程显示错误
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"应用程序发生严重错误：\n\n{exception.Message}",
                            "严重错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                }
            }
            catch
            {
                // 最后的异常处理失败，无法做更多处理
            }
        }
    }
}