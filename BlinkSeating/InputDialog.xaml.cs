using System.Windows;

namespace BlinkSeating;

public partial class InputDialog : Window
{
    public string ResultText { get; private set; } = "";

    public InputDialog(string prompt, string title, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        ValueBox.Text = defaultValue;
        ValueBox.Focus();
        ValueBox.SelectAll();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ResultText = ValueBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    /// <summary>Convenience static: returns null if cancelled.</summary>
    public static string? Show(Window owner, string prompt, string title, string defaultValue = "")
    {
        var dlg = new InputDialog(prompt, title, defaultValue) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.ResultText : null;
    }
}
