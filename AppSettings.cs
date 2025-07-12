using System;

namespace PresenterTimerApp
{
    [Serializable]
    public class AppSettings
    {
        public double TimerFontSize { get; set; }
        public string TimerColor { get; set; }
        public double MessageFontSize { get; set; }
        public string MessageColor { get; set; }
        public string BackgroundColor { get; set; }
        public int MonitorIndex { get; set; }
        public bool IsTopmost { get; set; }
        public bool IsFullScreen { get; set; }
        public string ImagePath { get; set; }
        public string[] RecentImages { get; set; }
        public int YellowAlertThreshold { get; set; }
        public int RedAlertThreshold { get; set; }
        public long MaxImageSizeBytes { get; set; }
        public bool EnableAnimations { get; set; }
        public string TimerFont { get; set; }
        public string MessageFont { get; set; }
        public string PreviousBackgroundColor { get; set; }

        public AppSettings()
        {
            TimerFontSize = 95;
            TimerColor = "White";
            MessageFontSize = 60;
            MessageColor = "White";
            BackgroundColor = "Black";
            MonitorIndex = -1;
            IsTopmost = false;
            IsFullScreen = false;
            ImagePath = "";
            RecentImages = new string[0];
            YellowAlertThreshold = 300; // 5 minutes in seconds
            RedAlertThreshold = 60; // 1 minute in seconds
            MaxImageSizeBytes = 5 * 1024 * 1024;
            EnableAnimations = true;
            TimerFont = "Arial";
            MessageFont = "Arial";
            PreviousBackgroundColor = "Black";
        }
    }
}