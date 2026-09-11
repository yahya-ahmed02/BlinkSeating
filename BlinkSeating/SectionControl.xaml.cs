using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlinkSeating.Models;

namespace BlinkSeating;

public partial class SectionControl : UserControl
{
    public Section Section { get; }

    public event Action<Seat, Section>? SeatClicked;
    public event Action<Section>? SectionDeleted;
    public event Action? LayoutChanged;

    private Point _dragStart;
    private bool _dragging;

    public SectionControl(Section section)
    {
        InitializeComponent();
        Section = section;
        SectionTitle.Text = section.Name;
        Render();
    }

    public void Render()
    {
        SectionTitle.Text = Section.Name;
        RowsHost.Items.Clear();

        // Rendered bottom-to-top: ROW1 (the front row) stays pinned at the bottom of the box,
        // and each new row added stacks above it, going further from the stage.
        foreach (var row in Enumerable.Reverse(Section.Rows))
        {
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };

            var rowLabel = new TextBlock
            {
                Text = row.Name,
                Width = 40,
                FontSize = 9,
                Foreground = Brushes.DarkSlateGray,
                VerticalAlignment = VerticalAlignment.Center
            };
            rowPanel.Children.Add(rowLabel);

            var addSeatBtn = new Button
            {
                Content = "+",
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 2, 0),
                FontSize = 9,
                Tag = row
            };
            addSeatBtn.Click += (_, _) =>
            {
                row.Seats.Add(new Seat { Row = row.Name });
                row.RenumberSeats();
                Render();
                LayoutChanged?.Invoke();
            };
            rowPanel.Children.Add(addSeatBtn);

            var removeSeatBtn = new Button
            {
                Content = "-",
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 2, 0),
                FontSize = 9
            };
            removeSeatBtn.Click += (_, _) =>
            {
                if (row.Seats.Count == 0) return;
                var last = row.Seats[^1];
                if (last.IsAssigned)
                {
                    var res = MessageBox.Show(
                        $"Seat {last.Number} in {row.Name} has {last.GuestName} assigned. Remove it anyway?",
                        "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res != MessageBoxResult.Yes) return;
                }
                row.Seats.RemoveAt(row.Seats.Count - 1);
                row.RenumberSeats();
                Render();
                LayoutChanged?.Invoke();
            };
            rowPanel.Children.Add(removeSeatBtn);

            var delRowBtn = new Button
            {
                Content = "-Row",
                Width = 34,
                Height = 18,
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 9
            };
            delRowBtn.Click += (_, _) =>
            {
                int assignedCount = row.Seats.Count(s => s.IsAssigned);
                string msg = assignedCount > 0
                    ? $"Delete {row.Name}? It has {assignedCount} seated guest(s) - they'll be removed too."
                    : $"Delete {row.Name}?";
                var res = MessageBox.Show(msg, "Confirm Delete Row", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    Section.Rows.Remove(row);
                    Render();
                    LayoutChanged?.Invoke();
                }
            };
            rowPanel.Children.Add(delRowBtn);

            RenderSeatsWithGroupBorders(row, rowPanel);

            RowsHost.Items.Add(rowPanel);
        }
    }

    private void RenderSeatsWithGroupBorders(SeatRow row, StackPanel rowPanel)
    {
        var buffer = new List<(Seat Seat, Button Btn)>();
        string? currentKey = null;

        void Flush()
        {
            if (buffer.Count == 0) return;

            if (currentKey == null || buffer.Count <= 1)
            {
                // Empty seats, or a lone guest - no grouping border needed.
                foreach (var (_, btn) in buffer) rowPanel.Children.Add(btn);
            }
            else
            {
                var groupPanel = new StackPanel { Orientation = Orientation.Horizontal };
                foreach (var (_, btn) in buffer) groupPanel.Children.Add(btn);

                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Padding = new Thickness(1),
                    Child = groupPanel
                };
                rowPanel.Children.Add(border);
            }

            buffer.Clear();
        }

        foreach (var seat in row.Seats)
        {
            var btn = CreateSeatButton(seat);
            var key = GroupKey(seat);

            // A single-person "family" doesn't need a border around just one seat.
            if (key != currentKey)
            {
                Flush();
                currentKey = key;
            }
            buffer.Add((seat, btn));
        }
        Flush();
    }

    private Button CreateSeatButton(Seat seat)
    {
        var btn = new Button
        {
            Content = seat.Label,
            Width = 22,
            Height = 22,
            Margin = new Thickness(1),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = SeatBrush(seat),
            Foreground = Brushes.White,
            ToolTip = seat.IsAssigned ? seat.GuestName : "(empty)",
            Tag = seat
        };
        btn.Click += (_, _) => SeatClicked?.Invoke(seat, Section);
        return btn;
    }

    /// <summary>Seats sharing this key are considered one group and get a border drawn around
    /// them (if the group has more than one member and they're contiguous). Auto-seated members
    /// share the label before " (" (e.g. all of "Top 3 (1/5)".."Top 3 (5/5)" -> "Top 3").
    /// Manually-seated seats group when the guest name matches exactly. Empty seats return null.</summary>
    private static string? GroupKey(Seat seat)
    {
        if (!seat.IsAssigned) return null;
        var name = seat.GuestName!;
        int idx = name.IndexOf(" (", StringComparison.Ordinal);
        return idx >= 0 ? name[..idx] : name;
    }

    private static Brush SeatBrush(Seat seat)
    {
        return seat.Status switch
        {
            SeatStatus.Available when seat.IsAssigned && seat.IsAutoSeated => Brushes.SteelBlue,   // auto-seated
            SeatStatus.Available when seat.IsAssigned => Brushes.SeaGreen,                          // manually seated
            SeatStatus.Available => new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)),           // red, empty
            SeatStatus.Reserved => Brushes.DarkOrange,
            SeatStatus.Maybe => new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D)),
            SeatStatus.Blind => Brushes.Black,
            _ => Brushes.Gray
        };
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        int rowCount = Section.Rows.Count + 1;
        Section.Rows.Add(new SeatRow { Name = $"ROW{rowCount}" });
        Render();
        LayoutChanged?.Invoke();
    }

    private void ClearSeating_Click(object sender, RoutedEventArgs e)
    {
        int assignedCount = Section.Rows.SelectMany(r => r.Seats).Count(s => s.IsAssigned);
        if (assignedCount == 0)
        {
            MessageBox.Show("No seated guests in this section.", "blink", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Clear all {assignedCount} seated guest(s) in '{Section.Name}'? Seats and rows stay - only the assignments are removed.",
            "Confirm Clear Seating", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            foreach (var row in Section.Rows)
            {
                foreach (var seat in row.Seats)
                {
                    seat.GuestName = null;
                    seat.Notes = null;
                    seat.Status = SeatStatus.Available;
                    seat.IsAutoSeated = false;
                }
            }
            Render();
            LayoutChanged?.Invoke();
        }
    }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var name = InputDialog.Show(owner, "Section name:", "Rename Section", Section.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            Section.Name = name;
            Render();
            LayoutChanged?.Invoke();
        }
    }

    private void DeleteSection_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show($"Delete section '{Section.Name}'?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            SectionDeleted?.Invoke(Section);
        }
    }

    // --- Dragging the section around the canvas ---
    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _dragStart = e.GetPosition(Parent as UIElement);
        CaptureMouse();
        MouseMove += SectionControl_MouseMove;
        MouseLeftButtonUp += SectionControl_MouseLeftButtonUp;
    }

    private void SectionControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || Parent is not Canvas canvas) return;

        var pos = e.GetPosition(canvas);
        double newX = Canvas.GetLeft(this) + (pos.X - _dragStart.X);
        double newY = Canvas.GetTop(this) + (pos.Y - _dragStart.Y);

        Canvas.SetLeft(this, Math.Max(0, newX));
        Canvas.SetTop(this, Math.Max(0, newY));
        _dragStart = pos;

        Section.X = Math.Max(0, newX);
        Section.Y = Math.Max(0, newY);
    }

    private void SectionControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        ReleaseMouseCapture();
        MouseMove -= SectionControl_MouseMove;
        MouseLeftButtonUp -= SectionControl_MouseLeftButtonUp;
        LayoutChanged?.Invoke();
    }
}
