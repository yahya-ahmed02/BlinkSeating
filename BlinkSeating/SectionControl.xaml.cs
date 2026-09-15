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
        RowsHost.Children.Clear();

        // Rendered bottom-to-top: ROW1 (the front row) sits at the bottom of the stack by
        // default, and each new row added stacks above it, going further from the stage.
        // Each row can then be dragged left/right from that default position.
        var displayRows = Enumerable.Reverse(Section.Rows).ToList();
        const double gap = 4;

        // Build and measure every row first, since a row with a family-group border is a few
        // pixels taller than a plain row - stacking by measured height (not a fixed guess)
        // is what stops adjacent rows' borders from overlapping each other.
        var built = new List<(Border Border, SeatRow Row, double Width, double Height)>();
        foreach (var row in displayRows)
        {
            var rowPanel = BuildRowPanel(row);
            var rowBorder = new Border { Child = rowPanel, Background = Brushes.Transparent, Cursor = Cursors.SizeWE };
            rowBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            built.Add((rowBorder, row, rowBorder.DesiredSize.Width, rowBorder.DesiredSize.Height));
        }

        double cumulativeY = 0;
        double maxRight = 0;

        foreach (var (rowBorder, row, width, height) in built)
        {
            double left = row.OffsetX;
            double top = cumulativeY;
            Canvas.SetLeft(rowBorder, left);
            Canvas.SetTop(rowBorder, top);
            RowsHost.Children.Add(rowBorder);

            AttachRowDrag(rowBorder, row);

            maxRight = Math.Max(maxRight, left + width);
            cumulativeY += height + gap;
        }

        RowsHost.Width = Math.Max(200, maxRight + 10);
        RowsHost.Height = Math.Max(20, cumulativeY);

        // Force the whole control (and its parent chain) to re-measure now, rather than
        // waiting for WPF's normal invalidation - otherwise a shrink can be left stuck at
        // the previous, larger size until something else happens to trigger a relayout.
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();
    }

    /// <summary>Lets the user drag a row's Border left/right on the section's row canvas. The
    /// row's OffsetX stores its horizontal position, so this survives save/load and doesn't
    /// disturb the front-to-back seating order. Dragging isn't clamped at the section's left
    /// edge - if a row goes further left than every other row, all rows are renormalized on
    /// release so nothing sits at a negative position, while keeping their relative spacing.</summary>
    private void AttachRowDrag(Border rowBorder, SeatRow row)
    {
        bool dragging = false;
        Point dragStart = default;

        rowBorder.MouseLeftButtonDown += (_, e) =>
        {
            dragging = true;
            dragStart = e.GetPosition(RowsHost);
            rowBorder.CaptureMouse();
        };

        rowBorder.MouseMove += (_, e) =>
        {
            if (!dragging) return;
            var pos = e.GetPosition(RowsHost);
            double newLeft = Canvas.GetLeft(rowBorder) + (pos.X - dragStart.X);
            Canvas.SetLeft(rowBorder, newLeft);
            row.OffsetX = newLeft;
            dragStart = pos;
        };

        rowBorder.MouseLeftButtonUp += (_, _) =>
        {
            dragging = false;
            rowBorder.ReleaseMouseCapture();

            // Always re-anchor so the current leftmost row sits at exactly 0. This handles both
            // directions symmetrically: dragging further left shifts everyone else right to
            // compensate, and dragging that row back right recompacts the whole group again -
            // otherwise a left-shift from an earlier drag would stick around permanently.
            double minX = Section.Rows.Count > 0 ? Section.Rows.Min(r => r.OffsetX) : 0;
            if (minX != 0)
            {
                foreach (var r in Section.Rows) r.OffsetX -= minX;
            }

            Render(); // resize the canvas to fit the new arrangement
            LayoutChanged?.Invoke();
        };
    }

    /// <summary>Builds one row's visual content: the row label, +/-/-Row controls, then the seats.</summary>
    private StackPanel BuildRowPanel(SeatRow row)
    {
        var rowPanel = new StackPanel { Orientation = Orientation.Horizontal };

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

        return rowPanel;
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
