using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using Serial_Port_Assistant.Models;

namespace Serial_Port_Assistant.Services
{
    public class SerialPortService : IDisposable
    {
        private SerialPort? _serialPort;
        private bool _disposed = false;

        /// <summary>
        /// 串口数据接收事件
        /// </summary>
        public event EventHandler<string>? DataReceived;

        /// <summary>
        /// 串口状态变化事件
        /// </summary>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<string>? ErrorOccurred;

        /// <summary>
        /// 当前串口是否已连接
        /// </summary>
        public bool IsConnected => _serialPort?.IsOpen ?? false;

        /// <summary>
        /// 获取可用的串口列表
        /// </summary>
        /// <returns>串口名称数组</returns>
        public string[] GetAvailablePorts()
        {
            try
            {
                return SerialPort.GetPortNames();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"获取串口列表失败: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 打开串口连接
        /// </summary>
        /// <param name="config">串口配置</param>
        /// <returns>是否成功打开</returns>
        public async Task<bool> OpenPortAsync(SerialPortConfig config)
        {
            try
            {
                // 如果已经打开，先关闭
                if (_serialPort?.IsOpen == true)
                {
                    await ClosePortAsync();
                }

                // 创建新的串口实例
                _serialPort = new SerialPort
                {
                    PortName = config.PortName,
                    BaudRate = config.BaudRate,
                    DataBits = config.DataBits,
                    Parity = config.Parity,
                    StopBits = config.StopBits,
                    Handshake = Handshake.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                // 订阅数据接收事件
                _serialPort.DataReceived += OnDataReceived;
                _serialPort.ErrorReceived += OnErrorReceived;

                // 打开串口
                _serialPort.Open();

                // 触发连接状态变化事件
                ConnectionStatusChanged?.Invoke(this, true);

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"打开串口失败: {ex.Message}");
                _serialPort?.Close();
                _serialPort?.Dispose();
                _serialPort = null;
                return false;
            }
        }

        /// <summary>
        /// 关闭串口连接
        /// </summary>
        public async Task ClosePortAsync()
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    // 取消事件订阅
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.ErrorReceived -= OnErrorReceived;

                    // 关闭串口
                    _serialPort.Close();
                }

                _serialPort?.Dispose();
                _serialPort = null;

                // 触发连接状态变化事件
                ConnectionStatusChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"关闭串口失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 发送文本数据
        /// </summary>
        /// <param name="data">要发送的文本</param>
        /// <returns>是否发送成功</returns>
        public async Task<bool> SendTextAsync(string data)
        {
            if (string.IsNullOrEmpty(data) || !IsConnected)
                return false;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                await SendBytesAsync(bytes);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"发送文本数据失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送十六进制数据
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <returns>是否发送成功</returns>
        public async Task<bool> SendHexAsync(string hexString)
        {
            if (string.IsNullOrEmpty(hexString) || !IsConnected)
                return false;

            try
            {
                // 移除空格和非十六进制字符
                hexString = hexString.Replace(" ", "").Replace("-", "");
                
                if (hexString.Length % 2 != 0)
                {
                    ErrorOccurred?.Invoke(this, "十六进制字符串长度必须为偶数");
                    return false;
                }

                byte[] bytes = new byte[hexString.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                await SendBytesAsync(bytes);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"发送十六进制数据失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送字节数组
        /// </summary>
        /// <param name="data">字节数组</param>
        private async Task SendBytesAsync(byte[] data)
        {
            if (_serialPort?.IsOpen != true)
                throw new InvalidOperationException("串口未打开");

            await Task.Run(() => _serialPort.Write(data, 0, data.Length));
        }

        /// <summary>
        /// 串口数据接收事件处理
        /// </summary>
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort?.IsOpen == true && _serialPort.BytesToRead > 0)
                {
                    byte[] buffer = new byte[_serialPort.BytesToRead];
                    _serialPort.Read(buffer, 0, buffer.Length);
                    
                    // 转换为字符串并触发事件
                    string receivedData = Encoding.UTF8.GetString(buffer);
                    DataReceived?.Invoke(this, receivedData);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"接收数据时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 串口错误事件处理
        /// </summary>
        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorOccurred?.Invoke(this, $"串口错误: {e.EventType}");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                ClosePortAsync().Wait();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}