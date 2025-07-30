using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Serial_Port_Assistant.Converters
{
    /// <summary>
    /// 将布尔值转换为 Visibility 枚举值，支持反向转换和逻辑反转。
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为 Visibility。
        /// </summary>
        /// <param name="value">布尔值。</param>
        /// <param name="targetType">目标类型 (Visibility)。</param>
        /// <param name="parameter">如果为 "True" 或 "Invert"，则反转逻辑。</param>
        /// <param name="culture">区域性信息。</param>
        /// <returns>Visible 或 Collapsed。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
            {
                return Visibility.Collapsed;
            }

            bool invert = false;
            if (parameter is string paramString)
            {
                invert = paramString.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                         paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            }

            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 将 Visibility 转换回布尔值。
        /// </summary>
        /// <param name="value">Visibility 值。</param>
        /// <param name="targetType">目标类型 (bool)。</param>
        /// <param name="parameter">如果为 "True" 或 "Invert"，则反转逻辑。</param>
        /// <param name="culture">区域性信息。</param>
        /// <returns>true 或 false。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility visibilityValue)
            {
                return false;
            }

            bool result = visibilityValue == Visibility.Visible;

            bool invert = false;
            if (parameter is string paramString)
            {
                invert = paramString.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                         paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            }

            return result ^ invert;
        }
    }
}