# Store Privacy and Data-Safety Declaration Guide

This document describes the behavior of the current SwiftDrop source tree so release managers can answer platform privacy questionnaires accurately. Store forms and legal requirements can change; verify the exact current form at release time.

## Current application behavior

SwiftDrop is designed as an account-free local-network transfer application. Transfer payloads are sent directly between user-selected peers on the local network. The project does not include a SwiftDrop-operated cloud upload endpoint, advertising SDK, analytics SDK, or account system.

## Local data

SwiftDrop can store on the user's device:

- device ID and display name;
- device certificate/private key through platform secure storage;
- local settings/preferences;
- trusted-peer device IDs, names, certificate fingerprints, and timestamps;
- transfer-history metadata such as direction, peer display name, filename/description, size, status, and timestamp;
- bounded privacy-aware diagnostic events;
- received files chosen/accepted by the user;
- temporary `.swiftdrop.part` receive staging;
- temporary app-cache copies of inbound Android share-sheet files until pruned.

Text snippet contents are not stored in transfer history or diagnostics.

## Network data

During a user-initiated pairing/transfer, the peer can necessarily observe protocol/network metadata including reachable local IP address, device display name, device ID, certificate/public-key identity, transfer metadata, and payload content that the user deliberately sends to that peer.

This peer-to-peer disclosure is different from collection by the SwiftDrop project/operator. Store forms should be answered using each platform's exact definitions for collection, sharing, peer communication, diagnostics, and user-generated content.

## Permissions/capabilities

Current source can request/use platform capabilities for:

- local network and Internet socket APIs used for LAN transfer;
- Bonjour/multicast discovery;
- system file/folder pickers;
- Android share intents;
- Android foreground data-sync service while an active user transfer runs;
- notifications required/used by platform foreground behavior;
- clipboard only after explicit user action;
- secure storage for certificate material.

SwiftDrop does not need contacts, microphone, precise location, SMS, call logs, or continuous clipboard access for its baseline transfer workflow.

## Advertising and tracking

No advertising or cross-app tracking SDK is included in the current repository. Do not mark advertising/tracking behavior as present unless a future release actually adds it and updates `PRIVACY.md` before shipping.

## Diagnostics

Local diagnostic events are bounded and intended to contain operational metadata only. Safe export is user-initiated. The source does not automatically upload diagnostic logs to a SwiftDrop server.

## Release checklist

Before submission:

1. Review `PRIVACY.md` against the exact tagged source.
2. Inspect the final dependency graph for SDK behavior that could alter store answers.
3. Inspect platform manifests/entitlements/capabilities.
4. Verify analytics/crash-reporting/ads have not been added without documentation.
5. Test permission prompts on physical devices.
6. Make store answers match actual binary behavior, not planned features.
7. Update this document and `PRIVACY.md` whenever a release materially changes data handling.

Do not copy this document blindly into a store form; use it as an engineering source of truth and apply the store's current definitions.
