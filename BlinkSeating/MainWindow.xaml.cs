using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using BlinkSeating.Models;
using BlinkSeating.Services;

namespace BlinkSeating;

public partial class MainWindow : Window
{
    private VenueLayout _layout = new();
    private string? _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        RebuildCanvas();
    }

    private void AddSection_Click(object sender, RoutedEventArgs e)
    {
        var name = InputDialog.Show(this, "Section name:", "New Section", "SECTION");
        if (string.IsNullOrWhiteSpace(name)) return;

        var section = new Section
        {
            Name = name,
            X = 40 + (_layout.Sections.Count % 4) * 260,
            Y = 40 + (_layout.Sections.Count / 4) * 300
        };
        // start with one row of 10 seats so it's not empty
        section.Rows.Add(new SeatRow
        {
            Name = "ROW1",
            Seats = Enumerable.Range(1, 10).Select(n => new Seat { Row = "ROW1", Number = n }).ToList()
        });

        _layout.Sections.Add(section);
        AddSectionControl(section);
        UpdateStatus();
    }

    private void AddSectionControl(Section section)
    {
        var control = new SectionControl(section);
        Canvas.SetLeft(control, section.X);
        Canvas.SetTop(control, section.Y);

        control.SeatClicked += OnSeatClicked;
        control.SectionDeleted += OnSectionDeleted;
        control.LayoutChanged += UpdateStatus;

        MainCanvas.Children.Add(control);
    }

    private void OnSeatClicked(Seat seat, Section section)
    {
        var dlg = new SeatDialog(seat, section.Name) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            if (dlg.Deleted)
            {
                var row = section.Rows.FirstOrDefault(r => r.Seats.Contains(seat));
                if (row != null)
                {
                    row.Seats.Remove(seat);
                    row.RenumberSeats();
                }
            }
            RebuildCanvas(); // simplest way to refresh seat colors everywhere
        }
    }

    private void OnSectionDeleted(Section section)
    {
        _layout.Sections.Remove(section);
        RebuildCanvas();
    }

    private void RebuildCanvas()
    {
        MainCanvas.Children.Clear();
        foreach (var section in _layout.Sections)
        {
            AddSectionControl(section);
        }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var allSeats = _layout.Sections.SelectMany(s => s.Rows).SelectMany(r => r.Seats).ToList();
        int total = allSeats.Count;
        int assigned = allSeats.Count(s => s.IsAssigned);
        int available = allSeats.Count(s => s.Status == SeatStatus.Available && !s.IsAssigned);
        int reserved = allSeats.Count(s => s.Status == SeatStatus.Reserved);
        int maybe = allSeats.Count(s => s.Status == SeatStatus.Maybe);
        int blind = allSeats.Count(s => s.Status == SeatStatus.Blind);

        StatusText.Text = $"Total: {total}   Assigned: {assigned}   Available: {available}   Reserved: {reserved}   Maybe: {maybe}   Blind/Damaged: {blind}";
    }

    private void AutoSeat_Click(object sender, RoutedEventArgs e)
    {
        if (_layout.Sections.Count == 0)
        {
            MessageBox.Show("Add a zone (section) first.", "blink", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new AutoSeatDialog(_layout.Sections) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            RebuildCanvas();
            var r = dlg.Result;
            var message = $"Seated {r.TotalSeated} of {r.TotalRequested} people.";
            if (r.Warnings.Count > 0)
            {
                message += "\n\n" + string.Join("\n", r.Warnings);
            }
            MessageBox.Show(message, "Auto-Seat Result", MessageBoxButton.OK,
                r.Warnings.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "blink layout (*.json)|*.json",
            FileName = _currentFilePath ?? "blink-layout.json"
        };
        if (dialog.ShowDialog() == true)
        {
            LayoutService.Save(_layout, dialog.FileName);
            _currentFilePath = dialog.FileName;
            MessageBox.Show("Layout saved.", "blink", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "blink layout (*.json)|*.json"
        };
        if (dialog.ShowDialog() == true)
        {
            _layout = LayoutService.Load(dialog.FileName);
            _currentFilePath = dialog.FileName;
            RebuildCanvas();
        }
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Start a new blank layout? Unsaved changes will be lost.",
            "New Layout", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _layout = new VenueLayout();
            _currentFilePath = null;
            RebuildCanvas();
        }
    }
}
