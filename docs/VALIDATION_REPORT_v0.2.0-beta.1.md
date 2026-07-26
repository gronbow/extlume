# Validation report — v0.2.0-beta.1

Date: 2026-07-26

This report separates automated coverage, live read-only discovery, and a real
hardware write. It does not claim universal hardware compatibility.

## Automated result

`test.cmd` passed all 20 checks on the release source:

- brightness range conversion and software-opacity bounds;
- model-name extraction, stable identifiers, and output classification;
- Win32 DisplayConfig structure sizes;
- built-in, virtual, unknown-target, and internal-clone safety guards;
- mirrored-source grouping and physical-target mapping;
- high-level and VCP `0x10` read/write/readback flows through fake adapters;
- hardware write/readback error propagation;
- language overrides; and
- live read-only display discovery and monitor refresh.

The static release audit also passed. It verifies the fixed VCP `0x10` boundary,
write readback, physical-handle cleanup, internal-clone overlay guard, absence
of production `H25T7` specialization, absence of network APIs, and release
version consistency.

## Live Windows discovery

The read-only probe completed with:

- 2 active physical display targets;
- 1 built-in panel, classified as internal;
- 1 `H25T7-3` over HDMI, classified as external; and
- 0 discovery warnings.

The probe does not write brightness.

## Real hardware write

The generic release source—not the earlier monitor-specific prototype—was used
to target the one exact external model `H25T7-3`.

| Check | Result |
|---|---|
| Detected control path | `HardwareHighLevel` |
| Value before write | 15% |
| Requested value | 25% |
| Immediate write readback | 25% |
| Fresh re-enumeration/read | 25% |
| Native error | 0 |
| Built-in panel targeted | No |

The physical handle was resolved from the external target's exact GDI source,
re-enumerated before the write, and released afterward.

## Real slider-to-monitor check

The release executable was then opened normally and its actual WinForms
brightness slider was operated with mouse input:

- a slider-page click changed the monitor from 25% to 15%;
- an independent fresh DDC/CI enumeration read back 15%;
- a later slider-page click restored the monitor from 35% to 25%; and
- an independent fresh DDC/CI enumeration read back the final 25%.

This check exercised the user-facing slider, debounce timer, asynchronous
hardware write, write readback, and subsequent fresh hardware discovery. The
test monitor was left at the user's requested 25%.

## UI and packaging checks

- Real Chinese and English windows were reviewed at 200% Windows scaling.
- Empty-state text, model labels, percentages, and hardware/software notes were
  visible without clipping.
- Single-instance signaling, startup-hidden behavior, tray behavior, silent
  per-user install, launch, and uninstall were exercised locally.
- The final installer, portable ZIP, standalone executable, and
  `SHA256SUMS.txt` were rebuilt after the last source change.
- Every checksum entry was recomputed successfully, and the executable inside
  the portable ZIP matched the standalone release executable.
- The final installer produced a byte-identical installed executable, the
  startup-hidden and second-instance behaviors passed, and silent uninstall
  removed the application and uninstall registration.
- The renamed `ExtLume.exe` exposed `ExtLume` as its process name, window
  title, product name, and file description while still showing `H25T7-3` and
  the live 25% hardware value.
- Installing with the desktop-icon task created `ExtLume.lnk`, targeting the
  installed `ExtLume.exe` and inheriting its embedded application icon.
  Uninstall removed both the shortcut and executable.
- The controlled release directory contained only the four expected ExtLume
  assets; stale pre-rename artifacts were excluded.
- A Windows Defender custom scan of the final release directory completed with
  current local signatures and reported zero detections.

## Beta coverage limits

- The real hardware write above covers one HDMI monitor model and the Windows
  high-level monitor API.
- The direct VCP `0x10` fallback is covered by automated read/write/readback
  tests, but not by a second physical monitor in this validation run.
- USB-C docks, KVMs, DisplayLink, MST, two identical external monitors, and
  additional vendors remain community beta-matrix items.
- The beta binaries are not code-signed.
