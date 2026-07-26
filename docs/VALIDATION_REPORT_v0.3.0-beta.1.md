# Validation report — v0.3.0-beta.1

Date: 2026-07-27

This report separates automated coverage, visual review, live read-only
discovery, and a real slider-to-hardware write. It does not claim universal
hardware compatibility.

## Automated result

`test.cmd` passed all 22 checks on the release source:

- brightness conversion and software-opacity bounds;
- model extraction, stable identifiers, and output classification;
- Win32 DisplayConfig structure sizes;
- built-in, virtual, unknown-target, and internal-clone safety guards;
- mirrored-source grouping and physical-target mapping;
- high-level and VCP `0x10` write/readback flows through fake adapters;
- hardware write/readback error propagation;
- Chinese and English language overrides;
- glass-theme contrast thresholds and custom-slider coordinate behavior; and
- live read-only display discovery and monitor refresh.

The static release audit passed. It checks the fixed VCP `0x10` boundary,
write readback, physical-handle cleanup, internal-clone guards, absence of
production model specialization, absence of network APIs, and release-version
consistency.

## Visual and interaction review

- Chinese and English previews were reviewed at 100%, 150%, and 200% scaling.
- Header, rescan button, monitor names, percentages, mode badges, notes,
  sliders, state text, status bar, and scrollbars remained visible.
- The high-DPI review found and removed an absolute-positioning defect that
  initially hid the rescan button at 150% and 200%.
- Primary text, secondary text, and lime accent colors meet the automated
  contrast thresholds against the dark background.
- The custom slider exposes a slider accessibility role, visible focus, and
  mouse, wheel, arrow-key, Page Up/Page Down, Home, and End input.
- Unsupported DWM title-bar attributes fall back to the system title bar
  without forcing an unreadable text color.

## Live Windows discovery

The read-only probe completed with:

- 2 active physical display targets;
- 1 built-in panel, classified as internal;
- 1 `H25T7-3` over HDMI, classified as external; and
- 0 discovery warnings.

## Real slider-to-monitor check

The new custom slider was exercised through the actual `MainForm` and
`MonitorCard` event chain against the one exact external model `H25T7-3`:

| Check | Result |
|---|---|
| Detected control path | `HardwareHighLevel` |
| Initial value | 25% |
| Slider test value | 24% |
| Independent fresh DDC/CI read | 24% |
| Slider restore value | 25% |
| Independent final DDC/CI read | 25% |
| Built-in panel targeted | No |

This covered custom-slider mouse mapping, the 220 ms debounce, asynchronous
hardware write, write readback, UI success state, and fresh re-enumeration.
The monitor was left at the user's requested 25%.

## Packaging checks

- Release version metadata is consistent across the executable, manifest,
  installer, packaging script, static audit, and release workflow.
- The selected dark rounded-square icon with white monitor outline and lime
  brightness curve remains embedded in the executable; its extracted 32 px
  pixels match the previously verified selected icon.
- The controlled release directory contains only the standalone executable,
  installer, portable ZIP, and checksum file.
- Every checksum entry was recomputed successfully after the final package
  build, and the portable ZIP contains a byte-identical executable.
- README image assets are present in both the portable ZIP and installer so
  the bundled documentation renders offline.
- Startup-hidden and second-instance signaling behavior passed against the
  final standalone executable.
- Microsoft Defender completed a custom scan of the release directory with
  current local signatures and reported zero detections.
- The beta executable and installer are intentionally reported as unsigned.

## Beta coverage limits

- The physical write above covers one HDMI monitor and the Windows high-level
  monitor API.
- Direct VCP `0x10` is covered by automated read/write/readback tests, but not
  by a second physical monitor in this validation run.
- USB-C docks, KVMs, DisplayLink, MST, identical multi-monitor sets, and
  additional vendors remain community beta-matrix items.
- The beta binaries are not code-signed.
