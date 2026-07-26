# Architecture and safety model

## Runtime flow

```text
Windows DisplayConfig
        |
        v
active targets + EDID names + output technology
        |
        +--> reject built-in and virtual targets
        |
        v
group by Windows source (extended or mirrored topology)
        |
        v
enumerate exact HMONITOR / physical monitor handles
        |
        +--> high-level brightness API
        |          |
        |          +--> VCP 0x10 fallback
        |
        +--> safe software-dimming fallback
                   |
                   +--> block if source is mirrored to built-in panel
```

## Display discovery

`DisplayDiscoveryService` calls:

- `GetDisplayConfigBufferSizes`
- `QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS)`
- `DisplayConfigGetDeviceInfo`

It obtains the GDI source name, EDID-friendly target name, monitor device path,
output technology, and active screen bounds. WMI is a metadata fallback, not
the primary topology source.

Only hashed device identities leave the discovery layer for local persistence.

## Classification

Embedded DisplayPort, embedded UDI, LVDS, and Windows internal-panel targets are
classified as built-in. Indirect virtual targets are rejected. Known physical
connector technologies are external.

An unknown connector is promoted to external only when the same active topology
also contains a known internal display and the unknown target has real bounds.
A lone unknown target remains unclassified to avoid darkening a laptop panel.

## Hardware brightness

`DdcBrightnessService` resolves the requested GDI display name to one HMONITOR,
then obtains physical monitor handles through `Dxva2.dll`.

Probe order:

1. `GetMonitorCapabilities` + `GetMonitorBrightness`
2. `GetVCPFeatureAndVCPFeatureReply(0x10)`
3. software fallback

Write order:

1. re-enumerate the exact logical source;
2. verify physical index and description;
3. read the current min/max range;
4. convert 0–100 UI percent into the monitor's raw range;
5. set hardware brightness;
6. read back and return the verified percentage;
7. release all physical monitor handles.

No “first monitor” fallback exists. No other VCP code is exposed.

DDC work runs off the UI thread and is serialized. User-facing probes and writes
have finite timeouts. A native call cannot be forcibly cancelled safely, so a
timed-out worker keeps the hardware gate until it eventually returns; the UI
falls back or reports failure without freezing.

## Presentation layer

The WinForms presentation layer has no third-party UI dependency. It uses:

- custom-painted rounded glass surfaces over an opaque graphite gradient;
- a custom brightness slider with mouse, wheel, and keyboard input;
- explicit accessibility roles, names, descriptions, and focus cues;
- per-monitor DPI scaling and layout containers rather than fixed page
  coordinates; and
- optional Windows dark-mode, rounded-corner, backdrop, and scrollbar theme
  requests that fail safely on unsupported Windows builds.

Presentation changes do not bypass `MonitorManager`. Slider requests still pass
through the same debounce, external-target descriptor, hardware gate, timeout,
write/readback, and failure handling used by the original interface.

## Mirrored topology

Multiple DisplayConfig targets can share one GDI source. The source is probed
only once. Unique physical descriptions are matched to EDID model names first;
equal-count position is the secondary mapping. An ambiguous unequal-count
mapping is rejected.

Hardware DDC/CI can still target an external physical monitor in Duplicate
mode, but an internal clone requires a unique physical-description/EDID match;
positional mapping is disabled. A software overlay belongs to the shared
desktop source. If that source also drives the built-in panel, software dimming
is blocked and the UI asks the user to switch to Extend. External-only mirrored
targets may share one software control.

## Software dimming

`SoftwareDimmingService` creates a borderless black overlay restricted to the
external screen bounds. The overlay is:

- click-through;
- non-activating;
- absent from Alt+Tab;
- hidden at 100%;
- capped at 85% opacity at 0%; and
- disposed on refresh and exit.

Internal, virtual, unclassified, and built-in-mirrored targets fail the overlay
guard.

## Local state

- Hashed monitor ID → software percentage
- Optional HKCU Run entry → executable path

There is no network component, updater, service, driver, telemetry SDK, or
privileged helper.
