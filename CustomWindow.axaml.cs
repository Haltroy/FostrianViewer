using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FostrianViewer
{
    public partial class CustomWindow : Window
    {
        public CustomWindow()
        {
            InitializeComponent();
            StartByte.Maximum = long.MaxValue;
            EndByte.Maximum = long.MaxValue;
        }

        private void OKClicked(object? s, RoutedEventArgs e) => Close(new long[] { Start, End });

        private void CancelClicked(object? s, RoutedEventArgs e) => Close();

        public long Start => (long)(StartByte.Value ?? 0);
        public long End => (long)(EndByte.Value ?? 0);
    }
}