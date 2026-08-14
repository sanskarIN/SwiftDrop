# SwiftDrop FAQ

## Is a SwiftDrop account required?

No. The maintained local-transfer workflow is account-free.

## Does SwiftDrop upload my files to its own cloud server?

The maintained protocol is designed for direct local-network peer-to-peer transfer and does not use a SwiftDrop-operated payload relay. Read `PRIVACY.md` for local metadata behavior.

## Can SwiftDrop transfer over the public Internet?

Protocol v1 is intentionally scoped to local/private/link-local/unique-local addresses. Public Internet targets and arbitrary DNS peer names are rejected by the maintained pairing/connection policy.

## What platforms are supported by the source?

The containing app targets:

- Android;
- iOS;
- Mac Catalyst;
- Windows.

The Share Extension target is iOS-only.

## Does macOS use a Share Extension?

No. The maintained Mac Catalyst architecture uses the containing desktop app with file/document intake and native `UIDropInteraction` for supported files, folders, text, and pairing links.

## Which solution file should developers use?

`SwiftDrop.slnx` is the canonical solution.

## What can I send?

Maintained source supports single files, multiple files, recursive folders where the platform selection/provider path permits them, and explicit text snippets.

## Does SwiftDrop monitor my clipboard continuously?

No. Clipboard reading occurs only after explicit user action.

## Does SwiftDrop automatically open received files?

No. Received files are not automatically opened or executed.

## Does SwiftDrop scan received files for malware?

No malware-scanning guarantee is made. SwiftDrop can warn for potentially dangerous extensions, but users should still review content and use their operating-system/security tools as appropriate.

## How does pairing work?

Maintained pairing paths include local discovery, QR/deep link, nearby request, one-time 8-digit code, and manual local IP. Pairing capabilities are short-lived, strictly parsed, and intended for one-time authorization.

## Why do I see a certificate fingerprint?

Each device has a local cryptographic identity. The SHA-256 certificate fingerprint is an identity-comparison aid so users can confirm that the peer they are approving is the intended device.

## What happens if I reset my device identity?

SwiftDrop clears stored trusted-device relationships and rotates/resets the local cryptographic identity. Previously paired/trusted peers may need to pair and establish trust again.

## What does trusted-device auto-accept mean?

It is an opt-in setting, off by default. It applies only when the stored trust relationship matches the expected device ID and certificate fingerprint and the content is within normal-risk policy. It does not remove higher-risk warnings/consent boundaries.

## How is transfer integrity checked?

Incoming file data is staged and verified against the expected SHA-256 before final promotion. A mismatched/corrupt payload must not become the final file.

## What is `.swiftdrop.part`?

It is a partial staging file used for safe in-progress/resume behavior. Final promotion occurs only after the expected transfer is complete and integrity checks pass.

## Can SwiftDrop resume a file transfer?

The maintained protocol supports resume when compatible safe partial state remains. Unsafe, missing, inconsistent, or invalid partial state is not trusted.

## Can a retried batch skip files that already finished?

Yes, but only after strict verification. SwiftDrop checks stable transfer ID, canonical sender path, receive-root identity, expected length/hash, path confinement, current file length, and a fresh hash. It verifies the completed item again immediately before acknowledging its reuse.

## Will sending the same files again overwrite existing files?

Final promotion does not silently overwrite an existing completed file. Normal destination collision handling is used for a brand-new explicit send.

## What happens with filenames that are invalid on another operating system?

SwiftDrop uses a portable canonical filename/path policy, including Unicode normalization, invalid-character handling, Windows reserved-name protection, byte/character bounds, and collision deconfliction.

## Can I choose a custom receive folder?

Currently, the maintained custom receive-folder picker is Windows-only. Other targets use SwiftDrop's app-private Received folder instead of requesting broad filesystem access.

## How many transfers can run at once?

The Settings UI allows transfer concurrency from 1 through 8, with a default of 2.

## Can I disable history?

Yes. History retention can be set to 0 days, meaning transfer history is not retained under the maintained retention policy.

## Does history contain my transferred file or text content?

No. History is metadata-oriented. Transferred file bytes and transferred text contents are not stored in SQLite history.

## What does Privacy Mode do?

Privacy Mode redacts/hides peer/file identifiers in privacy-sensitive history and diagnostic presentation/storage paths. It does not move or encrypt received files. Read `PRIVACY.md` for exact behavior.

## Are notifications supported?

Optional completion/failure system notifications are implemented on Android. Other maintained targets currently rely on in-app transfer status for this setting.

## Why can nearby discovery fail even when both devices are on Wi-Fi?

Guest Wi-Fi, AP/client isolation, multicast filtering, enterprise network policy, local-network permission denial, VPN/security software, and host firewalls can block discovery or inbound connections.

## Can manual IP pairing bypass guest-network isolation?

No. Manual IP avoids discovery dependency, but it still requires an actual network path between the devices.

## Which ports are used?

The maintained default transport/discovery configuration uses TCP port 47821 and UDP port 47822 for the discovery helper. Network policy can still block them.

## What is the iOS App Group?

The containing iOS app and Share Extension use:

`group.in.sanskar.swiftdrop`

Signed-device builds require matching Apple Developer provisioning for the app and extension identifiers.

## Why can an iOS Simulator compile pass while a device build still fail?

Simulator compile verifies source/toolchain integration but cannot prove real signing, App Group provisioning, device permissions, provider/lifecycle behavior, TestFlight packaging, or App Store configuration.

## Why can Windows CI compile pass while a signed package still need testing?

Hosted compilation does not prove MSIX signing, installation/update behavior, protocol registration, firewall policy, package capability behavior, or Store packaging on a real Windows system.

## Is a successful CI run the same as production-ready?

No. Production readiness requires the automated gates plus signed packages, physical-device transfer/network/storage/lifecycle tests, accessibility/localization checks, dependency/license review, and store/privacy/signing validation for the exact release candidate.

## Where do developers find build instructions?

See `BUILDING.md` and `docs/development-guide.md`.

## Where is the protocol documented?

See `docs/protocol/wire-format.md`, `docs/protocol/security.md`, and the compatibility documents under `docs/protocol/`.

## Where is the threat model?

`docs/security/THREAT_MODEL.md`.

## Where can I report a security issue?

Follow `SECURITY.md`. The project business/security contact is `sanskarin@outlook.in`.

## Where can I get general support?

See `SUPPORT.md`. General support email: `supportramsandesh@gmail.com`.

## Is financial support required?

No. Buy Me a Coffee support is optional and does not unlock features, privileged support, security priority, or access to user data.

---

**Made by the Sanskar**
