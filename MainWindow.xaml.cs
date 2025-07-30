using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Serial_Port_Assistant.ViewModels;

namespace Serial_Port_Assistant;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // DataContext 将在 App.xaml.cs 中设置
        // 这里只需要处理一些特殊的 UI 事件（如果需要的话）
    }

    /// <summary>
    /// 窗口标题栏双击事件 - 最大化/还原窗口
    /// </summary>
    private void TitleBar_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.MaximizeWindowCommand?.Execute(null);
        }
    }

    /// <summary>
    /// 窗口标题栏拖拽移动
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.HandleWindowDrag();
            }
        }
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 如果 ViewModel 有特殊的关闭逻辑，在这里处理
        if (DataContext is MainViewModel viewModel)
        {
            // 检查是否需要确认关闭
            if (viewModel.SerialPortViewModel.IsConnected)
            {
                bool confirmClose = viewModel.ShowConfirm("串口当前处于连接状态，确定要关闭吗？", "确认关闭");
                if (!confirmClose)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        base.OnClosing(e);
    }
}