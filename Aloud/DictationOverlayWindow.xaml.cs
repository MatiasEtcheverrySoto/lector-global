using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace LectorGlobalApp
{
    public partial class DictationOverlayWindow : Window
    {
        private DispatcherTimer _animationTimer;
        private Random _rnd = new Random();

        public event Action OnCancel;
        public event Action OnConfirm;

        public DictationOverlayWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CenterWindow();
            StartWaveformAnimation();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterWindow();
        }

        private void CenterWindow()
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.ActualWidth) / 2;
            this.Top = workArea.Height - this.ActualHeight - 40; // 40px from bottom
        }

        public void UpdateText(string text)
        {
            TxtPartial.Text = text;
            if (string.IsNullOrWhiteSpace(text))
            {
                TxtPartial.Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                TxtPartial.Margin = new Thickness(0, 0, 15, 0);
            }
        }

        private void StartWaveformAnimation()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(150);
            _animationTimer.Tick += (s, e) =>
            {
                AnimateBar(Bar1, 6, 14);
                AnimateBar(Bar2, 10, 20);
                AnimateBar(Bar3, 16, 24);
                AnimateBar(Bar4, 10, 18);
                AnimateBar(Bar5, 14, 22);
                AnimateBar(Bar6, 8, 16);
                AnimateBar(Bar7, 14, 24);
                AnimateBar(Bar8, 10, 20);
                AnimateBar(Bar9, 6, 12);
            };
            _animationTimer.Start();
        }

        private void AnimateBar(System.Windows.Shapes.Rectangle bar, double min, double max)
        {
            double newHeight = min + (_rnd.NextDouble() * (max - min));
            var anim = new DoubleAnimation(newHeight, TimeSpan.FromMilliseconds(150));
            bar.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }

        private void BtnCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnCancel?.Invoke();
        }

        private void BtnConfirm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnConfirm?.Invoke();
        }

        protected override void OnClosed(EventArgs e)
        {
            _animationTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
