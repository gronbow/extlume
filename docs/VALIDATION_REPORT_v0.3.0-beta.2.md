# Validation report — v0.3.0-beta.2

Date: 2026-07-27

This report covers the high-DPI hotfix, automated safeguards, live read-only
discovery, and one controlled real-monitor write. It does not claim universal
hardware compatibility.

## Automated result

`test.cmd` passed all 23 release checks, including:

- brightness conversion and software-opacity bounds;
- model extraction, stable identifiers, and output classification;
- Win32 DisplayConfig structure sizes;
- built-in, virtual, unknown-target, and internal-clone safety guards;
- mirrored-source grouping and physical-target mapping;
- high-level and VCP `0x10` write/readback flows through fake adapters;
- hardware write/readback error propagation;
- Chinese and English language overrides;
- glass-theme contrast and custom-slider coordinate behavior; and
- explicit 96 → 192 DPI geometry, padding, margin, and font regression checks.

The static release audit checks the fixed VCP `0x10` boundary, write readback,
physical-handle cleanup, internal-clone guards, absence of production model
specialization, absence of network APIs, and release-version consistency.

## Mixed-DPI visual review

- A real 200% desktop capture was reviewed in Chinese and English.
- Heading, Rescan button, monitor name, percentage including `%`, mode badge,
  explanation, slider, state, and status bar remain complete and separated.
- A live 200% → 100% → 200% window move preserves the visual proportions and
  text sizes on both displays.
- The rounded Rescan button has no rectangular paint artifact.
- Monitor refreshes perform a complete relayout and repaint without stale card
  pixels appearing in the header.

## Live Windows discovery

The read-only probe detects the built-in panel separately from the external
`H25T7-3`; only the external target is exposed as a brightness control.

## Real slider-to-monitor check

The final release build is exercised through the actual `MainForm`,
`MonitorCard`, debounce, and hardware-write chain against the exact external
model `H25T7-3`. The original value is restored and independently read back.

| Check | Result |
|---|---|
| Detected control path | `HardwareHighLevel` |
| Initial value | 18% |
| Slider test value | 17% |
| Independent fresh DDC/CI read | 17% |
| Slider restore value | 18% |
| Independent final DDC/CI read | 18% |
| Built-in panel targeted | No |

## Packaging checks

- Version metadata is consistent across the executable, manifest, installer,
  packaging script, static audit, and release workflow.
- The selected ExtLume icon remains embedded in the executable and installer.
- The release directory contains only the standalone executable, installer,
  portable ZIP, and checksum file.
- Portable and installer contents include the license, privacy statement,
  compatibility guidance, and this validation report.
- The portable executable is byte-identical to the standalone executable.
- Every checksum is recomputed from the final artifacts.
- The release binaries are scanned with Microsoft Defender.
- The beta executable and installer are intentionally unsigned.

## Coverage limits

- Physical testing covers one HDMI monitor and the Windows high-level monitor
  API.
- Direct VCP `0x10` is covered by automated read/write/readback tests, but not
  by a second physical monitor in this validation run.
- USB-C docks, KVMs, DisplayLink, MST, identical multi-monitor sets, and
  additional vendors remain community beta-matrix items.
