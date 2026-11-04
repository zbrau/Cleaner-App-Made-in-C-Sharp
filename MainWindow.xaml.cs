using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AppCleanerPro.Core;
using AppCleanerPro.Services;

namespace AppCleanerPro
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
        private bool _initialized;
        private double _currentIndicatorY = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Cargar y aplicar tema de color guardado
            try
            {
                var settings = new SystemEngine().LoadSettings();
                ThemeService.ApplyTheme(settings.AccentColor);
            }
            catch { }

            // Admin badge
            var isAdmin = CleanerEngine.IsAdmin();
            AdminText.Text = isAdmin ? "ADMIN" : "USUARIO";
            AdminText.Foreground = isAdmin
                ? (System.Windows.Media.Brush)FindResource("AccentGreen")
                : (System.Windows.Media.Brush)FindResource("AccentOrange");

            _clock.Tick += (_, _) => UpdateClock();
            _clock.Start();
            UpdateClock();

            _initialized = true;

            // Check disk health and notify if needed (non-blocking)
            System.Threading.Tasks.Task.Run(() =>
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                    Dispatcher.Invoke(() => NotificationService.Instance.CheckAndNotify())));
        }

        public void UpdateTheme(FullTheme theme)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var primary = (Color)ColorConverter.ConvertFromString(theme.AccentPrimary);
                    var secondary = (Color)ColorConverter.ConvertFromString(theme.AccentSecondary);
                    var cardHov = (Color)ColorConverter.ConvertFromString(theme.BgCardHov);
                    var border = (Color)ColorConverter.ConvertFromString(theme.BorderCard);
                    var bgDeep = (Color)ColorConverter.ConvertFromString(theme.BgDeep);
                    var bgSidebar = (Color)ColorConverter.ConvertFromString(theme.BgSidebar);

                    if (NavIndicatorBorder != null)
                        NavIndicatorBorder.Color = primary;
                    if (NavIndicatorGlow != null)
                        NavIndicatorGlow.Color = primary;
                    if (NavIndicatorBg != null)
                        NavIndicatorBg.Color = theme.IsLight ? Color.FromArgb(40, primary.R, primary.G, primary.B) : cardHov;

                    if (SidebarLogoGlow != null)
                        SidebarLogoGlow.Color = primary;
                    if (SidebarLogoGrad0 != null)
                        SidebarLogoGrad0.Color = primary;
                    if (SidebarLogoGrad1 != null)
                        SidebarLogoGrad1.Color = secondary;

                    if (RootBorder != null)
                        RootBorder.Background = new SolidColorBrush(bgDeep);
                    if (SidebarBorder != null)
                    {
                        SidebarBorder.Background = new SolidColorBrush(bgSidebar);
                        SidebarBorder.BorderBrush = new SolidColorBrush(border);
                    }
                }
                catch { }
            });
        }

        public void UpdateTheme(Color primary, Color secondary)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (NavIndicatorBorder != null)
                        NavIndicatorBorder.Color = primary;
                    if (NavIndicatorGlow != null)
                        NavIndicatorGlow.Color = primary;
                    if (SidebarLogoGlow != null)
                        SidebarLogoGlow.Color = primary;
                    if (SidebarLogoGrad0 != null)
                        SidebarLogoGrad0.Color = secondary;
                }
                catch { }
            });
        }

        private void AnimateNavIndicatorTo(FrameworkElement? targetBtn)
        {
            if (!_initialized || targetBtn == null || NavIndicatorTransform == null || NavContainer == null) return;

            try
            {
                var transform = targetBtn.TransformToVisual(NavContainer);
                var pt = transform.Transform(new Point(0, 0));
                double targetY = pt.Y;

                var anim = new DoubleAnimation
                {
                    From = _currentIndicatorY,
                    To = targetY,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                NavIndicatorTransform.BeginAnimation(TranslateTransform.YProperty, anim);
                _currentIndicatorY = targetY;
            }
            catch { }
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            ClockText.Text = now.ToString("HH:mm:ss");
            DateText.Text = now.ToString("ddd dd MMM", new System.Globalization.CultureInfo("es-MX"));
        }

        // ── Navigation ──────────────────────────────────────────────────────────

        private void ShowPage(FrameworkElement page)
        {
            PageDashboard.Visibility = Visibility.Collapsed;
            PageClean.Visibility     = Visibility.Collapsed;
            PageAnalyzer.Visibility   = Visibility.Collapsed;
            PageLargeFiles.Visibility = Visibility.Collapsed;
            PageDuplicates.Visibility = Visibility.Collapsed;
            PageTreemap.Visibility    = Visibility.Collapsed;
            PageShredder.Visibility   = Visibility.Collapsed;
            PagePrivacy.Visibility    = Visibility.Collapsed;
            PageProcess.Visibility    = Visibility.Collapsed;
            PageNetwork.Visibility    = Visibility.Collapsed;
            PageStartup.Visibility    = Visibility.Collapsed;
            PageUninstaller.Visibility = Visibility.Collapsed;
            PageRegistry.Visibility   = Visibility.Collapsed;
            PageFirewall.Visibility   = Visibility.Collapsed;
            PageSystemInfo.Visibility = Visibility.Collapsed;
            PageHistory.Visibility    = Visibility.Collapsed;
            PageSettings.Visibility   = Visibility.Collapsed;
            PageRamBooster.Visibility = Visibility.Collapsed;

            // Stop RAM monitor when leaving
            if (page != PageRamBooster)
                PageRamBooster.StopMonitor();

            page.Visibility = Visibility.Visible;

            // Transición suave moderna (Fade-in + deslizamiento ascendente sutil)
            var transform = new TranslateTransform(0, 8);
            page.RenderTransform = transform;

            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var slideUp = new DoubleAnimation(8.0, 0.0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            page.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
        }

        private void NavDashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageDashboard);
            HeaderTitle.Text = "Dashboard";
            HeaderSub.Text   = "Resumen de salud del sistema";
        }

        private void NavClean_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageClean);
            HeaderTitle.Text = "Limpieza del Sistema";
            HeaderSub.Text   = "Escanea y elimina archivos innecesarios";
        }

        private void NavAnalyze_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageAnalyzer);
            HeaderTitle.Text = "Analizador de Disco";
            HeaderSub.Text   = "Explora el uso de almacenamiento";
        }

        private void NavLargeFiles_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageLargeFiles);
            HeaderTitle.Text = "Archivos Gigantes";
            HeaderSub.Text   = "Encuentra y elimina los top 100 archivos más grandes";
        }

        private void NavDuplicates_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageDuplicates);
            HeaderTitle.Text = "Buscador de Clones";
            HeaderSub.Text   = "Encuentra archivos con contenido exactamente igual";
        }

        private void NavTreemap_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageTreemap);
            HeaderTitle.Text = "Mapa de Disco (Treemap)";
            HeaderSub.Text   = "Visualización proporcional de almacenamiento";
        }

        // ── Phase 3 Navigation ──
        private void NavShredder_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageShredder);
            HeaderTitle.Text = "Destructor de Archivos";
            HeaderSub.Text   = "Elimina archivos permanentemente sin recuperación";
        }

        private void NavPrivacy_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PagePrivacy);
            HeaderTitle.Text = "Escáner de Privacidad";
            HeaderSub.Text   = "Busca archivos de texto con contraseñas expuestas";
        }

        private void NavProcess_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageProcess);
            HeaderTitle.Text = "Procesos";
            HeaderSub.Text   = "Administra el consumo de memoria en tiempo real";
        }

        private void NavNetwork_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageNetwork);
            HeaderTitle.Text = "Red TCP/UDP";
            HeaderSub.Text   = "Monitorea comunicaciones activas sospechosas";
        }

        // ── Phase 4 Navigation ──
        private void NavStartup_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageStartup);
            HeaderTitle.Text = "Arranque";
            HeaderSub.Text   = "Controla qué programas inician con Windows";
        }

        private void NavUninstaller_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageUninstaller);
            HeaderTitle.Text = "Desinstalador";
            HeaderSub.Text   = "Elimina programas y sobras residuales del sistema";
        }

        private void NavRegistry_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageRegistry);
            HeaderTitle.Text = "Registro Windows";
            HeaderSub.Text   = "Repara extensiones y asociacines nulas del sistema";
        }

        private void NavFirewall_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageFirewall);
            HeaderTitle.Text = "Reglas de Firewall";
            HeaderSub.Text   = "Políticas de red configuradas";
        }

        // ── Phase 5 Navigation ──────────────────────────────────────────────────
        private void NavSystemInfo_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageSystemInfo);
            HeaderTitle.Text = "Información del Sistema";
            HeaderSub.Text   = "Hardware y especificaciones de tu PC";
            PageSystemInfo.LoadInfo();
        }

        private void NavHistory_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageHistory);
            HeaderTitle.Text = "Historial de Limpieza";
            HeaderSub.Text   = "Registro de todas tus limpiezas anteriores";
            PageHistory.LoadHistory();
        }

        private void NavSettings_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageSettings);
            HeaderTitle.Text = "Configuración";
            HeaderSub.Text   = "Personaliza el comportamiento de la aplicación";
            PageSettings.LoadSettings();
        }

        private void NavRamBooster_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            AnimateNavIndicatorTo(sender as FrameworkElement);
            ShowPage(PageRamBooster);
            HeaderTitle.Text = "RAM Booster";
            HeaderSub.Text   = "Monitoreo y optimización de memoria en tiempo real";
            PageRamBooster.StartMonitor();
        }

        // ── Window controls ─────────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
        private void MinBtn_Click(object sender, RoutedEventArgs e)   => WindowState = WindowState.Minimized;
        private void MaxBtn_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}