using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PresenterTimerApp
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private TimeSpan _remainingTime;
        private string _messageText = "";
        private string _statusMessage = "";
        private int _yellowAlertThreshold = 300; // Default 5 minutes in seconds
        private int _redAlertThreshold = 60; // Default 1 minute in seconds
        private double _yellowFlashSpeed = 1.0; // Default 1 second
        private double _redFlashSpeed = 1.0; // Default 1 second
        private ObservableCollection<string> _recentImages = new ObservableCollection<string>();
        private long _maxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
        private bool _enableAnimations = true;
        private string _timerFont = "Arial";
        private string _messageFont = "Arial";

        public event PropertyChangedEventHandler? PropertyChanged;

        public TimeSpan RemainingTime
        {
            get => _remainingTime;
            set
            {
                _remainingTime = value;
                OnPropertyChanged();
            }
        }

        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int YellowAlertThreshold
        {
            get => _yellowAlertThreshold;
            set
            {
                _yellowAlertThreshold = value;
                OnPropertyChanged();
            }
        }

        public int RedAlertThreshold
        {
            get => _redAlertThreshold;
            set
            {
                _redAlertThreshold = value;
                OnPropertyChanged();
            }
        }

        public double YellowFlashSpeed
        {
            get => _yellowFlashSpeed;
            set
            {
                _yellowFlashSpeed = value;
                OnPropertyChanged();
            }
        }

        public double RedFlashSpeed
        {
            get => _redFlashSpeed;
            set
            {
                _redFlashSpeed = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> RecentImages
        {
            get => _recentImages;
            set
            {
                _recentImages = value;
                OnPropertyChanged();
            }
        }

        public long MaxImageSizeBytes
        {
            get => _maxImageSizeBytes;
            set
            {
                _maxImageSizeBytes = value;
                OnPropertyChanged();
            }
        }

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                _enableAnimations = value;
                OnPropertyChanged();
            }
        }

        public string TimerFont
        {
            get => _timerFont;
            set
            {
                _timerFont = value;
                OnPropertyChanged();
            }
        }

        public string MessageFont
        {
            get => _messageFont;
            set
            {
                _messageFont = value;
                OnPropertyChanged();
            }
        }

        public string? PreviousBackgroundColor { get; set; } = "Black";

        public void AddRecentImage(string path)
        {
            if (!_recentImages.Contains(path))
            {
                _recentImages.Add(path);
            }
        }

        public void ClearRecentImages()
        {
            _recentImages.Clear();
            OnPropertyChanged(nameof(RecentImages));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}