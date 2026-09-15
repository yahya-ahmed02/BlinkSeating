using System.Windows;
using System.Windows.Controls;
using BlinkSeating.Models;

namespace BlinkSeating;

public partial class SeatDialog : Window
{
    private readonly Seat _seat;
    public bool Cleared { get; private set; }
    public bool Deleted { get; private set; }

    public SeatDialog(Seat seat, string sectionName)
    {
        InitializeComponent();
        _seat = seat;
        SeatHeader.Text = $"{sectionName} - {seat.Row} - Seat {seat.Number}";
        GuestNameBox.Text = seat.GuestName ?? "";
        NotesBox.Text = seat.Notes ?? "";
        StatusBox.SelectedIndex = (int)seat.Status;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        bool hasName = !string.IsNullOrWhiteSpace(GuestNameBox.Text);
        bool hasSpecialStatus = StatusBox.SelectedIndex > 0; // index 0 = "None (regular seat)"

        if (!hasName && !hasSpecialStatus)
        {
            MessageBox.Show(
                "Enter a guest name to seat this seat, or pick a status (Reserved/Blind/Maybe/Damaged) to flag it without a guest.\n\nTo leave it as a plain empty seat, use \"Clear Seat\" or \"Cancel\" instead of Save.",
                "Name Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _seat.GuestName = hasName ? GuestNameBox.Text.Trim() : null;
        _seat.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        _seat.Status = (SeatStatus)(StatusBox.SelectedIndex < 0 ? 0 : StatusBox.SelectedIndex);
        _seat.IsAutoSeated = false; // hand-edited seats are never touched by the auto-seat engine
        DialogResult = true;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _seat.GuestName = null;
        _seat.Notes = null;
        _seat.Status = SeatStatus.Available;
        Cleared = true;
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var confirmMsg = _seat.IsAssigned
            ? $"Delete Seat {_seat.Number} in {_seat.Row}? It has a guest assigned - the seat and the assignment will both be removed."
            : $"Delete Seat {_seat.Number} in {_seat.Row}?";
        var result = MessageBox.Show(confirmMsg, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            Deleted = true;
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
