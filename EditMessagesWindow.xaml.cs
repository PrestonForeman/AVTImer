﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PresenterTimerApp
{
    public partial class EditMessagesWindow : Window
    {
        private readonly string _messageFilePath;
        private readonly FileService _fileService;
        private readonly List<string> _currentMessages;

        public EditMessagesWindow(List<string> currentMessages, string messageFilePath, FileService fileService)
        {
            InitializeComponent();
            _messageFilePath = messageFilePath;
            _fileService = fileService;
            _currentMessages = currentMessages;
            MessagesListBox.ItemsSource = _currentMessages;
        }

        private void AddMessage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewMessageTextBox.Text))
            {
                _currentMessages.Add(NewMessageTextBox.Text);
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = _currentMessages;
                NewMessageTextBox.Text = "";
            }
        }

        private void RemoveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (MessagesListBox.SelectedItem != null)
            {
                _currentMessages.Remove((string)MessagesListBox.SelectedItem);
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = _currentMessages;
            }
        }

        private void SaveMessages_Click(object sender, RoutedEventArgs e)
        {
            _fileService.SaveMessagesAsync(_messageFilePath, _currentMessages.ToArray()).Wait();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}