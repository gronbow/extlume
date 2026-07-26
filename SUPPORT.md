# Support

## Before opening an issue

1. Confirm the display is active in Windows Display Settings.
2. Click **Rescan** in the app.
3. If hardware control is expected, enable DDC/CI in the monitor OSD when
   possible.
4. Test without a dock, KVM, or adapter if practical.
5. If the laptop panel is duplicated and the app shows the protected state,
   switch Windows to Extend and click **Rescan**.
6. Run `probe.cmd`; it is read-only.
7. Read [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).

## Include in a compatibility report

- App version
- Windows version
- Laptop/GPU model
- Monitor model as shown by the app
- HDMI, DisplayPort, USB-C, adapter, dock, or KVM path
- Whether the UI says **Hardware · DDC/CI** or **Software dimming**
- Expected and actual result

Do not post raw Windows device paths, EDID serial numbers, or unrelated system
logs.

General troubleshooting belongs in GitHub Issues. Security problems must follow
[SECURITY.md](SECURITY.md).
