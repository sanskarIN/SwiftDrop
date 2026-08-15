# SwiftDrop User Guide

This guide describes the maintained SwiftDrop user workflow. It documents source behavior; availability on a real device still depends on a correctly built, signed, and installed application for that platform.

## 1. What SwiftDrop does

SwiftDrop transfers files, folders where supported, multi-file batches, and explicit text snippets between nearby devices over the local network. No SwiftDrop account is required for the local-transfer workflow.

The protocol is designed for local/private/link-local/unique-local peer addresses. It is not a general Internet transfer service and does not use a SwiftDrop-operated payload relay.

## 2. Before transferring

For the most reliable connection:

- connect both devices to the same normal LAN or Wi-Fi network;
- avoid guest/client-isolated Wi-Fi where peer-to-peer traffic is blocked;
- allow local-network access where the operating system asks for it;
- allow the app through the local firewall when required;
- keep the receiving application available during an active transfer;
- use a fresh pairing invitation when reconnecting after an expired or consumed pairing attempt.

## 3. Discovering nearby devices

SwiftDrop uses local discovery with mDNS/DNS-SD and a bounded UDP fallback. A device may not appear automatically when multicast or peer-to-peer LAN traffic is blocked.

If discovery fails, use one of the explicit pairing alternatives:

- QR/deep-link pairing;
- nearby pairing request;
- one-time 8-digit code;
- manual local-IP pairing.

Manual pairing still requires a network path between the two devices; it does not bypass router isolation or firewall policy.

## 4. Pairing securely

A pairing invitation is short-lived and intended for one-time authorization. When pairing:

1. Start pairing from the receiving device.
2. Use the displayed QR/deep link, nearby request, one-time code, or manual local IP from the sending device.
3. Review the peer/device information.
4. Compare the certificate SHA-256 fingerprint when the UI asks you to confirm identity.
5. Approve only when the devices and fingerprint match what you expect.

If an invitation has expired or has already been used, create a new one.

## 5. Sending a single file

1. Pair with the receiving device.
2. Choose the file action in SwiftDrop.
3. Select a regular file from the platform picker.
4. Review the selected file and destination peer.
5. Start the send.
6. The receiving device must approve the incoming transfer unless a narrowly-scoped trusted-device auto-accept rule applies.
7. Follow progress, throughput, and status in the transfer UI.

SwiftDrop validates outgoing source safety and refuses link/reparse sources at the send boundary. The receiver stages incoming data as a partial file, verifies SHA-256, and only then promotes the result to the final destination.

## 6. Sending multiple files or a folder

Multi-file and recursive-folder transfers use a batch manifest. Folder selection availability depends on the target platform and its picker/provider model.

Before transfer, SwiftDrop canonicalizes protocol paths and deconflicts portable collisions. On the receiver, the batch approval UI can support:

- accept all;
- select individual items;
- reject the batch.

A batch uses a stable transfer ID while it is being resumed. A brand-new explicit send receives a new transfer ID, so deliberate duplicate sends remain separate operations.

## 7. Sending text

SwiftDrop supports explicit text snippets. Clipboard content is read only after an explicit user action; SwiftDrop does not continuously monitor the clipboard.

Use the text action, enter or paste the desired text, select the paired peer, review the send, and approve it on the receiver.

## 8. Receiving content

Incoming untrusted content requires user consent. Review:

- peer identity;
- item or batch type;
- filenames or text summary where applicable;
- sizes;
- any higher-risk extension warning.

SwiftDrop does not automatically open or execute received files.

## 9. Pause, cancel, retry, and resume

An active transfer can be cancelled. Supported send flows also maintain pause/retry state designed around fresh pairing authorization rather than silently replaying stale authorization.

For partial file resume, the receiver must still have the compatible partial state. If that state is missing, invalid, unsafe, or inconsistent, SwiftDrop starts safely rather than trusting it.

For completed items in a retried batch, SwiftDrop reuses an already-finalized item only after verifying its transfer ID, canonical sender path, receive-root identity, expected length, SHA-256, confinement, and current file content. It verifies the completed file again before acknowledging zero-byte reuse for that item.

## 10. Android sharing

Android supports `ACTION_SEND` and `ACTION_SEND_MULTIPLE` intake for supported text/files. Provider content is copied into bounded app cache, checked against count/per-file/aggregate staging budgets, then placed in the review flow.

Shared content is never auto-sent.

Optional completion/failure system notifications are implemented on Android. On Android 13 and newer, SwiftDrop requests notification permission only when notifications are enabled and saved.

## 11. iOS Share Extension

The maintained Share Extension is iOS-only. It accepts supported file/image/movie/text/web URL inputs and hands them to the containing app through App Group `group.in.sanskar.swiftdrop` using a bounded, versioned package format.

The containing app validates the package before presenting it for review. Shared content is never auto-sent.

Signed-device operation requires matching Apple Developer App Group provisioning for both:

- `in.sanskar.swiftdrop`;
- `in.sanskar.swiftdrop.share`.

## 12. Mac Catalyst drag and drop

The maintained Mac Catalyst desktop path uses native `UIDropInteraction`, document/file URL intake, and temporary security-scoped access. It supports supported files, folders, text, and pairing links through the containing app.

There is no maintained Mac Catalyst Share Extension target.

## 13. Windows drag, drop, and receive folder

Windows supports native files/folders/text/pair-link drag and drop and the `swiftdrop` protocol activation path.

Windows is also the maintained target that supports choosing a custom receive folder with the system folder picker. Other targets currently use SwiftDrop's app-private Received location instead of requesting broad filesystem access.

## 14. Queue, history, devices, trust, and diagnostics

### Queue

The queue coordinates bounded transfer concurrency and shows pending/active transfer state. Restart persistence is metadata-minimal; it does not silently replay stale transfer authorization after app restart.

### History

History stores transfer metadata, not transferred file/text contents. Retention can be changed in Settings, including zero days for no retained history. Completed transfers with real timing/byte measurements can show per-transfer duration/throughput plus a weighted History performance summary. Legacy and unmeasured rows remain unmeasured, and resumed-transfer rates use only bytes actually transferred after the resume offset.

### Nearby devices

The Devices page surfaces current locally discovered peers and pairing actions.

### Trusted devices

Trust is certificate-bound. Revoking a trusted device removes that stored trust relationship. Auto-accept for trusted devices is disabled by default and remains limited to normal-risk content.

### Diagnostics

Developer diagnostics are intentionally bounded and privacy-aware. Safe exports exclude transferred contents, private keys, reusable pairing capabilities, and pairing nonces.

## 15. Settings overview

See [Settings reference](configuration.md) for every maintained setting, defaults, ranges, platform differences, and security/privacy implications.

Important rules:

- Resetting device identity changes the local cryptographic identity and clears trusted-device relationships.
- Privacy mode changes how peer/file identifiers are presented/stored in history/diagnostics; it does not encrypt or relocate transferred files.
- Trusted-device auto-accept is opt-in.
- Completion/failure notifications are optional and off by default on Android, iOS, Mac Catalyst, and Windows; platform permission/system policy still applies.
- Custom receive-folder selection is currently Windows-only.

## 16. Privacy expectations

SwiftDrop's local-transfer design does not send transfer payloads through a SwiftDrop-operated cloud relay. Local metadata is used for settings, history, trust, diagnostics, queue state, and verified resume metadata.

For the exact data categories and retention behavior, read [PRIVACY.md](../PRIVACY.md).

## 17. When something fails

Read [Troubleshooting](troubleshooting.md) for discovery, pairing, firewall, local-network permission, share/provider, App Group, integrity, resume, storage, and platform-build issues.

For general support, see [SUPPORT.md](../SUPPORT.md).

## 18. Safety and trust reminder

Only pair with devices you recognize. Compare fingerprints when shown. Review incoming content before accepting it, especially executable/script/archive/document types from another person or device. SwiftDrop provides transport and integrity controls; it does not claim to be an antivirus or malware-scanning product.

---

**Made by the Sanskar**
