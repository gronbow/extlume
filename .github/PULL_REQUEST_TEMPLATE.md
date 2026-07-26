## Summary

<!-- What problem does this change solve? -->

## Validation

- [ ] `test.cmd` passes
- [ ] `probe.cmd` passes on a real Windows desktop when display code changed
- [ ] User-visible behavior is documented
- [ ] `CHANGELOG.md` is updated when appropriate

## Safety

- [ ] Built-in and virtual displays remain excluded
- [ ] No arbitrary-monitor fallback was added
- [ ] DDC/CI remains limited to brightness VCP `0x10`
- [ ] Hardware writes are re-resolved and read back
- [ ] Physical monitor handles are released
- [ ] No telemetry, network access, elevation, raw device logging, or secret was added

## Hardware context

<!-- If relevant: monitor model, connection chain, extend/mirror mode. Never include a serial number or raw device path. -->
