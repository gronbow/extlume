# Privacy

ExtLume is designed to work entirely offline.

## Data the app does not collect

- No telemetry or analytics
- No crash upload
- No update check
- No account or cloud synchronization
- No advertising identifier
- No network request

## Local data

The app may write the following values under the current Windows user:

- `HKCU\Software\ExtLume\SoftwareLevels`
  - a SHA-256-derived local monitor identifier;
  - the selected software-dimming percentage.
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  - the application path, only when “Start with Windows” is enabled.

The monitor identifier is derived locally and cannot be used by the app to
recover the original device path. The app does not persist raw EDID serial
numbers, Windows device paths, or a usage history.

## Diagnostics and issue reports

The bundled read-only probe displays model names and broad connection classes.
It does not transmit them. If a user attaches diagnostic output to a GitHub
issue, that upload is a deliberate action by the user and is then governed by
GitHub's privacy terms.
