using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Serial_Port_Assistant.Utils
{
    /// <summary>
    /// 十六进制转换工具类
    /// </summary>
    public static class HexConverter
    {
        /// <summary>
        /// 将字符串转换为十六进制字符串
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="encoding">编码方式，默认为UTF8</param>
        /// <param name="separator">分隔符，默认为空格</param>
        /// <returns>十六进制字符串</returns>
        public static string StringToHex(string input, Encoding? encoding = null, string separator = " ")
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            encoding ??= Encoding.UTF8;
            
            try
            {
                byte[] bytes = encoding.GetBytes(input);
                return BytesToHex(bytes, separator);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"字符串转换为十六进制失败: {ex.Message}", nameof(input));
            }
        }

        /// <summary>
        /// 将十六进制字符串转换为普通字符串
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <param name="encoding">编码方式，默认为UTF8</param>
        /// <returns>转换后的字符串</returns>
        public static string HexToString(string hexString, Encoding? encoding = null)
        {
            if (string.IsNullOrEmpty(hexString))
                return string.Empty;

            encoding ??= Encoding.UTF8;

            try
            {
                byte[] bytes = HexToBytes(hexString);
                return encoding.GetString(bytes);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"十六进制转换为字符串失败: {ex.Message}", nameof(hexString));
            }
        }

        /// <summary>
        /// 将字节数组转换为十六进制字符串
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="separator">分隔符，默认为空格</param>
        /// <param name="upperCase">是否使用大写，默认为true</param>
        /// <returns>十六进制字符串</returns>
        public static string BytesToHex(byte[] bytes, string separator = " ", bool upperCase = true)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            string format = upperCase ? "X2" : "x2";
            
            if (string.IsNullOrEmpty(separator))
            {
                return string.Concat(bytes.Select(b => b.ToString(format)));
            }
            else
            {
                return string.Join(separator, bytes.Select(b => b.ToString(format)));
            }
        }

        /// <summary>
        /// 将十六进制字符串转换为字节数组
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexToBytes(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return Array.Empty<byte>();

            // 清理输入字符串：移除空格、制表符、换行符、分隔符等
            hexString = CleanHexString(hexString);

            // 验证十六进制字符串格式
            if (!IsValidHexString(hexString))
            {
                throw new ArgumentException("无效的十六进制字符串格式", nameof(hexString));
            }

            // 确保字符串长度为偶数
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("十六进制字符串长度必须为偶数", nameof(hexString));
            }

            try
            {
                byte[] bytes = new byte[hexString.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }
                return bytes;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"十六进制字符串解析失败: {ex.Message}", nameof(hexString));
            }
        }

        /// <summary>
        /// 格式化十六进制字符串显示
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <param name="separator">分隔符</param>
        /// <param name="bytesPerLine">每行显示的字节数，0表示不换行</param>
        /// <param name="upperCase">是否使用大写</param>
        /// <returns>格式化后的十六进制字符串</returns>
        public static string FormatHexString(string hexString, string separator = " ", int bytesPerLine = 16, bool upperCase = true)
        {
            if (string.IsNullOrEmpty(hexString))
                return string.Empty;

            try
            {
                byte[] bytes = HexToBytes(hexString);
                return FormatBytes(bytes, separator, bytesPerLine, upperCase);
            }
            catch
            {
                return hexString; // 如果解析失败，返回原字符串
            }
        }

        /// <summary>
        /// 格式化字节数组显示
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="separator">分隔符</param>
        /// <param name="bytesPerLine">每行显示的字节数，0表示不换行</param>
        /// <param name="upperCase">是否使用大写</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatBytes(byte[] bytes, string separator = " ", int bytesPerLine = 16, bool upperCase = true)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            string format = upperCase ? "X2" : "x2";
            var result = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0)
                {
                    if (bytesPerLine > 0 && i % bytesPerLine == 0)
                    {
                        result.AppendLine();
                    }
                    else if (!string.IsNullOrEmpty(separator))
                    {
                        result.Append(separator);
                    }
                }
                result.Append(bytes[i].ToString(format));
            }

            return result.ToString();
        }

        /// <summary>
        /// 验证字符串是否为有效的十六进制格式
        /// </summary>
        /// <param name="hexString">待验证的字符串</param>
        /// <returns>是否为有效的十六进制字符串</returns>
        public static bool IsValidHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return false;

            // 使用正则表达式验证是否只包含十六进制字符
            return Regex.IsMatch(hexString, @"^[0-9A-Fa-f]+$");
        }

        /// <summary>
        /// 清理十六进制字符串，移除非十六进制字符
        /// </summary>
        /// <param name="hexString">原始十六进制字符串</param>
        /// <returns>清理后的十六进制字符串</returns>
        private static string CleanHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return string.Empty;

            // 移除所有非十六进制字符（保留0-9, A-F, a-f）
            return Regex.Replace(hexString, @"[^0-9A-Fa-f]", "");
        }

        /// <summary>
        /// 计算十六进制字符串表示的字节数
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <returns>字节数</returns>
        public static int GetByteCount(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return 0;

            string cleaned = CleanHexString(hexString);
            return (cleaned.Length + 1) / 2; // 向上取整
        }

        /// <summary>
        /// 将单个字节转换为十六进制字符串
        /// </summary>
        /// <param name="value">字节值</param>
        /// <param name="upperCase">是否使用大写</param>
        /// <returns>十六进制字符串</returns>
        public static string ByteToHex(byte value, bool upperCase = true)
        {
            return value.ToString(upperCase ? "X2" : "x2");
        }

        /// <summary>
        /// 检查十六进制字符串是否可以安全转换
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>是否可以安全转换</returns>
        public static bool TryValidateHex(string hexString, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(hexString))
            {
                errorMessage = "十六进制字符串不能为空";
                return false;
            }

            string cleaned = CleanHexString(hexString);
            
            if (cleaned.Length == 0)
            {
                errorMessage = "没有找到有效的十六进制字符";
                return false;
            }

            if (cleaned.Length % 2 != 0)
            {
                errorMessage = "十六进制字符串长度必须为偶数";
                return false;
            }

            return true;
        }
    }
}