using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using Serial_Port_Assistant.Models;
using Serial_Port_Assistant.Utils;

namespace Serial_Port_Assistant.Services
{
    public class SerialPortService : IDisposable
    {
        private SerialPort? _serialPort;
        private bool _disposed = false;

        /// <summary>
        /// 串口数据接收事件 (修改：确保泛型参数是 byte[])
        /// </summary>
        public event EventHandler<byte[]>? DataReceived;

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
        /// 数据编码格式，默认为 UTF-8
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

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
            if (IsConnected)
            {
                await ClosePortAsync();
            }

            try
            {
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

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.ErrorReceived += OnErrorReceived;

                await Task.Run(() => _serialPort.Open());

                ConnectionStatusChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"打开串口 {config.PortName} 失败: {ex.Message}");
                _serialPort?.Dispose();
                _serialPort = null;
                ConnectionStatusChanged?.Invoke(this, false);
                return false;
            }
        }

        /// <summary>
        /// 关闭串口连接
        /// </summary>
        public async Task ClosePortAsync()
        {
            if (!IsConnected || _serialPort == null)
            {
                return;
            }

            try
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.ErrorReceived -= OnErrorReceived;

                await Task.Run(() =>
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                });
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"关闭串口失败: {ex.Message}");
            }
            finally
            {
                _serialPort = null;
                ConnectionStatusChanged?.Invoke(this, false);
            }
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
                byte[] bytes = Encoding.GetBytes(data);
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
                // 使用 HexConverter 工具类进行转换，更健壮
                byte[] bytes = HexConverter.HexToBytes(hexString);
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
                throw new InvalidOperationException("串口未打开或已关闭。");

            // 使用 BaseStream 的异步方法，这是推荐的做法
            await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
        }

        /// <summary>
        /// 串口数据接收事件处理
        /// </summary>
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);
                    
                    // 确认这里调用的是传递 byte[] 的事件
                    DataReceived?.Invoke(this, buffer);
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
            ErrorOccurred?.Invoke(this, $"串口硬件错误: {e.EventType}");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    if (_serialPort != null)
                    {
                        ClosePortAsync().Wait(); // 同步等待关闭完成
                        _serialPort = null;
                    }
                }
                // 释放非托管资源（如果有）
                _disposed = true;
            }
        }

        ~SerialPortService()
        {
            Dispose(false);
        }
    }
}