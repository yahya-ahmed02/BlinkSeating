using System.Windows;
using BlinkSeating.Models;
using BlinkSeating.Services;

namespace BlinkSeating;

public partial class AutoSeatDialog : Window
{
    private readonly List<Section> _sections;
    public AutoSeatResult? Result { get; private set; }

    public AutoSeatDialog(List<Section> sections)
    {
        InitializeComponent();
        _sections = sections;
        foreach (var s in sections) ZoneBox.Items.Add(s.Name);
        if (ZoneBox.Items.Count > 0) ZoneBox.SelectedIndex = 0;
    }

    private void Run_Click(object sender, RoutedEventArgs e)
    {
        if (ZoneBox.SelectedIndex < 0)
        {
            MessageBox.Show("Pick a zone first.", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseSizes(TopSizesBox.Text, out var topSizes, out var topError))
        {
            MessageBox.Show($"Top-priority sizes: {topError}", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseSizes(OtherSizesBox.Text, out var otherSizes, out var otherError))
        {
            MessageBox.Show($"Other family sizes: {otherError}", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var section = _sections[ZoneBox.SelectedIndex];
        Result = AutoSeatService.Seat(section, topSizes, otherSizes);
        DialogResult = true;
    }

    private static bool TryParseSizes(string text, out List<int> sizes, out string error)
    {
        sizes = new List<int>();
        error = "";
        if (string.IsNullOrWhiteSpace(text)) return true; // empty is fine - means no families in that group

        foreach (var part in text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, out int value) || value <= 0)
            {
                error = $"'{part}' is not a valid positive number.";
                return false;
            }
            sizes.Add(value);
        }
        return true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
