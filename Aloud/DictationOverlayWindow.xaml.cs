using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        private bool _isClosing = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CenterWindow();
            StartWaveformAnimation();
            
            if (_isClosing) return;
            
            var animOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            var animTransform = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(200));
            animTransform.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            
            this.BeginAnimation(Window.OpacityProperty, animOpacity);
            if (MainBorder.RenderTransform is TranslateTransform trans)
            {
                trans.BeginAnimation(TranslateTransform.YProperty, animTransform);
            }
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

        public float CurrentAudioLevel { get; set; } = 0f;

        private void StartWaveformAnimation()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(100); // Faster updates for responsiveness
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
            double newHeight = 3; // flat

            // Only move if we hear sound
            if (CurrentAudioLevel > 0.02f) 
            {
                // Scale based on audio level, multiplier is arbitrary to make it look good
                double scale = Math.Min(1.0, (CurrentAudioLevel - 0.02) * 5.0);
                double scaledMin = 3 + (min - 3) * scale;
                double scaledMax = 3 + (max - 3) * scale;

                newHeight = scaledMin + (_rnd.NextDouble() * (scaledMax - scaledMin));
            }

            var anim = new DoubleAnimation(newHeight, TimeSpan.FromMilliseconds(100));
            bar.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }

        public async void HideAndClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            var animOpacity = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            var animTransform = new DoubleAnimation(0, 20, TimeSpan.FromMilliseconds(150));
            animTransform.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            
            this.BeginAnimation(Window.OpacityProperty, animOpacity);
            if (MainBorder.RenderTransform is TranslateTransform trans)
            {
                trans.BeginAnimation(TranslateTransform.YProperty, animTransform);
            }
            
            await System.Threading.Tasks.Task.Delay(200);
            try { this.Close(); } catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            _animationTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
