# Contributing

Thank you for helping improve monitor compatibility.

## Development setup

- Windows 10 or Windows 11
- .NET Framework 4.8
- Either the Windows framework compiler used by `build.cmd`, or Visual Studio
  2022 with the .NET Framework 4.8 targeting pack
- Inno Setup 6 or 7 only when building the installer

Run before submitting a pull request:

```bat
test.cmd
probe.cmd
```

The probe is read-only. Never add a hardware-write test that changes a user's
monitor without a separate, explicit opt-in.

`scripts\build-release.ps1` creates the installer and portable package. The
release workflow pins and verifies Inno Setup 6.7.3; commercial users should
review the current Inno Setup licensing guidance for their own build context.

## Safety invariants

Changes must preserve all of the following:

- Never control a built-in or virtual display.
- Never software-dim or positionally map a source mirrored to a built-in panel.
- Never fall back to an arbitrary first monitor.
- Re-enumerate and match the requested display before a DDC/CI write.
- Release every physical-monitor handle.
- Keep low-level monitor control limited to brightness VCP `0x10`.
- Verify a hardware write with a readback.
- Keep software dimming click-through, capped below full black, and removable.
- Do not add telemetry, network access, or administrator requirements.
- Do not log raw device paths or serial numbers.

## Pull requests

Keep each pull request focused. Add or update tests for classification, raw
brightness ranges, cloned topology mapping, and fallback behavior as relevant.
Document user-visible changes in `CHANGELOG.md`.

Hardware compatibility reports are valuable even when no code change is
proposed; use the monitor compatibility issue template.
