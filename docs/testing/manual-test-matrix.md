# SwiftDrop Manual Test Matrix

Use synthetic test files only. Never use secrets or irreplaceable personal files while validating development builds.

## Platforms

| Sender | Receiver | Pairing | Small file | Large file | Resume | Cancel | Collision | Risk warning |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Android | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Android | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | macOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| macOS | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| macOS | macOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Android | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | macOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| macOS | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |

## Pairing and identity

1. Generate a fresh invitation and confirm that it contains no private key.
2. Confirm sender rejects a receiver certificate whose SHA-256 fingerprint differs from the invitation.
3. Confirm receiver requires a sender certificate.
4. Confirm receiver displays sender name and sender certificate fingerprint before accepting bytes.
5. Reject the incoming transfer and confirm no final file is created.
6. Reuse the same invitation and confirm it is rejected.
7. Wait beyond the pairing lifetime and confirm the invitation is rejected.
8. Confirm a new invitation succeeds after an old one expires or is consumed.

## Transfer safety

1. Send a zero-byte file.
2. Send a normal text/image file.
3. Send a file near the configured single-file safety limit using synthetic data.
4. Attempt a file above the limit and confirm rejection occurs before transfer.
5. Interrupt a transfer, retain the `.swiftdrop.part` file, retry with a fresh invitation, and verify resume behavior.
6. Corrupt a staged file and confirm SHA-256 verification prevents finalization.
7. Attempt `../`, rooted, alternate-separator, and nested traversal paths and confirm rejection.
8. Send a filename that already exists and confirm SwiftDrop creates a collision-free destination rather than overwriting it.
9. Fill a disposable test volume close to capacity and confirm the free-space guard rejects the transfer before consuming the remaining bytes.
10. Confirm completed files preserve the manifest last-write timestamp where the platform permits it.

## Potentially dangerous files

Test synthetic empty files with extensions such as `.exe`, `.ps1`, `.apk`, `.zip`, `.docm`, and `.jpg`.

- High-risk extensions must display a clear warning before acceptance.
- Caution extensions must display a caution message.
- Ordinary documents/images must still show sender, filename, size, and certificate fingerprint.
- No received file may auto-open after transfer.

## UI and accessibility

- Test light, dark, and system themes.
- Test text scaling at the platform's largest supported accessibility size.
- Navigate all interactive controls by keyboard on desktop.
- Verify focus order and readable labels for pairing, send, cancel, history, settings, and diagnostics.
- Verify long filenames and device names do not make controls unreachable.
- Test portrait, landscape, tablet, narrow desktop, and resized desktop windows.
- Confirm progress and completion are communicated without relying on color alone.

## Network failure scenarios

- Receiver closes during handshake.
- Receiver closes after metadata but before bytes.
- Sender cancels mid-transfer.
- Wi-Fi disconnects mid-transfer.
- Device changes Wi-Fi networks mid-transfer.
- Windows/macOS firewall blocks inbound traffic.
- Apple local-network permission is denied.
- Guest Wi-Fi/client isolation prevents direct peer connectivity.
- LAN has IPv6 only and UDP IPv4 fallback is unavailable.

Every failure should be bounded, should not freeze the UI thread, and should leave either a valid completed file or a clearly staged/failed state rather than a corrupt final file.

## Release evidence

For each supported platform pair record:

- app version and commit;
- OS/device versions;
- network type;
- test file sizes and SHA-256 values;
- pass/fail result;
- screenshots only when they contain no real pairing invitations or personal content;
- defect link for every failure;
- retest result after a fix.
