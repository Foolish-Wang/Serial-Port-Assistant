using System.Windows;
using System.Windows.Controls;

namespace Serial_Port_Assistant.Behaviors
{
    public static class AutoScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false, AutoScrollPropertyChanged));

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox && (bool)e.NewValue)
            {
                textBox.TextChanged += TextBox_TextChanged;
            }
            else if (d is TextBox textBoxOld)
            {
                textBoxOld.TextChanged -= TextBox_TextChanged;
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}