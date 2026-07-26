# Security policy

## Supported versions

Until the first stable release, only the latest published beta receives
security fixes.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Use the repository's
private “Report a vulnerability” / Security Advisory form. Include:

- the affected version;
- Windows version and architecture;
- whether hardware DDC/CI or software dimming was active;
- minimal reproduction steps; and
- the security impact.

Do not include monitor serial numbers, raw device paths, credentials, or other
personal data.

## Security boundaries

The app:

- runs as the current user and does not request elevation;
- performs no network communication;
- exposes only brightness VCP `0x10`;
- rejects virtual displays and built-in panels;
- blocks software dimming and positional hardware mapping when an external
  target shares a duplicated desktop source with the built-in panel;
- re-resolves an exact display before every hardware write;
- verifies hardware writes by reading the value back;
- times out the user-facing operation if a monitor driver stalls; and
- removes software overlays when the app exits.

The app cannot make a malicious or non-compliant monitor firmware safe. Monitor
control remains dependent on the Windows graphics stack and connected hardware.
