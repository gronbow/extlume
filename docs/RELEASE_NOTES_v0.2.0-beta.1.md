# ExtLume v0.2.0-beta.1

This is the first public beta.

## Highlights

- Automatically identifies active external monitor models.
- Provides per-monitor brightness sliders.
- Uses real DDC/CI hardware brightness when available.
- Falls back to clearly labeled software dimming when DDC/CI is unavailable
  and the external desktop area is safely isolated.
- Excludes built-in panels and virtual displays.
- Supports multiple displays, mirrored groups, hot-plugging, and resume.
- Includes Chinese and English UI, system tray, and per-user startup.
- Runs offline, without telemetry, network access, or administrator rights.

## Important compatibility note

Hardware brightness depends on the monitor and the full connection chain passing
DDC/CI. Docks, KVMs, adapters, drivers, or monitor firmware may block it.
Software dimming changes the image only and does not reduce backlight power.
It is blocked when Duplicate mode shares one desktop source with the built-in
panel; switch to Extend in that case.

Read the compatibility guide before filing a report.

## Assets

- `ExtLume-0.2.0-beta.1-Setup.exe`
- `ExtLume-0.2.0-beta.1-portable.zip`
- `ExtLume.exe`
- `SHA256SUMS.txt`

The beta binaries are not code-signed. Windows SmartScreen may therefore show a
reputation warning. Verify the SHA-256 checksum and download only from the
project's GitHub Releases page.
