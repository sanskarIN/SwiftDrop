# SwiftDrop Troubleshooting

This guide covers common user, network, platform-integration, build, and validation failures for the maintained SwiftDrop source.

## First checks

Before deeper debugging:

1. Confirm both devices are on the same reachable LAN/Wi-Fi.
2. Avoid guest/client-isolated networks for normal peer-to-peer testing.
3. Confirm local-network permission is allowed where the OS exposes it.
4. Confirm the receiving app is available during connection/transfer.
5. Confirm the host firewall permits SwiftDrop on the intended private/local network profile.
6. Generate a fresh pairing invitation when reconnecting; invitations are short-lived and one-time authorization is not reusable.
7. Check free storage on both sender and receiver.
8. If the problem involves an external provider/share/drop, try a normal file selected directly from app storage to separate provider problems from transport problems.

## Nearby device does not appear

Possible causes:

- mDNS/multicast blocked by router or enterprise policy;
- AP/client isolation;
- guest Wi-Fi;
- local-network permission denied;
- multicast lock/lifecycle issue on Android;
- Windows/macOS firewall policy;
- devices are actually on different VLANs/subnets without peer routing;
- VPN/security software intercepting local traffic.

Try:

- QR/deep-link pairing;
- one-time 8-digit code;
- manual local-IP pairing;
- a normal home/private LAN with multicast allowed.

Manual IP avoids discovery dependency but cannot bypass a network that blocks traffic between the devices.

## Cannot connect after successful discovery

Discovery and TCP connectivity are separate.

Check:

- receiver remains active/listening;
- TCP port is not blocked;
- firewall profile is Private/local rather than an unexpected restrictive Public profile;
- no other process owns the configured port;
- the peer address is still current after Wi-Fi/network changes;
- pairing/authorization has not expired or already been consumed.

SwiftDrop's maintained default transport/discovery configuration uses TCP port **47821** and UDP port **47822** for the discovery helper.

## Pairing link/code expired or rejected

Create a fresh pairing invitation.

The pairing representation is deliberately strict. Modified, padded, percent-encoded, whitespace-wrapped, duplicate-field, unknown-field, malformed, expired, or replayed capabilities are rejected rather than normalized into acceptance.

Do not edit a `swiftdrop://pair` URI by hand.

## Fingerprint does not match

Do not approve the connection.

A mismatch can mean:

- the other device reset/recreated its local identity;
- you selected the wrong peer;
- stale peer metadata is being compared;
- an unexpected endpoint is answering.

Verify the device directly, remove/revoke stale trust if applicable, and create a fresh pairing flow.

## Trusted device no longer auto-accepts

Auto-accept requires the stored trust relationship to match both the expected device identity and certificate fingerprint and is limited to normal-risk content.

It will stop applying after identity reset/rotation or trust revocation. Pair/trust the intended device again rather than weakening the certificate check.

## Integrity check failed

SwiftDrop verifies the expected SHA-256 before final promotion. A mismatch means the received bytes are not the declared content.

Possible causes include:

- source content changed during transfer;
- interrupted/corrupted network or staging state;
- incompatible partial state;
- underlying provider/file behavior changed after selection.

Retry from a stable source/network. Do not rename/promote the `.swiftdrop.part` file manually as a substitute for integrity verification.

## Resume starts from zero

Safe resume is conditional. SwiftDrop may restart when:

- the partial file is missing;
- the partial state length is invalid;
- receive-root/path safety cannot be re-established;
- the source is no longer a regular safe source;
- expected transfer metadata no longer matches;
- local cleanup removed staging state.

Starting from zero is safer than trusting incompatible state.

## A completed batch item is sent again on retry

Completed-item reuse is intentionally strict. Reuse fails closed when the previously completed destination:

- is missing;
- changed length;
- changed contents/hash;
- no longer matches the same stable transfer ID/canonical source path;
- no longer maps to the same receive-root identity;
- cannot be safely confined below the receive root.

This prevents a stale completion record from falsely acknowledging modified content.

## Filename changed or a collision suffix was added

SwiftDrop uses portable canonical filename rules and non-overwrite collision handling. A filename may be sanitized/deconflicted to remain safe across Windows/Android/iOS/Mac Catalyst filesystems or to avoid overwriting an existing file.

The protocol requires canonical sender paths; unsafe/noncanonical inbound paths are rejected rather than silently reinterpreted after authorization.

## File/folder selection is rejected

SwiftDrop rejects symbolic-link/reparse sources at important send boundaries. Recursive folder selection is link-safe and bounded.

Choose the real regular file/folder rather than a link/alias/reparse path where the platform exposes one.

## Low-storage rejection

SwiftDrop performs storage capacity/budget checks before and during important staging/transfer paths. External provider data with unknown length is bounded by the remaining staging budget and repeated reserve checks.

Free storage space and retry. Do not disable storage checks to force the transfer.

## Android share does not import

Check:

- provider URI remains readable by the receiving app;
- provider has not returned an invalid declared size;
- aggregate/count/per-file staging limits are not exceeded;
- cache has enough reserve space;
- the provider did not fail/cancel mid-stream;
- the shared type is supported by the current intake path.

SwiftDrop copies provider content into bounded app cache and cleans failed staging. Shared content appears for review; it is not auto-sent.

## Android notification is missing

Optional completion/failure system notifications are Android-only in the maintained Settings implementation.

On Android 13+, ensure notification permission is allowed after enabling the preference. A denied notification permission does not mean the transfer itself failed; inspect in-app status/history.

## Android transfer stops when app/lifecycle changes

The maintained Android path uses a foreground data-sync service for active user-initiated transfers, but real behavior can still vary with OS/OEM battery policy, force-stop, network changes, and platform restrictions.

Test release behavior on the actual supported device/OS and review current Android policy before publication.

## iOS Share Extension does not appear

For a signed/device build, verify:

- Share Extension target is included in the containing app;
- extension bundle ID is `in.sanskar.swiftdrop.share`;
- activation rules match the shared content type;
- containing app/extension versions are consistent;
- the correct provisioning profile is used.

The maintained Share Extension is **iOS-only**.

## iOS Share Extension opens but handoff fails

Verify the signed app and extension both have the correct App Group entitlement:

`group.in.sanskar.swiftdrop`

Check:

- App Group exists in Apple Developer configuration;
- both identifiers/profiles include it;
- package manifest is current and valid;
- package is not stale/malformed;
- physical `files/` contents exactly match the manifest;
- no symlink/unmapped/nested undeclared content is present;
- app cache has enough capacity for validated recopy.

Simulator compile success does not prove signed App Group provisioning.

## iOS/Mac document URL cannot be staged

The source may require security-scoped access depending on provider/location. The provider can revoke access or return a URL that cannot be read later.

Retry from a locally accessible file and test the real provider under a signed build.

## Mac Catalyst drop is rejected

The maintained Mac path rejects unsafe/link sources and applies shared external staging budgets.

Check:

- dropped item is a supported file/folder/text/pair link;
- security-scoped provider access succeeds;
- provider callback returns within the bounded response wait;
- staging budgets/storage are available;
- folder/source is not a symlink/reparse path.

Mac Catalyst uses native drop/document intake; it does not use a maintained Mac Share Extension.

## Windows drag/drop does not work

For packaged/signed testing verify:

- target build includes current Windows integration source;
- drag payload type is supported (files, folders, text, pairing link);
- Windows App SDK/WinRT APIs are available under the package/environment;
- source path remains accessible;
- the dropped source passes regular-source/link checks.

## Windows custom receive folder cannot be selected

Custom external receive-folder selection is currently a Windows-specific feature using the system folder picker.

If it fails:

- verify the application is running in the expected packaged/desktop context;
- choose a folder the current user can access;
- test a normal local user folder first;
- fall back to **Use app folder** to confirm transfer logic independently of the custom picker.

## Windows protocol activation does not work

Hosted compile does not prove installed protocol registration.

For real validation:

- install the signed MSIX/package;
- inspect package protocol declaration;
- invoke a valid `swiftdrop://pair` URI;
- test cold and warm activation;
- verify package identity/capabilities after update.

## Firewall blocks Windows or Mac transfers

Allow the signed application for the intended private/local network scope. Test both allowed and blocked cases so diagnostics/help text are accurate.

Avoid suggesting users disable the firewall globally.

## Localization text is missing or formatting fails

Run:

```bash
python3 scripts/validate_localization.py
```

Check:

- English/Hindi key parity;
- duplicate keys;
- nonempty values;
- formatted placeholder index parity;
- XML validity.

Do not fix one language by deleting the corresponding key from the other catalog.

## Apple metadata validator fails

Run:

```bash
python3 scripts/validate_apple_integration.py
```

Check current source assumptions:

- iOS containing app entitlement file;
- Mac Catalyst sandbox entitlement file;
- iOS-only Share Extension target;
- iOS extension App Group entitlement;
- bundle/application versions and identifiers;
- App Group consistency.

Do not add a fake Mac Catalyst extension entitlement to satisfy stale documentation; the maintained extension architecture is iOS-only.

## Core restore fails because of NuGet vulnerability audit

SwiftDrop explicitly audits direct/transitive packages at low-or-higher severity with warnings-as-errors.

Do not bypass the audit first. Identify the package/advisory, determine whether a fixed version exists, update the smallest compatible dependency set, and rerun restore/tests/platform builds.

Machine-readable review:

```bash
dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --vulnerable --format json
```

## Tests fail after package/tool update

Separate failures into:

- source compile/API changes;
- analyzer/test-runner behavior changes;
- actual regression failures;
- platform workload/package version mismatch.

Do not weaken assertions blindly. Determine whether the expected contract or the implementation is wrong, then fix source/tests/docs consistently.

## Platform build passes locally but fails in CI

Compare:

- exact .NET SDK version;
- installed MAUI workload versions;
- runtime identifier;
- target framework;
- runner OS/architecture;
- restore arguments;
- packaging/signing differences;
- environment-only SDK availability.

Use the maintained workflow commands as the source for hosted compile behavior.

## CI passes but the app fails on device

This is possible and expected for OS integration defects. Hosted compilation cannot prove:

- signing/provisioning;
- runtime permissions;
- App Group behavior;
- provider lifecycle;
- firewall/router policy;
- package protocol registration;
- low-storage behavior;
- OEM background behavior;
- accessibility integration;
- store packaging/declarations.

Use the manual test matrix and release checklist.

## Diagnostics and bug reports

When reporting a bug, include non-sensitive information such as:

- exact SwiftDrop commit/version;
- OS/version/device model;
- sender and receiver platforms;
- pairing method;
- transfer type/approximate size;
- whether discovery or direct connection failed;
- relevant sanitized diagnostic output;
- exact error text;
- reproducible steps.

Do **not** publish private keys, signing files, pairing capabilities/nonces, real private documents, or unrelated personal data.

## More help

- [User guide](user-guide.md)
- [FAQ](faq.md)
- [Build guide](../BUILDING.md)
- [Platform integration status](platform/integration-status.md)
- [Security policy](../SECURITY.md)
- [Support](../SUPPORT.md)

---

**Made by the Sanskar**
