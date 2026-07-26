# Test plan

## Automated tests

`test.cmd` builds the release executable and runs a dependency-free test runner.

Coverage includes:

- percentage clamping;
- raw ranges with non-zero minimum and non-100 maximum;
- software opacity and recoverability;
- EDID model extraction and generic-name fallback;
- deterministic hashed identifiers;
- embedded/external/virtual output classification;
- safe treatment of unknown output technologies;
- Win32 structure sizes used by DisplayConfig;
- internal-display DDC exclusion;
- software fallback when no exact physical display exists;
- high-level and VCP read/write/readback flows through fake hardware adapters;
- hardware write and readback failure propagation;
- mirrored-source grouping;
- physical-description to EDID-target mapping;
- internal, virtual, and built-in-clone software-overlay guards;
- glass-theme primary, secondary, and accent contrast thresholds;
- custom-slider coordinate mapping, clamping, and non-user update behavior;
- live read-only DisplayConfig discovery; and
- live read-only monitor refresh.

No automated test writes a physical monitor brightness value.

## Visual tests

- 100%, 150%, and 200% Windows scale
- Chinese and English UI
- long monitor model names
- hardware and software method badges
- 0%, 25%, 50%, 70%, and 100% labels
- one, two, and four monitor cards
- empty state
- narrow minimum window size
- title-bar fallback when optional DWM attributes are unsupported
- dark scrollbar with multi-monitor overflow
- keyboard focus plus arrow, Page Up/Page Down, Home, and End input

## Hardware matrix

The beta should collect results for:

- direct HDMI;
- direct DisplayPort;
- USB-C DisplayPort Alt Mode;
- a USB-C/Thunderbolt dock;
- a DisplayLink adapter;
- duplicate and extend modes;
- Duplicate mode with the built-in panel, including the protected no-overlay
  state when DDC/CI is unavailable;
- sleep/resume;
- hot-unplug during a write;
- two different external models; and
- two identical external models.

For each display, record model, connection chain, detected method, read value,
requested value, verified value, and whether reconnect was successful. Do not
record EDID serial numbers.

## Release gates

- `test.cmd` returns zero.
- `probe.cmd` returns zero on a real Windows desktop.
- Release build emits no compiler warning.
- UI screenshot review shows no clipped text.
- Single-instance, tray hide/show, rescan, and exit work.
- Portable ZIP and installer scan clean with built-in Windows protections.
- SHA-256 checksums match.
- No secret or private device path is present in tracked files.
- GitHub Actions succeeds on a clean Windows runner.
- Beta limitations are visible in README and release notes.
