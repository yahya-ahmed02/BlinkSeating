# blink Seating System

A WPF desktop app for designing a custom venue seating layout and assigning guests to seats.

## What it does right now (v0.1 — functional MVP)

- Add sections anywhere on the canvas (drag by the blue header bar to reposition)
- Add rows to a section, add/remove seats at the end of a row (the "+" / "-" buttons next to each row)
- Delete a whole row with "-Row" (asks for confirmation if it has seated guests)
- Click any seat to open a dialog: assign a guest name, set status (Available / Reserved / Maybe / Blind), add notes, **Clear Seat** (unassign but keep the seat), or **Delete Seat** (remove the seat entirely)
- "Clear" on a section's header bulk-unseats everyone in that section without touching the seats/rows themselves
- Each row can be dragged left or right independently (grab any empty space on the row, not just a button) within its section, so you can stagger rows to match a real venue shape
- Seated groups (families placed by auto-seat, or manually-seated seats sharing an identical guest name) are drawn with a thin border around the whole block, so you can see family boundaries at a glance
- Seats color-code automatically: red = empty, green = manually seated, steel blue = auto-seated, orange = reserved, gray = maybe, black = blind/damaged
- Save the whole layout to a `.json` file, load it back later
- Live counter bar at the bottom (total / assigned / available / reserved / maybe / blind)
- Auto-Seat Zone: assigns families automatically per the rules described below

## Auto-Seat (new)

Click **Auto-Seat Zone** in the toolbar. It asks for:
- Which zone (section)
- Top-priority family sizes, comma-separated, in rank order (e.g. `2,3,3,4,5,2,3,1,2,8`)
- Other family sizes, comma-separated, in booking order (e.g. `3,4,2,1,7,4,9,2,5,7`)

Then it seats them automatically:
- Every family - top-priority ones first (rank order), then others (booking order) - is placed
  in the frontmost row that has enough contiguous space for it, checking the whole zone from
  the front each time. So if a bigger family skips over a row because it doesn't fit, a smaller
  family placed later can legitimately fill that gap instead of leaving it empty.
- No family is split across rows unless nothing single row has enough room for it - then it
  fills whatever space is available, front-to-back, until fully seated
- Seats you assigned by hand (via the seat dialog) are never touched or overwritten by
  auto-seat, even if you run it again
- **Running Auto-Seat again on the same zone is additive** - it does NOT clear or redo anyone
  already seated (manually or by a previous auto-seat run). It only seats the new family sizes
  you enter, into whatever's still free, and its label numbering (Top N / Guest N) continues on
  from what's already there so it won't collide with earlier labels. If you actually want a
  clean slate, hit **Clear** on the zone first, then run Auto-Seat.
- Auto-seated guests show up **steel blue**; hand-seated guests show up **green** - so you can
  tell at a glance which is which
- If the zone runs out of room, you'll get a summary telling you exactly how many people from
  which family couldn't be seated, so you can move them manually or add another zone

**Known simplification:** when a family gets split across rows, the overflow isn't guaranteed
to sit in the exact same seat columns as the row above - it just takes the largest free block
in the next row with space. Visually "directly behind" in the strict column-aligned sense isn't
enforced yet. Tell me if that matters for your actual layout and I'll tighten it.

## What it does NOT do yet (be aware)

- No curved/custom-shaped sections (yours are all rectangular boxes right now — matching the exact
  fan-shaped "C.LEFT / C.MID / C.RIGHT" curve from your reference image would need custom path
  geometry, which I left out of this first pass so you'd have something working fast)
- No guest list import (e.g. from Excel/CSV) — you type names in one at a time per seat
- No print/PDF export
- No undo

Tell me which of these you actually need and I'll build it next — no point building everything
before you've used the basic version.

## Requirements to build it

You need **Windows** and the **.NET 8 SDK** (with the "Desktop development" workload — this is a
WPF app, it will not build or run on Linux/Mac).

Download the SDK: https://dotnet.microsoft.com/download/dotnet/8.0

## Run it during development

```
cd BlinkSeating
dotnet run
```

## Build it as a single .exe

This is the part you specifically asked for. Run this from inside the `BlinkSeating` folder:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The `.exe` will show up at:

```
bin\Release\net8.0-windows\win-x64\publish\BlinkSeating.exe
```

That file is standalone — you can copy it to any Windows machine and double-click it, no .NET
install required on the target machine (it's bundled in). It'll be somewhere around 60-150MB
because the whole runtime is embedded; that's normal for self-contained single-file publishing.

If you'd rather have a small exe and don't mind requiring .NET 8 to be installed on the machine
that runs it, drop `--self-contained true` and the two `-p:` flags — you'll get a tiny exe instead.
