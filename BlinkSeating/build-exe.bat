@echo off
echo Building blink Seating System as a standalone .exe...
echo This may take a minute or two the first time.
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed - scroll up for the error.
    pause
    exit /b 1
)

echo.
echo Done! Your exe is at:
echo   bin\Release\net8.0-windows\win-x64\publish\BlinkSeating.exe
echo.
echo Tip: right-click that file, choose "Send to > Desktop (create shortcut)"
echo so you can launch it without opening a terminal again.
pause
