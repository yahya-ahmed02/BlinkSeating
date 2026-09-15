using System.Text.RegularExpressions;
using BlinkSeating.Models;

namespace BlinkSeating.Services;

public class AutoSeatResult
{
    public int TotalRequested { get; set; }
    public int TotalSeated { get; set; }
    public int Unseated => TotalRequested - TotalSeated;
    public List<string> Warnings { get; } = new();
}

/// <summary>
/// Seats families into a section's rows.
/// Assumptions (documented so they're easy to challenge/adjust):
///  - Rows are treated front-to-back in the order they appear in Section.Rows (ROW1 = front).
///  - A "family" must sit in one contiguous block within a row whenever possible.
///  - Every family - top-priority first (in rank order), then others (in booking order) -
///    is placed in the EARLIEST (frontmost) row that has enough contiguous free space for it,
///    searching the whole zone from the front each time. This means a smaller family placed
///    later can legitimately backfill a gap a bigger, earlier family skipped over - that's
///    intentional: front seats shouldn't sit empty just because an earlier family didn't fit there.
///  - A family bigger than every row's free capacity is split: it fills whatever room is
///    available in the frontmost row(s) with space, continuing further back until fully seated.
///  - ADDITIVE ACROSS RUNS: running this again on the same section does NOT clear anything -
///    manually-seated seats and previously auto-seated seats are both left alone and treated as
///    occupied. Only the newly given family sizes get placed, into whatever's still free. Label
///    numbering (Top N / Guest N) continues on from whatever's already in the section, so it
///    won't collide with an earlier run's labels. If you want a totally fresh start, use the
///    section's "Clear" button first to unseat everyone, then run Auto-Seat again.
/// </summary>
public static class AutoSeatService
{
    public static AutoSeatResult Seat(Section section, List<int> topFamilySizes, List<int> otherFamilySizes)
    {
        var result = new AutoSeatResult();
        var rows = section.Rows;

        int topStart = NextLabelIndex(section, "Top");
        for (int i = 0; i < topFamilySizes.Count; i++)
        {
            int size = topFamilySizes[i];
            if (size <= 0) continue;
            result.TotalRequested += size;
            PlaceFamily(rows, size, $"Top {topStart + i}", result);
        }

        int otherStart = NextLabelIndex(section, "Guest");
        for (int i = 0; i < otherFamilySizes.Count; i++)
        {
            int size = otherFamilySizes[i];
            if (size <= 0) continue;
            result.TotalRequested += size;
            PlaceFamily(rows, size, $"Guest {otherStart + i}", result);
        }

        return result;
    }

    /// <summary>Finds the highest existing "{prefix} N" label already in the section and
    /// returns N+1, so a second (or third...) run numbers new families past whatever's there
    /// already instead of restarting at 1 and colliding with earlier labels.</summary>
    private static int NextLabelIndex(Section section, string prefix)
    {
        int max = 0;
        var pattern = new Regex($"^{Regex.Escape(prefix)} (\\d+)");
        foreach (var seat in section.Rows.SelectMany(r => r.Seats))
        {
            if (!seat.IsAssigned) continue;
            var match = pattern.Match(seat.GuestName!);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
            {
                max = Math.Max(max, idx);
            }
        }
        return max + 1;
    }

    /// <summary>Searches every row front-to-back for the first one with enough contiguous
    /// free space. Falls back to splitting across rows (front-to-back) if no single row fits.</summary>
    private static void PlaceFamily(List<SeatRow> rows, int size, string label, AutoSeatResult result)
    {
        if (rows.Count == 0)
        {
            result.Warnings.Add($"{label}: no rows exist in this zone.");
            return;
        }

        foreach (var row in rows)
        {
            var run = FindRun(row, size);
            if (run != null)
            {
                AssignRun(run, label);
                result.TotalSeated += size;
                return;
            }
        }

        // No single row had room for the whole family - split, filling whatever space
        // exists, row by row, front-to-back.
        int remaining = size;
        foreach (var row in rows)
        {
            if (remaining <= 0) break;
            var run = FindLargestRun(row);
            if (run.Count == 0) continue;

            int take = Math.Min(run.Count, remaining);
            AssignRun(run.Take(take).ToList(), label);
            result.TotalSeated += take;
            remaining -= take;
        }

        if (remaining > 0)
        {
            result.Warnings.Add($"{label}: {remaining} of {size} people could not be seated - zone is full.");
        }
    }

    private static List<Seat>? FindRun(SeatRow row, int size)
    {
        foreach (var run in GetFreeRuns(row))
        {
            if (run.Count >= size) return run.Take(size).ToList();
        }
        return null;
    }

    private static List<Seat> FindLargestRun(SeatRow row)
    {
        return GetFreeRuns(row).OrderByDescending(r => r.Count).FirstOrDefault() ?? new List<Seat>();
    }

    private static List<List<Seat>> GetFreeRuns(SeatRow row)
    {
        var runs = new List<List<Seat>>();
        var current = new List<Seat>();

        foreach (var seat in row.Seats)
        {
            bool free = seat.Status == SeatStatus.Available && !seat.IsAssigned;
            if (free)
            {
                current.Add(seat);
            }
            else if (current.Count > 0)
            {
                runs.Add(current);
                current = new List<Seat>();
            }
        }
        if (current.Count > 0) runs.Add(current);
        return runs;
    }

    private static void AssignRun(List<Seat> seats, string label)
    {
        for (int i = 0; i < seats.Count; i++)
        {
            seats[i].GuestName = seats.Count == 1 ? label : $"{label} ({i + 1}/{seats.Count})";
            seats[i].IsAutoSeated = true;
        }
    }
}
