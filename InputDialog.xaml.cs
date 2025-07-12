using System.Windows;

namespace PresenterTimerApp
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}