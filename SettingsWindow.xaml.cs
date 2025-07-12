using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace PresenterTimerApp
{
    public partial class SettingsWindow : Window
    {
        private readonly FileService _fileService;
        private readonly string _messageFilePath;
        private readonly MainWindowViewModel _viewModel;
        private readonly string _dataFolder;

        public SettingsWindow(FileService fileService, string messageFilePath, MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _fileService = fileService;
            _messageFilePath = messageFilePath;
            _viewModel = viewModel;
            _dataFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "Data");
            LoadFonts();
            LoadMessages();
            LoadSettings();
        }

        private void LoadFonts()
        {
            foreach (var font in System.Windows.Media.Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            {
                TimerFontComboBox.Items.Add(font.Source);
                MessageFontComboBox.Items.Add(font.Source);
            }
            TimerFontComboBox.SelectedItem = _viewModel.TimerFont ?? "Arial";
            MessageFontComboBox.SelectedItem = _viewModel.MessageFont ?? "Arial";
        }

        private async void LoadMessages()
        {
            try
            {
                var messages = await _fileService.LoadMessagesAsync(_messageFilePath);
                MessagesListBox.ItemsSource = messages;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading messages: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            YellowAlertSlider.Value = _viewModel.YellowAlertThreshold;
            RedAlertSlider.Value = _viewModel.RedAlertThreshold;
            MaxImageSizeTextBox.Text = (_viewModel.MaxImageSizeBytes / 1024 / 1024).ToString();
            EnableAnimationsCheckBox.IsChecked = _viewModel.EnableAnimations;
            DefaultBackgroundColorPicker.SelectedItem = DefaultBackgroundColorPicker.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == _viewModel.PreviousBackgroundColor) ?? DefaultBackgroundColorPicker.Items[0];
        }

        private async void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.TimerFont = TimerFontComboBox.SelectedItem?.ToString() ?? "Arial";
            _viewModel.MessageFont = MessageFontComboBox.SelectedItem?.ToString() ?? "Arial";
            await SaveSettingsAsync();
        }

        private async void AddMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewMessageTextBox.Text))
            {
                var messages = MessagesListBox.ItemsSource.Cast<string>().ToList();
                messages.Add(NewMessageTextBox.Text);
                await _fileService.SaveMessagesAsync(_messageFilePath, messages.ToArray());
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
                NewMessageTextBox.Text = "";
            }
        }

        private async void EditMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessagesListBox.SelectedItem is string selected && !string.IsNullOrWhiteSpace(NewMessageTextBox.Text))
            {
                var messages = MessagesListBox.ItemsSource.Cast<string>().ToList();
                var index = messages.IndexOf(selected);
                messages[index] = NewMessageTextBox.Text;
                await _fileService.SaveMessagesAsync(_messageFilePath, messages.ToArray());
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
                NewMessageTextBox.Text = "";
            }
        }

        private async void DeleteMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessagesListBox.SelectedItem is string selected)
            {
                var messages = MessagesListBox.ItemsSource.Cast<string>().ToList();
                messages.Remove(selected);
                await _fileService.SaveMessagesAsync(_messageFilePath, messages.ToArray());
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
            }
        }

        private async void AlertSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _viewModel.YellowAlertThreshold = (int)YellowAlertSlider.Value;
            _viewModel.RedAlertThreshold = (int)RedAlertSlider.Value;
            await SaveSettingsAsync();
        }

        private async void DefaultBackgroundColorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.PreviousBackgroundColor = ((ComboBoxItem)DefaultBackgroundColorPicker.SelectedItem)?.Content.ToString() ?? "Black";
            await SaveSettingsAsync();
        }

        private async void MaxImageSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MaxImageSizeTextBox.Text, out int mb))
            {
                _viewModel.MaxImageSizeBytes = mb * 1024 * 1024;
                await SaveSettingsAsync();
            }
        }

        private async void EnableAnimationsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.EnableAnimations = true;
            await SaveSettingsAsync();
        }

        private async void EnableAnimationsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _viewModel.EnableAnimations = false;
            await SaveSettingsAsync();
        }

        private async void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to reset all settings?", "Confirm Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
            {
                _viewModel.TimerFont = "Arial";
                _viewModel.MessageFont = "Arial";
                _viewModel.YellowAlertThreshold = 300;
                _viewModel.RedAlertThreshold = 60;
                _viewModel.MaxImageSizeBytes = 5 * 1024 * 1024;
                _viewModel.EnableAnimations = true;
                _viewModel.PreviousBackgroundColor = "Black";
                _viewModel.ClearRecentImages();
                await _fileService.SaveMessagesAsync(_messageFilePath, new[] { "Please Wrap Up", "Time is up" });
                await SaveSettingsAsync();
                LoadFonts();
                LoadMessages();
                LoadSettings();
            }
        }

        private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON Files|*.json", DefaultExt = "json" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    await _fileService.SaveSettingsAsync(sfd.FileName, await _fileService.LoadSettingsAsync(System.IO.Path.Combine(_dataFolder, "settings.json")));
                    System.Windows.MessageBox.Show("Settings exported successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error exporting settings: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog { Filter = "JSON Files|*.json" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var settings = await _fileService.LoadSettingsAsync(ofd.FileName);
                    await _fileService.SaveSettingsAsync(System.IO.Path.Combine(_dataFolder, "settings.json"), settings);
                    LoadSettings();
                    System.Windows.MessageBox.Show("Settings imported successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error importing settings: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _fileService.BackupSettingsAsync(System.IO.Path.Combine(_dataFolder, "settings.json"), System.IO.Path.Combine(_dataFolder, "settings.bak"));
                var settings = new AppSettings
                {
                    TimerFont = _viewModel.TimerFont,
                    MessageFont = _viewModel.MessageFont,
                    YellowAlertThreshold = _viewModel.YellowAlertThreshold,
                    RedAlertThreshold = _viewModel.RedAlertThreshold,
                    MaxImageSizeBytes = _viewModel.MaxImageSizeBytes,
                    EnableAnimations = _viewModel.EnableAnimations,
                    PreviousBackgroundColor = _viewModel.PreviousBackgroundColor
                };
                await _fileService.SaveSettingsAsync(System.IO.Path.Combine(_dataFolder, "settings.json"), settings);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}