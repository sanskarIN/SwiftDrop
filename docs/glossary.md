# SwiftDrop Glossary

This glossary defines project-specific terms used across SwiftDrop's user, protocol, security, platform, testing, and release documentation.

## App Group

An Apple entitlement/capability that allows explicitly related apps/extensions from the same developer configuration to share a controlled container. SwiftDrop's iOS containing app and iOS Share Extension use `group.in.sanskar.swiftdrop` for bounded handoff packages.

## App-private receive folder

A receive location owned by SwiftDrop's application storage context. Android, iOS, and Mac Catalyst use the maintained app-private receive location instead of requesting broad filesystem access. Windows additionally supports an explicitly selected custom receive folder.

## Batch

One logical transfer containing multiple files, including files derived from a selected folder tree. A batch has a stable transfer ID during an interrupted retry/resume operation.

## Batch manifest

The sender-declared canonical list of batch items and their protocol metadata, including portable relative paths, lengths, and integrity information used by receiver validation and transfer planning.

## Canonical representation

The single accepted representation of data when aliases could create ambiguity. SwiftDrop applies canonical rules to pairing capabilities, JSON shape, Base64URL, protocol paths, filenames, and transfer IDs. Noncanonical aliases are rejected rather than silently normalized at security boundaries.

## Certificate fingerprint

A SHA-256 digest of a device's local identity certificate. SwiftDrop displays/uses fingerprints to bind peer identity and trust decisions. A fingerprint is not a secret but should still be handled as device-identifying metadata in privacy-sensitive diagnostics.

## Collision deconfliction

The process of producing distinct portable names when two selected/sent items would map to the same canonical destination name. SwiftDrop preserves uniqueness markers even near filename size limits.

## Completed-item reuse

A batch-resume optimization that avoids retransmitting an item already finalized in a previous interrupted attempt. Reuse is permitted only after strict transfer/root/source/path/length/hash/content verification and a second verification immediately before the zero-byte completion acknowledgement.

## Containing app

The normal SwiftDrop application that hosts the main user interface. On iOS it embeds the iOS Share Extension and imports validated App Group handoff packages. On Mac Catalyst, the containing app itself handles native drag/drop; there is no maintained Mac Catalyst Share Extension.

## Core

`SwiftDrop.Core`, the portable .NET project containing reusable protocol, security, validation, transfer, discovery, persistence, privacy, path, and integrity policy that should not depend on MAUI UI/platform types.

## Deep link

A URI routed into SwiftDrop, especially canonical `swiftdrop://pair` pairing activation. Parsing a deep link is not itself proof that the peer is trusted; pairing/TLS/authorization rules still apply.

## Discovery

The untrusted local mechanism used to find candidate peers. SwiftDrop uses mDNS/DNS-SD plus a bounded UDP fallback/helper. Discovery presence is not identity or authorization.

## DNS-SD

DNS-Based Service Discovery, commonly used with mDNS on local networks to advertise/find services without a central DNS server configuration.

## External-input staging

A bounded temporary copy of files/text entering SwiftDrop from an operating-system share/drop/provider path before the user reviews it for transfer. Android shares, the iOS Share Extension, and Mac native drop use shared count/per-file/aggregate budget policy.

## Fresh pairing authorization

New short-lived authorization established for a transfer/retry rather than replaying an earlier consumed/expired pairing capability. Resume metadata is not authorization.

## Hosted-platform compile

A GitHub Actions build showing that platform source/project references compile under the configured hosted runner/workload. It is not proof of real signing, device permissions, package installation, App Group provisioning, firewall behavior, or store acceptance.

## Integrity verification

Validation that received file bytes match the sender-declared expected SHA-256 and length before final promotion. Integrity does not mean the file is malware-free or trustworthy to execute.

## Local-first

SwiftDrop's design principle that the transfer payload moves directly between nearby devices over the local network rather than through a SwiftDrop-operated cloud relay, with metadata kept on device unless an explicitly documented future feature changes that model.

## Local-network permission

Operating-system privacy/network authorization that can gate access to nearby local services, especially on Apple platforms. It is distinct from SwiftDrop pairing approval.

## mDNS

Multicast DNS, a local-network name/service-discovery mechanism. Networks can block multicast even while direct unicast traffic remains possible.

## Metadata-only persistence

Local persistence that records operational/history/trust/resume information without storing transferred file bytes or transferred text contents in SQLite.

## Mutual TLS / client certificate

TLS where the receiver requires a sender client certificate in addition to the sender validating the receiver certificate/pin. SwiftDrop derives sender identity from the authenticated TLS certificate rather than trusting a sender-supplied JSON fingerprint.

## Nonce

A random one-time value used as part of a short-lived pairing/transfer authorization capability. Nonces should not be logged/published and consumed authorization must not be replayable.

## Pairing capability

A short-lived canonical token/URI payload granting a narrowly scoped opportunity to establish an authorized local connection. It contains local connection metadata/fingerprint/expiration/random authorization material but not the private key.

## Partial file / `.swiftdrop.part`

The receiver-side staging file for an incomplete transfer. It is not promoted to the final filename until expected length/integrity validation succeeds. Compatible safe partial state can support resume.

## Portable path

The canonical protocol relative path representation that behaves consistently across sender/receiver operating systems. SwiftDrop uses `/` as the wire separator and rejects rooted, traversal, empty/repeated/trailing separator, backslash, over-depth, and otherwise noncanonical paths.

## Privacy Mode

An opt-in setting that redacts/hides peer/file identifiers in privacy-sensitive history/diagnostic paths. It is not file encryption and does not relocate or delete received files.

## Production-ready

A release state reached only after required automated gates **and** signed-package, physical-device, provider, local-network, storage/lifecycle, accessibility/localization, dependency/license/provenance, privacy, signing, and store validation for an exact release candidate.

## Provider

An operating-system/application source that supplies a shared/selected file or text representation, such as Android `ContentResolver`/content URI providers or Apple `NSItemProvider` representations. Provider metadata/callbacks are untrusted and can fail/change/timeout.

## Receive root

The approved base directory beneath which final incoming files may be created. SwiftDrop validates confinement and rejects unsafe existing link/reparse components so peer-controlled paths cannot escape it.

## Reparse point

A Windows filesystem indirection mechanism that can include symbolic links/junctions and other redirection behavior. SwiftDrop treats relevant reparse/link sources/destination components as unsafe at source/destination boundaries.

## Resume metadata

Bounded local metadata used to determine whether a partial/completed item can be safely resumed/reused. It is not reusable peer authorization and is revalidated against current filesystem/content state.

## Release candidate

One exact frozen Git commit SHA selected for complete automated, signed-package, physical-device, dependency/license, privacy, accessibility, and store validation. A moving branch name is not sufficient candidate identity.

## Release readiness workflow

The candidate-oriented GitHub Actions workflow that aggregates portable verification, platform compiles, and dependency/vulnerability evidence. Its success is necessary automated evidence but does not replace the external signed/manual release checklist.

## Share Extension

SwiftDrop's dedicated **iOS-only** app extension that accepts supported iOS share-sheet inputs, stages them under bounded policy, publishes a strict App Group package, and hands control to the containing app for review. It does not perform an automatic background transfer from the extension.

## Signed/device validated

Evidence from the real target using its actual signing/provisioning/package/permission/provider/filesystem/network environment, distinct from source implementation or hosted compilation.

## Stable transfer ID

A bounded canonical token identifying one logical batch across interruption/retry. It remains stable for an interrupted batch retry but changes for a new explicit send.

## Staging budget

Shared limits for external input, including file count, per-file bytes, and aggregate bytes. Budget is committed only after successful staging so a failed item does not incorrectly consume capacity for later valid items.

## Strict JSON

SwiftDrop's fail-closed JSON policy: valid strict UTF-8, bounded depth/length, no comments/trailing commas, no duplicate members (including case-insensitive aliases where applicable), no unknown fields, and type-specific required shape.

## Symlink

A filesystem object that redirects a path to another location. SwiftDrop rejects relevant source/destination/package symlink/reparse conditions so path containment cannot be bypassed.

## Trusted device

A locally stored relationship bound to a device ID and certificate fingerprint. Trusted-device auto-accept is opt-in, off by default, and restricted to normal-risk content; it is not a blanket permission to bypass higher-risk warnings.

## Untrusted input

Any data not guaranteed to be under SwiftDrop's control, including peer protocol messages, manifests, discovery advertisements, filenames/paths, provider metadata/content, dropped/shared items, pairing text, and persisted state that must be revalidated before security-sensitive reuse.

## Warnings-as-errors

Repository compiler/analyzer policy that treats configured warnings as build failures. Together with explicit NuGet auditing, this helps prevent known quality/security warnings from being silently ignored.

## Wire format

The exact serialized/framed representation exchanged by SwiftDrop peers. See `docs/protocol/wire-format.md`; wire compatibility is controlled by explicit protocol/canonical rules rather than informal object similarity.

---

**Made by the Sanskar**
