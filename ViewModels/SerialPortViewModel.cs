using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Serial_Port_Assistant.Models;
using Serial_Port_Assistant.Services;
using Serial_Port_Assistant.Utils;

namespace Serial_Port_Assistant.ViewModels
{
    public class SerialPortViewModel : INotifyPropertyChanged
    {
        #region 私有字段
        private readonly SerialPortService _serialPortService;
        private readonly Action<string, string> _showErrorAction;
        private SerialPortConfig _config;
        
        // 修改：存储数据块列表，而不是连续的字节列表
        private readonly List<byte[]> _receivedChunks = new List<byte[]>();
        
        private string _sendData = string.Empty;
        private bool _isConnected = false;
        private bool _isHexMode = false;
        private bool _isReceiveHexMode = false;
        private string _statusMessage = "就绪";
        private int _sentCount = 0;
        #endregion

        #region 构造函数
        public SerialPortViewModel(Action<string, string> showErrorAction)
        {
            // 修复：保存传入的弹窗委托
            _showErrorAction = showErrorAction; 
            
            _serialPortService = new SerialPortService();
            _config = new SerialPortConfig();
            
            RefreshPortList();
            
            _serialPortService.DataReceived += OnDataReceived;
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _serialPortService.ErrorOccurred += OnErrorOccurred;
            
            ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => !string.IsNullOrEmpty(Config.PortName));
            DisconnectCommand = new RelayCommand(async () => await DisconnectAsync(), () => IsConnected);
            
            // 修改：移除 IsConnected 的检查，让发送按钮始终由 SendData 是否为空决定
            SendCommand = new RelayCommand(async () => await SendDataAsync(), () => !string.IsNullOrEmpty(SendData));
            
            ClearReceiveCommand = new RelayCommand(ClearReceiveData);
            ClearSendCommand = new RelayCommand(ClearSendData);
            RefreshPortsCommand = new RelayCommand(RefreshPortList);
            ResetCountersCommand = new RelayCommand(ResetCounters);
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 串口配置
        /// </summary>
        public SerialPortConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可用串口列表
        /// </summary>
        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 可用波特率列表
        /// </summary>
        public ObservableCollection<int> AvailableBaudRates { get; } = new ObservableCollection<int>
        {
            1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600
        };

        /// <summary>
        /// 可用数据位列表
        /// </summary>
        public ObservableCollection<int> AvailableDataBits { get; } = new ObservableCollection<int>
        {
            5, 6, 7, 8
        };

        /// <summary>
        /// 可用校验位列表
        /// </summary>
        public ObservableCollection<Parity> AvailableParities { get; } = new ObservableCollection<Parity>
        {
            Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space
        };

        /// <summary>
        /// 可用停止位列表
        /// </summary>
        public ObservableCollection<StopBits> AvailableStopBits { get; } = new ObservableCollection<StopBits>
        {
            StopBits.One, StopBits.OnePointFive, StopBits.Two
        };

        /// <summary>
        /// 接收到的数据 (显示用)
        /// </summary>
        public string ReceivedData
        {
            // 修改：动态生成显示内容
            get
            {
                if (_receivedChunks.Count == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                // 修改：遍历每个数据块，并用换行符连接
                foreach (var chunk in _receivedChunks)
                {
                    string line = IsReceiveHexMode
                        ? HexConverter.BytesToHex(chunk, " ")
                        : Encoding.UTF8.GetString(chunk);
                    sb.AppendLine(line);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 要发送的数据
        /// </summary>
        public string SendData
        {
            get => _sendData;
            set
            {
                _sendData = value;
                OnPropertyChanged();
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                _isConnected = value;
                OnPropertyChanged();
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 发送模式是否为十六进制
        /// </summary>
        public bool IsHexMode
        {
            get => _isHexMode;
            set
            {
                _isHexMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 接收模式是否为十六进制显示
        /// </summary>
        public bool IsReceiveHexMode
        {
            get => _isReceiveHexMode;
            set
            {
                if (_isReceiveHexMode != value)
                {
                    _isReceiveHexMode = value;
                    OnPropertyChanged();
                    // 修改：只需通知UI更新ReceivedData属性即可
                    OnPropertyChanged(nameof(ReceivedData));
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 接收字节计数
        /// </summary>
        public int ReceivedCount => _receivedChunks.Sum(chunk => chunk.Length); // 修改：直接返回列表计数

        /// <summary>
        /// 发送字节计数
        /// </summary>
        public int SentCount
        {
            get => _sentCount;
            private set
            {
                _sentCount = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 命令
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand ClearReceiveCommand { get; }
        public ICommand ClearSendCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand ResetCountersCommand { get; }
        #endregion

        #region 私有方法
        /// <summary>
        /// 连接串口
        /// </summary>
        private async Task ConnectAsync()
        {
            try
            {
                StatusMessage = "正在连接...";
                bool success = await _serialPortService.OpenPortAsync(Config);
                
                if (success)
                {
                    StatusMessage = $"已连接到 {Config.PortName}";
                }
                else
                {
                    StatusMessage = "连接失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"连接错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 断开串口连接
        /// </summary>
        private async Task DisconnectAsync()
        {
            try
            {
                StatusMessage = "正在断开连接...";
                await _serialPortService.ClosePortAsync();
                StatusMessage = "已断开连接";
            }
            catch (Exception ex)
            {
                StatusMessage = $"断开连接错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        private async Task SendDataAsync()
        {
            // 新增：在方法开头检查连接状态
            if (!IsConnected)
            {
                _showErrorAction?.Invoke("串口未连接，无法发送数据。", "操作失败");
                return;
            }

            if (string.IsNullOrEmpty(SendData)) return;
            try
            {
                bool success;
                byte[] dataToSend;
                if (IsHexMode)
                {
                    dataToSend = HexConverter.HexToBytes(SendData);
                    success = await _serialPortService.SendHexAsync(SendData);
                }
                else
                {
                    dataToSend = Encoding.UTF8.GetBytes(SendData);
                    success = await _serialPortService.SendTextAsync(SendData);
                }

                if (success)
                {
                    SentCount += dataToSend.Length;
                    StatusMessage = $"发送成功: {dataToSend.Length} 字节";
                }
                // 这里的 else 分支现在可以移除了，因为失败会通过异常处理
            }
            catch (Exception ex)
            {
                string errorMsg = $"发送错误: {ex.Message}";
                StatusMessage = errorMsg;
                _showErrorAction?.Invoke(errorMsg, "发送失败");

                // 修复：发生 I/O 错误时，认为连接已失效，自动断开
                if (IsConnected)
                {
                    await DisconnectAsync();
                }
            }
        }

        /// <summary>
        /// 清空接收数据
        /// </summary>
        private void ClearReceiveData()
        {
            // 修改：清空数据块列表
            _receivedChunks.Clear();
            OnPropertyChanged(nameof(ReceivedData));
            OnPropertyChanged(nameof(ReceivedCount));
        }

        /// <summary>
        /// 清空发送数据框
        /// </summary>
        private void ClearSendData()
        {
            SendData = string.Empty;
        }

        /// <summary>
        /// 刷新串口列表
        /// </summary>
        private void RefreshPortList()
        {
            try
            {
                AvailablePorts.Clear();
                string[] ports = _serialPortService.GetAvailablePorts();
                
                foreach (string port in ports)
                {
                    AvailablePorts.Add(port);
                }

                // 如果当前选择的串口不在列表中，选择第一个可用的
                if (!AvailablePorts.Contains(Config.PortName) && AvailablePorts.Count > 0)
                {
                    Config.PortName = AvailablePorts[0];
                }

                StatusMessage = $"找到 {ports.Length} 个可用串口";
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新串口列表失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        private void ResetCounters()
        {
            // 修改：清空数据块列表来重置接收计数
            _receivedChunks.Clear();
            SentCount = 0;
            OnPropertyChanged(nameof(ReceivedData));
            OnPropertyChanged(nameof(ReceivedCount));
            StatusMessage = "计数器已重置";
        }

        #endregion

        #region 事件处理
        /// <summary>
        /// 数据接收事件处理
        /// </summary>
        private void OnDataReceived(object? sender, byte[] data)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 修改：将接收到的数据块作为一个整体添加到列表中
                    _receivedChunks.Add(data);

                    OnPropertyChanged(nameof(ReceivedData));
                    OnPropertyChanged(nameof(ReceivedCount));
                    StatusMessage = $"接收到 {data.Length} 字节";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"处理接收数据时出错: {ex.Message}";
            }
        }

        /// <summary>
        /// 连接状态变化事件处理
        /// </summary>
        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
            });
        }

        /// <summary>
        /// 错误事件处理
        /// </summary>
        private void OnErrorOccurred(object? sender, string error)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = error;
                // 修复：现在 _showErrorAction 不为 null，可以正常调用
                _showErrorAction?.Invoke(error, "串口错误");
            });
        }
        #endregion

        #region INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable 支持
        public void Dispose()
        {
            _serialPortService?.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// 简单的 RelayCommand 实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}