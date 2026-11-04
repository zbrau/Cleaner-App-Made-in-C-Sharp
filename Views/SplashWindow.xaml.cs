using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace AppCleanerPro.Views
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public async Task AnimateAndLoad()
        {
            var steps = new[]
            {
                (0.10, "Verificando permisos de administrador..."),
                (0.28, "Cargando motores de análisis..."),
                (0.45, "Inicializando módulos de seguridad..."),
                (0.65, "Preparando interfaz NovaDash..."),
                (0.88, "Cargando configuración de usuario..."),
                (1.00, "¡Listo!"),
            };

            foreach (var (pct, msg) in steps)
            {
                StatusText.Text = msg;
                AnimateBar(pct);
                await Task.Delay(340);
            }

            await Task.Delay(300);
        }

        private void AnimateBar(double pct)
        {
            const double maxW = 280;
            var anim = new DoubleAnimation
            {
                To = maxW * pct,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            LoadingBar.BeginAnimation(WidthProperty, anim);
        }
    }
}
