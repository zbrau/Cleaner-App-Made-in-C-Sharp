using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace AppCleanerPro.Services
{
    /// <summary>
    /// Windows balloon tip notifications via WPF MessageBox fallback + 
    /// optional Shell_NotifyIcon integration.
    /// Uses a simple non-intrusive approach that doesn't require System.Windows.Forms.
    /// </summary>
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        // Track last notification time to avoid spam
        private DateTime _lastNotification = DateTime.MinValue;
        private const int CooldownSeconds = 30;

        /// <summary>Queue a subtle system health alert. Skips if recently shown.</summary>
        public void Notify(string title, string message)
        {
            // Use a background thread so it never blocks the UI
            System.Threading.Tasks.Task.Run(() =>
            {
                // Cooldown: don't spam the user
                if ((DateTime.Now - _lastNotification).TotalSeconds < CooldownSeconds)
                    return;
                _lastNotification = DateTime.Now;

                // Show as WPF info dialog on the UI thread
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    ShowNotificationPopup(title, message, isWarning: false);
                });
            });
        }

        /// <summary>Queue a warning alert.</summary>
        public void Warn(string title, string message)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                if ((DateTime.Now - _lastNotification).TotalSeconds < CooldownSeconds)
                    return;
                _lastNotification = DateTime.Now;

                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    ShowNotificationPopup(title, message, isWarning: true);
                });
            });
        }

        /// <summary>Check disk space and notify if critically low.</summary>
        public void CheckAndNotify()
        {
            try
            {
                var di = new System.IO.DriveInfo("C");
                double freePct = (double)di.AvailableFreeSpace / di.TotalSize * 100;
                double freeGb = di.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;

                if (freePct < 5)
                    Warn("⚠️ Disco Crítico", $"Solo quedan {freeGb:0.0} GB libres. Tu PC puede volverse muy lenta.");
                else if (freePct < 10)
                    Notify("💾 Disco casi lleno", $"Tienes {freeGb:0.0} GB libres ({freePct:0}%). Considera limpiar tu sistema.");
            }
            catch { }
        }

        private static void ShowNotificationPopup(string title, string message, bool isWarning)
        {
            // Create a subtle non-blocking toast-style window
            var popup = new Window
            {
                Title = "",
                Width = 360,
                Height = 90,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
            };

            // Position bottom-right
            popup.Left = SystemParameters.WorkArea.Right - 376;
            popup.Top  = SystemParameters.WorkArea.Bottom - 106;

            var border = new System.Windows.Controls.Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x11, 0x16, 0x27)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
            };
            border.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    isWarning
                        ? System.Windows.Media.Color.FromRgb(0xFB, 0x92, 0x3C)
                        : System.Windows.Media.Color.FromRgb(0x7B, 0x9F, 0xFF));
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black, BlurRadius = 20, ShadowDepth = 4, Opacity = 0.5
            };

            var panel = new System.Windows.Controls.StackPanel();
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = title,
                Foreground = System.Windows.Media.Brushes.White,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 13, FontWeight = FontWeights.SemiBold,
            });
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = message,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x5A, 0x62, 0x80)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 11, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
            });
            border.Child = panel;
            popup.Content = border;

            // Auto-close after 5 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (_, _) => { timer.Stop(); popup.Close(); };

            popup.MouseLeftButtonDown += (_, _) => popup.Close();
            popup.Show();
            timer.Start();

            // Fade in animation
            popup.Opacity = 0;
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
                TimeSpan.FromMilliseconds(300));
            popup.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}
