using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Serial_Port_Assistant.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        #region 私有字段
        private string _title = "串口助手";
        private string _version = "v1.0.0";
        private bool _isWindowMaximized = false;
        private WindowState _windowState = WindowState.Normal;
        private double _windowWidth = 900;
        private double _windowHeight = 600;
        private bool _disposed = false;
        #endregion

        #region 构造函数
        public MainViewModel()
        {
            // 初始化串口 ViewModel
            SerialPortViewModel = new SerialPortViewModel();
            
            // 初始化命令
            InitializeCommands();
            
            // 订阅串口连接状态变化事件，更新标题
            SerialPortViewModel.PropertyChanged += OnSerialPortViewModelPropertyChanged;
            
            // 更新初始标题
            UpdateTitle();
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 串口 ViewModel
        /// </summary>
        public SerialPortViewModel SerialPortViewModel { get; }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 版本信息
        /// </summary>
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 窗口是否最大化
        /// </summary>
        public bool IsWindowMaximized
        {
            get => _isWindowMaximized;
            set
            {
                _isWindowMaximized = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 窗口状态
        /// </summary>
        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                _windowState = value;
                IsWindowMaximized = value == WindowState.Maximized;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 窗口宽度
        /// </summary>
        public double WindowWidth
        {
            get => _windowWidth;
            set
            {
                _windowWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 窗口高度
        /// </summary>
        public double WindowHeight
        {
            get => _windowHeight;
            set
            {
                _windowHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前连接状态描述
        /// </summary>
        public string ConnectionStatus => SerialPortViewModel.IsConnected 
            ? $"已连接 - {SerialPortViewModel.Config.PortName}" 
            : "未连接";

        /// <summary>
        /// 应用程序完整标题（包含连接状态）
        /// </summary>
        public string FullTitle => SerialPortViewModel.IsConnected 
            ? $"{Title} - {SerialPortViewModel.Config.PortName} ({SerialPortViewModel.Config.BaudRate})" 
            : Title;
        #endregion

        #region 命令
        public ICommand MinimizeWindowCommand { get; private set; }
        public ICommand MaximizeWindowCommand { get; private set; }
        public ICommand CloseWindowCommand { get; private set; }
        public ICommand ShowAboutCommand { get; private set; }
        public ICommand ExitApplicationCommand { get; private set; }
        public ICommand WindowLoadedCommand { get; private set; }
        public ICommand WindowClosingCommand { get; private set; }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            MinimizeWindowCommand = new RelayCommand(MinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(MaximizeWindow);
            CloseWindowCommand = new RelayCommand(CloseWindow);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ExitApplicationCommand = new RelayCommand(ExitApplication);
            WindowLoadedCommand = new RelayCommand(OnWindowLoaded);
            WindowClosingCommand = new RelayCommand(OnWindowClosing);
        }

        /// <summary>
        /// 最小化窗口
        /// </summary>
        private void MinimizeWindow()
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 最大化/还原窗口
        /// </summary>
        private void MaximizeWindow()
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            Application.Current.MainWindow?.Close();
        }

        /// <summary>
        /// 显示关于对话框
        /// </summary>
        private void ShowAbout()
        {
            string aboutMessage = $"串口助手 {Version}\n\n" +
                                 "功能特性：\n" +
                                 "• 支持多种串口参数配置\n" +
                                 "• 文本和十六进制收发模式\n" +
                                 "• 实时数据统计\n" +
                                 "• 友好的用户界面\n\n" +
                                 "技术栈：\n" +
                                 "• WPF + MVVM 架构\n" +
                                 "• .NET 8.0\n" +
                                 "• System.IO.Ports\n\n" +
                                 "© 2025 Serial Port Assistant";

            MessageBox.Show(aboutMessage, "关于串口助手", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            // 如果串口已连接，询问是否确认退出
            if (SerialPortViewModel.IsConnected)
            {
                var result = MessageBox.Show(
                    "串口当前处于连接状态，确定要退出应用程序吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            Application.Current.Shutdown();
        }

        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void OnWindowLoaded()
        {
            // 窗口加载完成后的初始化工作
            try
            {
                // 刷新串口列表
                SerialPortViewModel.RefreshPortsCommand.Execute(null);
                
                // 更新状态
                SerialPortViewModel.StatusMessage = "应用程序启动完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序初始化时发生错误：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void OnWindowClosing()
        {
            try
            {
                // 如果串口已连接，先断开连接
                if (SerialPortViewModel.IsConnected)
                {
                    SerialPortViewModel.DisconnectCommand.Execute(null);
                }

                // 保存窗口状态等设置
                SaveWindowSettings();
            }
            catch (Exception ex)
            {
                // 记录错误但不阻止关闭
                System.Diagnostics.Debug.WriteLine($"窗口关闭时发生错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 保存窗口设置
        /// </summary>
        private void SaveWindowSettings()
        {
            try
            {
                // 这里可以实现设置保存逻辑
                // 例如保存到配置文件或注册表
                // Properties.Settings.Default.WindowWidth = WindowWidth;
                // Properties.Settings.Default.WindowHeight = WindowHeight;
                // Properties.Settings.Default.WindowState = WindowState;
                // Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存窗口设置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 加载窗口设置
        /// </summary>
        private void LoadWindowSettings()
        {
            try
            {
                // 这里可以实现设置加载逻辑
                // WindowWidth = Properties.Settings.Default.WindowWidth;
                // WindowHeight = Properties.Settings.Default.WindowHeight;
                // WindowState = Properties.Settings.Default.WindowState;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载窗口设置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新窗口标题
        /// </summary>
        private void UpdateTitle()
        {
            OnPropertyChanged(nameof(ConnectionStatus));
            OnPropertyChanged(nameof(FullTitle));
        }

        /// <summary>
        /// 处理串口 ViewModel 属性变化
        /// </summary>
        private void OnSerialPortViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当串口连接状态或配置发生变化时，更新主窗口标题
            if (e.PropertyName == nameof(SerialPortViewModel.IsConnected) ||
                e.PropertyName == nameof(SerialPortViewModel.Config))
            {
                UpdateTitle();
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 处理窗口拖拽移动
        /// </summary>
        public void HandleWindowDrag()
        {
            try
            {
                Application.Current.MainWindow?.DragMove();
            }
            catch (InvalidOperationException)
            {
                // 忽略拖拽时可能发生的异常
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="title">标题</param>
        public void ShowError(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">信息消息</param>
        /// <param name="title">标题</param>
        public void ShowInfo(string message, string title = "信息")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        public bool ShowConfirm(string message, string title = "确认")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
        #endregion

        #region INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable 实现
        public void Dispose()
        {
            if (!_disposed)
            {
                // 取消事件订阅
                if (SerialPortViewModel != null)
                {
                    SerialPortViewModel.PropertyChanged -= OnSerialPortViewModelPropertyChanged;
                    SerialPortViewModel.Dispose();
                }

                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~MainViewModel()
        {
            Dispose();
        }
        #endregion
    }
}