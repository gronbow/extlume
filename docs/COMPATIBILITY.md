# Compatibility and troubleshooting

## Supported scenario

The app is intended for an active physical external display attached to a
Windows 10 or Windows 11 PC. It is especially useful when Windows exposes a
brightness slider only for the laptop panel and the external monitor's OSD is
unavailable or inconvenient.

“Supported” has two distinct meanings:

1. **Hardware brightness** is available when DDC/CI brightness commands pass
   through the complete connection.
2. **Software dimming** is available when Windows exposes a safe physical
   external display target with a desktop source that is not shared by the
   built-in panel, even if DDC/CI is unavailable.

This distinction is shown on every monitor card.

## Connection matrix

| Connection | Hardware DDC/CI expectation | Fallback |
|---|---|---|
| Direct HDMI or DisplayPort | Often supported | Software dimming |
| USB-C DisplayPort Alt Mode | Often supported; device-dependent | Software dimming |
| Thunderbolt/USB-C dock | Depends on dock firmware and topology | Software dimming |
| DisplayLink/indirect wired adapter | Driver-dependent | Software dimming |
| KVM or active adapter | Frequently blocks DDC/CI | Software dimming |
| VGA/DVI adapter chain | Highly variable | Software dimming |
| Miracast | No physical DDC/CI path | Software dimming when safely classified |
| Remote or virtual display | Deliberately excluded | None |

## How a monitor is identified

1. Windows DisplayConfig supplies each active source/target path.
2. The EDID-friendly monitor name is used when available.
3. WMI metadata and the monitor hardware model code are fallback sources.
4. Output technology marks embedded panels as internal.
5. Virtual targets are excluded.
6. DDC/CI handles are enumerated only for the exact Windows display source.

For a mirrored source, physical monitor descriptions are matched to EDID names.
When names are unavailable but counts agree, stable positional mapping is used.
An ambiguous unequal-count mapping is rejected instead of guessing.

In Duplicate mode with the laptop panel, hardware DDC/CI is attempted normally.
The physical handle must uniquely match the external EDID model; positional
mapping is not allowed. If hardware control is unavailable, software dimming is
deliberately blocked because a desktop overlay would also dim the built-in
panel. Switch Windows to Extend and rescan to enable the safe software fallback.

## Why hardware control may fail

- DDC/CI is disabled in the monitor OSD.
- The monitor does not implement brightness VCP `0x10`.
- A cable, adapter, dock, KVM, or MST hub does not pass DDC/CI.
- The GPU driver or vendor utility intercepts monitor control.
- The display reports invalid capability or range data.
- The display was disconnected, resumed, or moved to another port.
- A monitor firmware call stalls or times out.

The app first tries the Windows high-level brightness interface and then reads
VCP `0x10` directly. It never sends arbitrary VCP codes.

Microsoft notes that many monitors do not fully implement the MCCS/DDC/CI
standard, so low-level monitor behavior is hardware-dependent:
[SetVCPFeature documentation](https://learn.microsoft.com/windows/win32/api/lowlevelmonitorconfigurationapi/nf-lowlevelmonitorconfigurationapi-setvcpfeature).

## Troubleshooting

1. Make sure the monitor is active in Windows Display Settings.
2. Click **Rescan**.
3. If possible, enable DDC/CI in the monitor's own settings.
4. Try a direct cable instead of a dock, KVM, or adapter.
5. Update the GPU and dock drivers/firmware.
6. Exit other monitor-control utilities temporarily.
7. Run `probe.cmd` and attach its redacted output to a compatibility issue.

The probe performs no brightness writes.

## Software-dimming limitations

- It changes perceived image brightness, not panel backlight or power use.
- The minimum remains partially visible so the display is recoverable.
- It is blocked when an external display shares a duplicated desktop source
  with the built-in panel.
- External-only displays sharing one desktop source also share one overlay.
- HDR and color-managed content may look different under a black overlay.
- The overlay disappears when the app exits.
