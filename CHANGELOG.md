# Changelog

All notable changes are documented here.

## [Unreleased]

## [0.3.0-beta.2] - 2026-07-27

### Fixed

- Fixed clipped and overlapping text, badges, percentages, and controls on
  displays using 125%–200% Windows scaling.
- Fixed the missing percent sign and truncated Rescan button at 200% scaling.
- Fixed per-monitor resizing when moving the window between displays with
  different DPI settings.
- Removed the rectangular paint artifact around the rounded Rescan button and
  forced a clean relayout after display refreshes.

### Changed

- Layout dimensions, custom glass geometry, slider metrics, and dynamically
  created monitor cards now follow the active window DPI explicitly.
- Added deterministic DPI-scaling regression coverage and real-screen
  validation tooling for mixed-DPI systems.

### Safety

- Display discovery, DDC/CI targeting, verified write/readback, built-in-panel
  exclusion, clone guards, and software dimming are unchanged.

## [0.3.0-beta.1] - 2026-07-27

### Added

- iOS-inspired glass surfaces, graphite depth, and lime accent styling aligned
  with the ExtLume icon.
- A custom brightness slider with mouse drag, wheel, arrow keys, Home/End, and
  Page Up/Page Down input.
- Slider accessibility metadata and explicit keyboard guidance.
- Automated color-contrast and slider-coordinate tests.
- Reproducible UI preview and real slider-to-hardware validation tools.

### Changed

- Reworked the window, header, monitor cards, badges, status bar, and empty
  states for clearer visual hierarchy.
- Added dark-scrollbar styling and safe Windows 10/11 title-bar backdrop,
  dark-mode, and rounded-corner requests with graceful fallback.
- Increased layout resilience at 100%, 150%, and 200% display scaling.

### Safety

- DDC/CI targeting, readback verification, built-in-display exclusion, clone
  guards, and software-dimming boundaries are unchanged.

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
