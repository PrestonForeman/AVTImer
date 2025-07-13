using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PresenterTimerApp
{
    public partial class DisplayWindow : Window
    {
        private DispatcherTimer _flashTimer;
        private bool _isFlashing = false;
        private bool _isRed = false;
        private System.Windows.Media.Brush _originalTimerColor;
        private bool _enableAnimations = true;

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set => _enableAnimations = value;
        }

        public DisplayWindow()
        {
            InitializeComponent();
            _flashTimer = new DispatcherTimer();
            _originalTimerColor = System.Windows.Media.Brushes.White;
            SetupFlashTimer();
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.F11)
                {
                    if (WindowState == WindowState.Maximized)
                    {
                        WindowState = WindowState.Normal;
                        WindowStyle = WindowStyle.SingleBorderWindow;
                    }
                    else
                    {
                        WindowState = WindowState.Maximized;
                        WindowStyle = WindowStyle.None;
                    }
                }
            };
        }

        private void SetupFlashTimer()
        {
            _flashTimer.Interval = TimeSpan.FromMilliseconds(500);
            _flashTimer.Tick += (s, e) =>
            {
                if (!_isFlashing) return;
                TimerText.Foreground = _isRed ? _originalTimerColor : System.Windows.Media.Brushes.Red;
                _isRed = !_isRed;
            };
        }

        public void SetTime(string time)
        {
            TimerText.Text = time;
            if (time == "00:00:00")
                StartFlashing();
            else
                StopFlashing();
        }

        public void SetMessage(string message)
        {
            MessageText.Text = message;
            if (_enableAnimations && !string.IsNullOrEmpty(message))
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                MessageText.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        public void SetTimerFont(System.Windows.Media.FontFamily fontFamily, double fontSize, System.Windows.Media.Brush color)
        {
            TimerText.FontFamily = fontFamily;
            if (ActualWidth > 0)
            {
                var maxFontSize = ActualWidth * 0.95;
                var minFontSize = ActualWidth * 0.25;
                TimerText.FontSize = Math.Max(minFontSize, Math.Min(maxFontSize, fontSize));
            }
            else
            {
                TimerText.FontSize = fontSize;
            }
            TimerText.Foreground = color;
            _originalTimerColor = color;
        }

        public void SetMessageFont(System.Windows.Media.FontFamily fontFamily, double fontSize, System.Windows.Media.Brush color)
        {
            MessageText.FontFamily = fontFamily;
            MessageText.FontSize = fontSize;
            MessageText.Foreground = color;
        }

        public void UpdateTimerColor(System.Windows.Media.Brush color)
        {
            if (!_isFlashing)
            {
                TimerText.Foreground = color;
                _originalTimerColor = color;
            }
        }

        public void SetBackgroundImage(string imagePath)
        {
            try
            {
                System.Windows.Media.ImageBrush brush = new System.Windows.Media.ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                    Stretch = System.Windows.Media.Stretch.UniformToFill
                };
                DisplayGrid.Background = brush;
                if (_enableAnimations)
                {
                    var animation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };
                    DisplayGrid.BeginAnimation(UIElement.OpacityProperty, animation);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load background image: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void ClearBackgroundImage(System.Windows.Media.Brush? color = null)
        {
            DisplayGrid.Background = color ?? System.Windows.Media.Brushes.Black;
            if (_enableAnimations)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                DisplayGrid.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        public void StartFlashing()
        {
            _isFlashing = true;
            _flashTimer.Start();
        }

        public void StopFlashing()
        {
            _isFlashing = false;
            _flashTimer.Stop();
            TimerText.Foreground = _originalTimerColor;
        }

        public TextBlock TimerTextControl => TimerText;

        public TextBlock MessageTextControl => MessageText;
    }
}