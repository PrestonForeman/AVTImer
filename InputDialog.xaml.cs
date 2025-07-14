using System.Windows;

namespace PresenterTimerApp
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }
        public string WindowTitle { get; set; }
        public string Message { get; set; }
        public string DefaultValue { get; set; }

        public InputDialog(string title, string message, string defaultValue = "")
        {
            InitializeComponent();
            WindowTitle = title;
            Message = message;
            DefaultValue = defaultValue;
            DataContext = this;
            
            Loaded += (s, e) => 
            {
                InputTextBox.Text = DefaultValue;
                InputTextBox.SelectAll();
                InputTextBox.Focus();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
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