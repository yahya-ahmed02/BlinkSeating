namespace BlinkSeating.Models;

public enum SeatStatus
{
    Available,
    Reserved,
    Blind,       // matches "Blind/Damaged" in the reference chart
    Maybe
}

public class Seat
{
    public string Row { get; set; } = "";
    public int Number { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public string? GuestName { get; set; }
    public string? Notes { get; set; }
    public bool IsAutoSeated { get; set; } // true if placed by the auto-seat engine, false if seated by hand

    public string Label => Number.ToString();

    public bool IsAssigned => !string.IsNullOrWhiteSpace(GuestName);
}
