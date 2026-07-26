# Changelog

All notable changes are documented here.

## [Unreleased]

## [0.2.0-beta.1] - 2026-07-26

### Added

- Generic active external-display discovery using Windows DisplayConfig.
- EDID friendly model names with WMI and hardware-ID fallback.
- Built-in and virtual display exclusion.
- High-level Windows hardware brightness control with VCP `0x10` fallback.
- Verified writes, explicit physical-monitor matching, and handle cleanup.
- Software-dimming fallback that is labeled separately from hardware control.
- Multiple-display and mirrored-topology grouping.
- Display hot-plug and resume rescanning.
- Chinese and English UI, system tray, single instance, and per-user startup.
- Per-monitor DPI-aware layout and a multi-size application icon.
- Offline privacy design, automated tests, read-only probe, CI, and packaging.

### Safety

- Block software dimming when Duplicate mode shares one desktop source with
  the built-in panel.
- Require a unique model match for hardware control on an internal clone;
  positional mapping is disabled.
