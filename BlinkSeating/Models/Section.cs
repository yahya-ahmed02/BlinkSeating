namespace BlinkSeating.Models;

public class SeatRow
{
    public string Name { get; set; } = "ROW1";
    public List<Seat> Seats { get; set; } = new();

    /// <summary>Resets seat numbers to 1..N based on current left-to-right order.
    /// Call this after any seat is added or removed so numbering never has gaps.</summary>
    public void RenumberSeats()
    {
        for (int i = 0; i < Seats.Count; i++)
        {
            Seats[i].Number = i + 1;
        }
    }
}

public class Section
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "NEW SECTION";
    public double X { get; set; } = 40;
    public double Y { get; set; } = 40;
    public string ColorHex { get; set; } = "#C0392B"; // default seat red
    public List<SeatRow> Rows { get; set; } = new();
}

public class VenueLayout
{
    public string EventName { get; set; } = "blink";
    public List<Section> Sections { get; set; } = new();
}
