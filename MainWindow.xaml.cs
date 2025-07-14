using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Globalization;
using System.Windows.Media.TextFormatting;

namespace PresenterTimerApp
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly DisplayWindow _displayWindow;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _fontUpdateTimer;
        private readonly FileService _fileService;
        private readonly Logger _logger;
        private readonly string _dataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
        private readonly string _messageFilePath;
        private readonly string _imagePathsFile;
        private readonly string _settingsFile;
        private string _imagePath = "";
        private int _selectedImageIndex = -1;
        private bool _isPreviewMode = false;
        private bool _isPaused = false;
        private bool _enableAnimations = true;

        public MainWindow()
        {
            Directory.CreateDirectory(_dataFolder);
            _messageFilePath = Path.Combine(_dataFolder, "messages.txt");
            _imagePathsFile = Path.Combine(_dataFolder, "imagePath.txt");
            _settingsFile = Path.Combine(_dataFolder, "settings.json");
            _fileService = new FileService(_dataFolder);
            _logger = new Logger(_dataFolder);

            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = _viewModel;

            _displayWindow = new DisplayWindow();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _fontUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _fontUpdateTimer.Tick += (object sender, EventArgs e) => { UpdateTimerFont(); UpdateMessageFont(); };

            SetupEventHandlers();
            LoadFonts();
            LoadSavedImage();
            LoadMessagesToComboBox();
            LoadSettings();
            PopulateMonitorComboBox();
            CheckSecondaryMonitor();

            MoveDisplayToSecondaryMonitor();
            _displayWindow.Show();
            ImagePreview1.Drop += Image_Drop;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.MessageText))
            {
                // Update only the draft preview, not the live preview
                DraftMessagePreview.Text = _viewModel.MessageText;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTimerFont();
            UpdateMessageFont();
            _viewModel.StatusMessage = "Application started";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettingsAsync();
        }

        private void SetupEventHandlers()
        {
            StartButton.Click += StartButton_Click;
            PauseButton.Click += PauseButton_Click;
            ResetButton.Click += ResetButton_Click;
            SendMessageButton.Click += SendMessageButton_Click;
            ClearMessageButton.Click += (object sender, RoutedEventArgs e) => ClearMessage();
            HideBackgroundButton.Click += HideBackgroundButton_Click;
            OpenDisplayButton.Click += (object sender, RoutedEventArgs e) => { if (!_displayWindow.IsVisible) _displayWindow.Show(); _viewModel.StatusMessage = "Display window opened"; };
            CloseDisplayButton.Click += (object sender, RoutedEventArgs e) => { _displayWindow.Hide(); _viewModel.StatusMessage = "Display window closed"; };
            FullScreenButton.Click += (object sender, RoutedEventArgs e) => ToggleFullScreen();
            TopmostToggle.Checked += (object sender, RoutedEventArgs e) => { _displayWindow.Topmost = true; _viewModel.StatusMessage = "Display set to always on top"; };
            TopmostToggle.Unchecked += (object sender, RoutedEventArgs e) => { _displayWindow.Topmost = false; _viewModel.StatusMessage = "Display no longer always on top"; };
            MonitorComboBox.SelectionChanged += MonitorComboBox_SelectionChanged;
            PredefinedMessageComboBox.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (PredefinedMessageComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
                {
                    _viewModel.MessageText = item.Content.ToString();
                    // Update only the draft preview, not the live preview
                    DraftMessagePreview.Text = _viewModel.MessageText;
                }
            };
            TimerFontSizeSlider.ValueChanged += (object sender, RoutedPropertyChangedEventArgs<double> e) => _fontUpdateTimer.Start();
            TimerColorComboBox.SelectionChanged += (object sender, SelectionChangedEventArgs e) => UpdateTimerColor();
            MessageFontSizeSlider.ValueChanged += (object sender, RoutedPropertyChangedEventArgs<double> e) => _fontUpdateTimer.Start();
            MessageColorComboBox.SelectionChanged += (object sender, SelectionChangedEventArgs e) => UpdateMessageFont();
            BackgroundColorPicker.SelectionChanged += (object sender, SelectionChangedEventArgs e) => UpdateBackgroundColor();
            KeyDown += MainWindow_KeyDown;
            LoadImage1.Click += LoadImage_Click;
            PreviewImage1.Click += PreviewImage_Click;
            TakeLiveImage1.Click += TakeLiveImage_Click;
            DeleteImageButton.Click += DeleteImageButton_Click;
            RecentImagesComboBox.SelectionChanged += RecentImagesComboBox_SelectionChanged;
            SettingsButton.Click += SettingsButton_Click;
            
            // Add handlers for message management buttons
            AddMessageButton.Click += AddMessageButton_Click;
            EditMessageButton.Click += EditMessageButton_Click;
            DeleteMessageButton.Click += DeleteMessageButton_Click;
        }

        private void Image_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0 && Path.GetExtension(files[0]).ToLower() is ".jpg" or ".jpeg" or ".png" or ".bmp")
                {
                    try
                    {
                        if (new FileInfo(files[0]).Length > _viewModel.MaxImageSizeBytes)
                        {
                            System.Windows.MessageBox.Show($"Image size exceeds limit ({_viewModel.MaxImageSizeBytes / 1024 / 1024} MB).", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            _viewModel.StatusMessage = "Image not loaded: size too large";
                            return;
                        }
                        _imagePath = files[0];
                        ImagePreview1.Source = new BitmapImage(new Uri(_imagePath));
                        _viewModel.AddRecentImage(_imagePath);
                        PopulateRecentImages();
                        SaveImagePathAsync();
                        _viewModel.StatusMessage = $"Image dropped: {Path.GetFileName(_imagePath)}";
                    }
                    catch (Exception ex)
                    {
                        _ = _logger.LogErrorAsync($"Error dropping image: {ex.Message}\n{ex.StackTrace}");
                        _viewModel.StatusMessage = "Error dropping image";
                    }
                }
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Space:
                    if (_timer.IsEnabled) PauseButton_Click(null!, null!); else StartButton_Click(null!, null!);
                    break;
                case System.Windows.Input.Key.R:
                    ResetButton_Click(null!, null!);
                    break;
                case System.Windows.Input.Key.P:
                    PauseButton_Click(null!, null!);
                    break;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_timer.IsEnabled && _viewModel.RemainingTime.TotalSeconds > 0)
            {
                _timer.Start();
                _viewModel.StatusMessage = "Timer started";
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                _isPaused = true;
                _viewModel.StatusMessage = "Timer paused";
            }
            else if (_isPaused)
            {
                _timer.Start();
                _isPaused = false;
                _viewModel.StatusMessage = "Timer resumed";
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SetTimeFromUI();
            _timer.Stop();
            _isPaused = false;
            _displayWindow.StopFlashing();
            UpdateTimeDisplays();
            _viewModel.StatusMessage = "Timer reset";
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.MessageText))
            {
                System.Windows.MessageBox.Show("Please enter a message.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                _viewModel.StatusMessage = "Message not sent: empty text";
                return;
            }
            LiveMessagePreview.Text = _viewModel.MessageText;
            _displayWindow.SetMessage(_viewModel.MessageText);
            TriggerAnimation(LiveMessagePreview, _displayWindow.MessageTextControl);
            _viewModel.StatusMessage = "Message displayed";
        }

        private void ClearMessage()
        {
            // Don't clear _viewModel.MessageText anymore to keep it in the text box
            LiveMessagePreview.Text = "";
            _displayWindow.SetMessage("");
            TriggerAnimation(LiveMessagePreview, _displayWindow.MessageTextControl);
            _viewModel.StatusMessage = "Message hidden";
        }

        private void HideBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            ClearBackground();
            _viewModel.StatusMessage = "Background image hidden";
        }

        private void ClearBackground()
        {
            var colorName = _viewModel.PreviousBackgroundColor ?? "Black";
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorName);
            var brush = new SolidColorBrush(color);
            _displayWindow.ClearBackgroundImage(brush);
            LivePreviewGrid.Background = brush;
            DraftPreviewGrid.Background = brush;
            _selectedImageIndex = -1;
            _isPreviewMode = false;
            TriggerAnimation(LivePreviewGrid, DraftPreviewGrid);
            SaveSettingsAsync();
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    if (new FileInfo(ofd.FileName).Length > _viewModel.MaxImageSizeBytes)
                    {
                        System.Windows.MessageBox.Show($"Image size exceeds limit ({_viewModel.MaxImageSizeBytes / 1024 / 1024} MB).", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        _viewModel.StatusMessage = "Image not loaded: size too large";
                        return;
                    }
                    _imagePath = ofd.FileName;
                    ImagePreview1.Source = new BitmapImage(new Uri(_imagePath));
                    _viewModel.AddRecentImage(_imagePath);
                    PopulateRecentImages();
                    SaveImagePathAsync();
                    _viewModel.StatusMessage = $"Image loaded: {Path.GetFileName(_imagePath)}";
                }
                catch (Exception ex)
                {
                    _ = _logger.LogErrorAsync($"Error loading image: {ex.Message}\n{ex.StackTrace}");
                    _viewModel.StatusMessage = "Error loading image";
                }
            }
        }

        private void PreviewImage_Click(object sender, RoutedEventArgs e)
        {
            _selectedImageIndex = 0;
            _isPreviewMode = true;
            if (!string.IsNullOrEmpty(_imagePath))
            {
                var brush = new ImageBrush(new BitmapImage(new Uri(_imagePath))) { Stretch = Stretch.UniformToFill };
                DraftPreviewGrid.Background = brush;
                TriggerAnimation(DraftPreviewGrid);
                _viewModel.StatusMessage = "Image previewed";
            }
            UpdateImageBorder();
        }

        private void TakeLiveImage_Click(object sender, RoutedEventArgs e)
        {
            _selectedImageIndex = 0;
            _isPreviewMode = false;
            if (!string.IsNullOrEmpty(_imagePath))
            {
                _displayWindow.SetBackgroundImage(_imagePath);
                LivePreviewGrid.Background = new ImageBrush(new BitmapImage(new Uri(_imagePath))) { Stretch = Stretch.UniformToFill };
                DraftPreviewGrid.Background = new ImageBrush(new BitmapImage(new Uri(_imagePath))) { Stretch = Stretch.UniformToFill };
                TriggerAnimation(LivePreviewGrid, DraftPreviewGrid, _displayWindow.DisplayGrid);
                _viewModel.StatusMessage = "Image taken live";
            }
            UpdateImageBorder();
            SaveSettingsAsync();
        }

        private void RecentImagesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentImagesComboBox.SelectedItem != null)
            {
                string selectedFileName = RecentImagesComboBox.SelectedItem.ToString();
                string fullPath = _viewModel.RecentImages.FirstOrDefault(p => Path.GetFileName(p) == selectedFileName);
                
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    _imagePath = fullPath;
                    ImagePreview1.Source = new BitmapImage(new Uri(fullPath));
                    _viewModel.StatusMessage = $"Selected recent image: {Path.GetFileName(fullPath)}";
                    SaveImagePathAsync();
                }
            }
        }

        private void UpdateImageBorder()
        {
            var border = ImageBorder;
            border.BorderBrush = _isPreviewMode && _selectedImageIndex == 0 ? System.Windows.Media.Brushes.Green :
                                _selectedImageIndex == 0 && !_isPreviewMode ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Gray;
        }

        private async void SaveImagePathAsync()
        {
            await _fileService.SaveImagePathAsync(_imagePathsFile, _imagePath);
        }

        private async void LoadSavedImage()
        {
            try
            {
                _imagePath = await _fileService.LoadImagePathAsync(_imagePathsFile) ?? "";
                if (File.Exists(_imagePath))
                {
                    ImagePreview1.Source = new BitmapImage(new Uri(_imagePath));
                    _viewModel.AddRecentImage(_imagePath);
                    PopulateRecentImages();
                }
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error loading saved image: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error loading saved image";
            }
        }

        private void PopulateRecentImages()
        {
            RecentImagesComboBox.Items.Clear();
            foreach (var path in _viewModel.RecentImages)
            {
                if (File.Exists(path))
                {
                    // Store the filename for display but use Tag to store the full path
                    string fileName = Path.GetFileName(path);
                    RecentImagesComboBox.Items.Add(fileName);
                }
            }
        }

        private void LoadFonts()
        {
            TimerColorComboBox.SelectedIndex = 0;
            MessageColorComboBox.SelectedIndex = 0;
        }

        private async void LoadMessagesToComboBox()
        {
            PredefinedMessageComboBox.Items.Clear();
            try
            {
                var messages = await _fileService.LoadMessagesAsync(_messageFilePath);
                if (messages.Length == 0)
                {
                    messages = new[] { "Please Wrap Up", "Time is up" };
                    await _fileService.SaveMessagesAsync(_messageFilePath, messages);
                }
                foreach (var msg in messages)
                {
                    PredefinedMessageComboBox.Items.Add(new ComboBoxItem { Content = msg });
                }
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error loading messages: {ex.Message}\n{ex.StackTrace}");
                var defaultMessages = new[] { "Please Wrap Up", "Time is up" };
                await _fileService.SaveMessagesAsync(_messageFilePath, defaultMessages);
                foreach (var msg in defaultMessages)
                {
                    PredefinedMessageComboBox.Items.Add(new ComboBoxItem { Content = msg });
                }
                _viewModel.StatusMessage = "Error loading messages; using defaults";
            }
        }

        private void SetTimeFromUI()
        {
            int h = int.Parse(((ComboBoxItem)HoursComboBox.SelectedItem)?.Content?.ToString() ?? "0");
            int m = int.Parse(((ComboBoxItem)MinutesComboBox.SelectedItem)?.Content?.ToString() ?? "0");
            int s = int.Parse(((ComboBoxItem)SecondsComboBox.SelectedItem)?.Content?.ToString() ?? "0");
            _viewModel.RemainingTime = new TimeSpan(h, m, s);
            UpdateTimeDisplays();
        }

        private void UpdateTimeDisplays()
        {
            _displayWindow.SetTime(_viewModel.RemainingTime.ToString("hh\\:mm\\:ss"));
            LiveTimerPreview.Text = _viewModel.RemainingTime.ToString("hh\\:mm\\:ss");
            DraftTimerPreview.Text = _viewModel.RemainingTime.ToString("hh\\:mm\\:ss");

            if (_viewModel.RemainingTime.TotalSeconds <= 0)
            {
                _displayWindow.StopFlashing();
                LiveTimerPreview.Foreground = _displayWindow.TimerTextControl.Foreground;
                DraftTimerPreview.Foreground = _displayWindow.TimerTextControl.Foreground;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_viewModel.RemainingTime.TotalSeconds > 0)
            {
                _viewModel.RemainingTime = _viewModel.RemainingTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimeDisplays();
                UpdateTimerColorForWarnings();
            }
            else
            {
                UpdateTimeDisplays();
                _timer.Stop();
                _isPaused = false;
                _displayWindow.StopFlashing();
                LiveTimerPreview.Foreground = _displayWindow.TimerTextControl.Foreground;
                DraftTimerPreview.Foreground = _displayWindow.TimerTextControl.Foreground;
            }
        }

        private void UpdateTimerColorForWarnings()
        {
            var seconds = _viewModel.RemainingTime.TotalSeconds;
            var brush = seconds <= _viewModel.RedAlertThreshold ? System.Windows.Media.Brushes.Red :
                        seconds <= _viewModel.YellowAlertThreshold ? System.Windows.Media.Brushes.Yellow :
                        System.Windows.Media.Brushes.White;

            if (seconds <= 0)
            {
                _displayWindow.StartFlashing();
                LiveTimerPreview.Foreground = _displayWindow.TimerTextControl.Foreground;
            }
            else
            {
                _displayWindow.UpdateTimerColor(brush);
                LiveTimerPreview.Foreground = brush;
                DraftTimerPreview.Foreground = brush;
            }
        }

        private void UpdateTimerFont()
        {
            try
            {
                if (_displayWindow.ActualWidth <= 0 || LivePreviewGrid.ActualWidth <= 0) return;

                var font = "Arial";
                var sizePercentage = TimerFontSizeSlider.Value / 100.0;
                var displayWidth = _displayWindow.ActualWidth;
                var displayHeight = _displayWindow.ActualHeight;
                var previewWidth = LivePreviewGrid.ActualWidth;

                // Get current time text
                string timeText = _viewModel.RemainingTime.ToString("hh\\:mm\\:ss");

                // Calculate maximum allowed size (95% of display width)
                double maxAllowedWidth = displayWidth * 0.95;

                // Start with a base size determined by the slider
                double baseSize = displayWidth * sizePercentage * 0.25; // 0.25 is a scaling factor

                // Ensure the text fits within the maximum width
                var testFormatting = new FormattedText(
    timeText,
    CultureInfo.CurrentCulture,
    System.Windows.FlowDirection.LeftToRight, // Correctly qualified with the type name
    new Typeface(new System.Windows.Media.FontFamily(font), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
    baseSize,
    System.Windows.Media.Brushes.Black, // Specify the namespace explicitly
    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                // If text is wider than allowed max, scale it down
                if (testFormatting.Width > maxAllowedWidth)
                {
                    baseSize = baseSize * (maxAllowedWidth / testFormatting.Width);
                }

                // Apply a minimum size
                baseSize = Math.Max(10, baseSize);

                // Scale for the preview
                var scaleFactor = previewWidth / displayWidth;
                var previewSize = baseSize * scaleFactor;

                // Get color
                var colorName = ((ComboBoxItem)TimerColorComboBox.SelectedItem)?.Content.ToString() ?? "White";
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorName);
                var brush = new SolidColorBrush(color);

                // Apply the font settings
                _displayWindow.SetTimerFont(new System.Windows.Media.FontFamily(font), baseSize, brush);
                LiveTimerPreview.FontFamily = new System.Windows.Media.FontFamily(font);
                DraftTimerPreview.FontFamily = new System.Windows.Media.FontFamily(font);
                LiveTimerPreview.FontSize = previewSize;
                DraftTimerPreview.FontSize = previewSize;
                LiveTimerPreview.Foreground = brush;
                DraftTimerPreview.Foreground = brush;
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error updating timer font: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error updating timer font";
            }
        }

        private void UpdateMessageFont()
        {
            try
            {
                if (_displayWindow.ActualHeight <= 0 || LivePreviewGrid.ActualWidth <= 0) return;
                var font = "Arial";
                var sliderValue = MessageFontSizeSlider.Value;
                
                // Calculate percentage based on slider position
                double percentage;
                double minSliderValue = 20;  // Min slider value
                double maxSliderValue = 100; // Max slider value
                double defaultValue = 40;    // Default/middle value
                
                if (sliderValue <= defaultValue)
                {
                    // Linear mapping from min to default (40% to 75%)
                    percentage = 40.0 + (sliderValue - minSliderValue) * (75.0 - 40.0) / (defaultValue - minSliderValue);
                }
                else
                {
                    // Linear mapping from default to max (75% to 95%)
                    percentage = 75.0 + (sliderValue - defaultValue) * (95.0 - 75.0) / (maxSliderValue - defaultValue);
                }
                
                // Calculate font size based on display width and percentage
                double displayWidth = _displayWindow.ActualWidth;
                double fontSize = displayWidth * (percentage / 100.0) * 0.1; // Scale factor for appropriate text size
                
                var colorName = ((ComboBoxItem)MessageColorComboBox.SelectedItem)?.Content.ToString() ?? "White";
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorName);
                var brush = new SolidColorBrush(color);

                _displayWindow.SetMessageFont(new System.Windows.Media.FontFamily(font), fontSize, brush);
                
                // Set font properties for preview elements
                LiveMessagePreview.FontFamily = new System.Windows.Media.FontFamily(font);
                DraftMessagePreview.FontFamily = new System.Windows.Media.FontFamily(font);
                
                // Scale font size for preview panels based on their width relative to display window
                double livePreviewScale = LivePreviewGrid.ActualWidth / displayWidth;
                double draftPreviewScale = DraftPreviewGrid.ActualWidth / displayWidth;
                
                LiveMessagePreview.FontSize = fontSize * livePreviewScale;
                DraftMessagePreview.FontSize = fontSize * draftPreviewScale;
                
                LiveMessagePreview.Foreground = brush;
                DraftMessagePreview.Foreground = brush;
                
                // Ensure text wrapping is enabled
                LiveMessagePreview.TextWrapping = TextWrapping.Wrap;
                DraftMessagePreview.TextWrapping = TextWrapping.Wrap;
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error updating message font: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error updating message font";
            }
        }

        private void UpdateTimerColor()
        {
            try
            {
                var colorName = ((ComboBoxItem)TimerColorComboBox.SelectedItem)?.Content.ToString() ?? "White";
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorName);
                var brush = new SolidColorBrush(color);
                _displayWindow.UpdateTimerColor(brush);
                LiveTimerPreview.Foreground = brush;
                DraftTimerPreview.Foreground = brush;
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error updating timer color: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error updating timer color";
            }
            SaveSettingsAsync();
        }

        private void UpdateBackgroundColor()
        {
            try
            {
                var colorName = ((ComboBoxItem)BackgroundColorPicker.SelectedItem)?.Content.ToString() ?? "Black";
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorName);
                var brush = new SolidColorBrush(color);
                _viewModel.PreviousBackgroundColor = colorName;
                if (_selectedImageIndex == -1)
                {
                    _displayWindow.ClearBackgroundImage(brush);
                    LivePreviewGrid.Background = brush;
                    DraftPreviewGrid.Background = brush;
                    TriggerAnimation(LivePreviewGrid, DraftPreviewGrid);
                }
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error updating background color: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error updating background color";
            }
            SaveSettingsAsync();
        }

        private void TriggerAnimation(params FrameworkElement[] elements)
        {
            if (!_enableAnimations) return;
            foreach (var element in elements)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                element.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        private void ToggleFullScreen()
        {
            if (_displayWindow.WindowState == WindowState.Maximized)
            {
                _displayWindow.WindowState = WindowState.Normal;
                _displayWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                _viewModel.StatusMessage = "Full-screen disabled";
            }
            else
            {
                _displayWindow.WindowState = WindowState.Maximized;
                _displayWindow.WindowStyle = WindowStyle.None;
                _viewModel.StatusMessage = "Full-screen enabled";
            }
            SaveSettingsAsync();
        }

        private void PopulateMonitorComboBox()
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                MonitorComboBox.Items.Add($"Monitor {i + 1} ({screens[i].DeviceName})");
            }
            MonitorComboBox.SelectedIndex = screens.Length > 1 ? 1 : 0;
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonitorComboBox.SelectedIndex >= 0)
            {
                var screen = System.Windows.Forms.Screen.AllScreens[MonitorComboBox.SelectedIndex];
                _displayWindow.Left = screen.Bounds.Left;
                _displayWindow.Top = screen.Bounds.Top;
                _displayWindow.Width = screen.Bounds.Width;
                _displayWindow.Height = screen.Bounds.Height;
                _viewModel.StatusMessage = $"Display moved to Monitor {MonitorComboBox.SelectedIndex + 1}";
                SaveSettingsAsync();
            }
        }

        private void MoveDisplayToSecondaryMonitor()
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length > 1)
            {
                var secondary = screens.FirstOrDefault(s => !s.Primary) ?? screens[0];
                _displayWindow.Left = secondary.Bounds.Left;
                _displayWindow.Top = secondary.Bounds.Top;
                _displayWindow.Width = secondary.Bounds.Width;
                _displayWindow.Height = secondary.Bounds.Height;
                MonitorComboBox.SelectedIndex = Array.IndexOf(screens, secondary);
                _viewModel.StatusMessage = $"Display on Monitor {MonitorComboBox.SelectedIndex + 1}";
            }
        }

        private void CheckSecondaryMonitor()
        {
            if (System.Windows.Forms.Screen.AllScreens.Length == 1)
            {
                System.Windows.MessageBox.Show("No secondary monitor detected. Display window will use the primary monitor.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _viewModel.StatusMessage = "Using primary monitor";
            }
        }

        private async void SaveSettingsAsync()
        {
            try
            {
                await _fileService.BackupSettingsAsync(_settingsFile, Path.Combine(_dataFolder, "settings.bak"));
                var settings = new AppSettings
                {
                    TimerFontSize = TimerFontSizeSlider.Value,
                    TimerColor = ((ComboBoxItem)TimerColorComboBox.SelectedItem)?.Content.ToString() ?? "White",
                    MessageFontSize = MessageFontSizeSlider.Value,
                    MessageColor = ((ComboBoxItem)MessageColorComboBox.SelectedItem)?.Content.ToString() ?? "White",
                    BackgroundColor = ((ComboBoxItem)BackgroundColorPicker.SelectedItem)?.Content.ToString() ?? "Black",
                    MonitorIndex = MonitorComboBox.SelectedIndex,
                    IsTopmost = TopmostToggle.IsChecked ?? false,
                    IsFullScreen = _displayWindow.WindowState == WindowState.Maximized,
                    ImagePath = _imagePath,
                    RecentImages = _viewModel.RecentImages.ToArray(),
                    YellowAlertThreshold = _viewModel.YellowAlertThreshold,
                    RedAlertThreshold = _viewModel.RedAlertThreshold,
                    MaxImageSizeBytes = _viewModel.MaxImageSizeBytes,
                    EnableAnimations = _enableAnimations,
                    TimerFont = "Arial",
                    MessageFont = "Arial"
                };
                await _fileService.SaveSettingsAsync(_settingsFile, settings);
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error saving settings: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error saving settings";
            }
        }

        private async void LoadSettings()
        {
            try
            {
                var settings = await _fileService.LoadSettingsAsync(_settingsFile) ?? new AppSettings();
                TimerFontSizeSlider.Value = settings.TimerFontSize;
                TimerColorComboBox.SelectedItem = TimerColorComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == settings.TimerColor) ?? TimerColorComboBox.Items[0];
                MessageFontSizeSlider.Value = settings.MessageFontSize;
                MessageColorComboBox.SelectedItem = MessageColorComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == settings.MessageColor) ?? MessageColorComboBox.Items[0];
                BackgroundColorPicker.SelectedItem = BackgroundColorPicker.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == settings.BackgroundColor) ?? BackgroundColorPicker.Items[0];
                _viewModel.PreviousBackgroundColor = settings.BackgroundColor ?? "Black";
                MonitorComboBox.SelectedIndex = settings.MonitorIndex >= 0 && settings.MonitorIndex < System.Windows.Forms.Screen.AllScreens.Length ? settings.MonitorIndex : (System.Windows.Forms.Screen.AllScreens.Length > 1 ? 1 : 0);
                TopmostToggle.IsChecked = settings.IsTopmost;
                _viewModel.YellowAlertThreshold = settings.YellowAlertThreshold;
                _viewModel.RedAlertThreshold = settings.RedAlertThreshold;
                _viewModel.MaxImageSizeBytes = settings.MaxImageSizeBytes;
                _enableAnimations = settings.EnableAnimations;
                if (settings.IsFullScreen) ToggleFullScreen();
                if (!string.IsNullOrEmpty(settings.ImagePath) && File.Exists(settings.ImagePath))
                {
                    _imagePath = settings.ImagePath;
                    ImagePreview1.Source = new BitmapImage(new Uri(_imagePath));
                    _viewModel.AddRecentImage(_imagePath);
                }
                if (settings.RecentImages != null)
                {
                    foreach (var path in settings.RecentImages)
                        _viewModel.AddRecentImage(path);
                    PopulateRecentImages();
                }
                UpdateTimerFont();
                UpdateMessageFont();
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error loading settings: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = "Error loading settings; using defaults";
                TimerFontSizeSlider.Value = 95;
                TimerColorComboBox.SelectedIndex = 0;
                MessageFontSizeSlider.Value = 40;
                MessageColorComboBox.SelectedIndex = 0;
                BackgroundColorPicker.SelectedIndex = 0;
                MonitorComboBox.SelectedIndex = System.Windows.Forms.Screen.AllScreens.Length > 1 ? 1 : 0;
                TopmostToggle.IsChecked = false;
                _imagePath = "";
                ImagePreview1.Source = null!;
                _viewModel.YellowAlertThreshold = 300;
                _viewModel.RedAlertThreshold = 60;
                _viewModel.MaxImageSizeBytes = 5 * 1024 * 1024;
                _enableAnimations = true;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create the settings window with the correct dependencies
                var settingsWindow = new SettingsWindow(_fileService, _messageFilePath, _viewModel)
                {
                    // Set the owner to this window to ensure proper modal behavior and center position
                    Owner = this
                };

                // Show as dialog to ensure it blocks interaction with main window
                settingsWindow.ShowDialog();

                // Reload messages after settings are closed
                LoadMessagesToComboBox();
                _viewModel.StatusMessage = "Settings closed";
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync($"Settings window error: {ex.Message}\n{ex.StackTrace}");
                _viewModel.StatusMessage = $"Error opening settings: {ex.Message}";
            }
        }

        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecentImagesComboBox.SelectedItem != null)
            {
                // Get the filename from the selected item
                string selectedImageName = RecentImagesComboBox.SelectedItem.ToString();
                
                // Find the full path in the RecentImages collection
                string fullPath = _viewModel.RecentImages.FirstOrDefault(p => Path.GetFileName(p) == selectedImageName);
                
                if (!string.IsNullOrEmpty(fullPath))
                {
                    // Remove the image from the recent images collection
                    _viewModel.RecentImages.Remove(fullPath);
                    
                    // If the deleted image was the current image, clear it
                    if (_imagePath == fullPath)
                    {
                        _imagePath = "";
                        ImagePreview1.Source = null;
                    }
                    
                    // Refresh the ComboBox
                    PopulateRecentImages();
                    
                    // Save changes to settings
                    SaveSettingsAsync();
                    
                    _viewModel.StatusMessage = $"Removed image: {selectedImageName}";
                }
            }
        }

        private async void AddMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Add Message", "Enter a new message:", "")
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                try
                {
                    var messages = await _fileService.LoadMessagesAsync(_messageFilePath);
                    var messagesList = messages.ToList();
                    messagesList.Add(dialog.ResponseText);
                    
                    await _fileService.SaveMessagesAsync(_messageFilePath, messagesList.ToArray());
                    LoadMessagesToComboBox();
                    
                    // Select the newly added message
                    for (int i = 0; i < PredefinedMessageComboBox.Items.Count; i++)
                    {
                        if (PredefinedMessageComboBox.Items[i] is ComboBoxItem item && 
                            item.Content.ToString() == dialog.ResponseText)
                        {
                            PredefinedMessageComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                    
                    _viewModel.StatusMessage = "Message added successfully";
                }
                catch (Exception ex)
                {
                    _ = _logger.LogErrorAsync($"Error adding message: {ex.Message}\n{ex.StackTrace}");
                    _viewModel.StatusMessage = "Error adding message";
                }
            }
        }

        private async void EditMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (PredefinedMessageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string currentMessage = selectedItem.Content.ToString();
                
                var dialog = new InputDialog("Edit Message", "Edit the selected message:", currentMessage)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    try
                    {
                        var messages = await _fileService.LoadMessagesAsync(_messageFilePath);
                        var messagesList = messages.ToList();
                        int index = messagesList.IndexOf(currentMessage);
                        
                        if (index >= 0)
                        {
                            messagesList[index] = dialog.ResponseText;
                            await _fileService.SaveMessagesAsync(_messageFilePath, messagesList.ToArray());
                            
                            int selectedIndex = PredefinedMessageComboBox.SelectedIndex;
                            LoadMessagesToComboBox();
                            
                            // Try to keep the selection
                            if (selectedIndex >= 0 && selectedIndex < PredefinedMessageComboBox.Items.Count)
                            {
                                PredefinedMessageComboBox.SelectedIndex = selectedIndex;
                            }
                            
                            _viewModel.StatusMessage = "Message updated successfully";
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = _logger.LogErrorAsync($"Error updating message: {ex.Message}\n{ex.StackTrace}");
                        _viewModel.StatusMessage = "Error updating message";
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a message to edit.", "No Message Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (PredefinedMessageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string messageToDelete = selectedItem.Content.ToString();
                
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete this message?\n\n\"{messageToDelete}\"", 
                    "Confirm Delete", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var messages = await _fileService.LoadMessagesAsync(_messageFilePath);
                        var messagesList = messages.ToList();
                        messagesList.Remove(messageToDelete);
                        
                        await _fileService.SaveMessagesAsync(_messageFilePath, messagesList.ToArray());
                        LoadMessagesToComboBox();
                        
                        _viewModel.StatusMessage = "Message deleted successfully";
                    }
                    catch (Exception ex)
                    {
                        _ = _logger.LogErrorAsync($"Error deleting message: {ex.Message}\n{ex.StackTrace}");
                        _viewModel.StatusMessage = "Error deleting message";
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a message to delete.", "No Message Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}