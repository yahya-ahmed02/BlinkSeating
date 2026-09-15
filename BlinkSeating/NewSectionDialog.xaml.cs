using System.Windows;

namespace BlinkSeating;

public partial class NewSectionDialog : Window
{
    public string SectionName { get; private set; } = "SECTION";
    public int RowCount { get; private set; }
    public int SeatsPerRow { get; private set; }

    public NewSectionDialog()
    {
        InitializeComponent();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a section name.", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RowsBox.Text.Trim(), out int rows) || rows < 1 || rows > 100)
        {
            MessageBox.Show("Rows must be a whole number between 1 and 100.", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(ColsBox.Text.Trim(), out int cols) || cols < 1 || cols > 100)
        {
            MessageBox.Show("Seats per row must be a whole number between 1 and 100.", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SectionName = name;
        RowCount = rows;
        SeatsPerRow = cols;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
