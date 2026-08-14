# SwiftDrop Usage Terms

SwiftDrop source code is licensed under the Apache License 2.0. See `LICENSE` for the controlling software-license terms.

This document explains expected product use and project boundaries. It does not replace or modify the Apache-2.0 license.

## Local-transfer responsibility

SwiftDrop provides tools for user-initiated local-network transfer. You are responsible for having permission to send, receive, store, or redistribute the content you transfer and for complying with applicable law, organizational/device/network policy, and third-party rights.

Do not use SwiftDrop to distribute malware, stolen credentials, illegal content, or content you are not permitted to transfer.

## User authorization

Only pair/connect with devices you are authorized to use or communicate with. Do not use SwiftDrop to probe, access, disrupt, or transfer data to/from systems or networks without permission.

Manual IP/QR/deep-link pairing does not grant permission to access another person's device or network.

## Received files

A file received from another device can be unsafe even when transport integrity succeeds.

SHA-256 integrity verification means SwiftDrop can verify that received bytes match the transfer's expected bytes under the protocol. It does **not** mean the file is trustworthy, legitimate, safe to execute, or malware-free.

SwiftDrop does not automatically open or execute received files.

## Local-network/security limitations

Security controls reduce specific risks but cannot make a compromised endpoint trustworthy. A malicious or compromised sender can intentionally send harmful content; a compromised receiver can access data that the user/system allows it to receive/read.

Do not treat certificate fingerprint confirmation, TLS, integrity checks, or trusted-device status as a substitute for user judgment about the content itself.

## Availability and data safety

Peer-to-peer transfer depends on device storage, permissions, provider behavior, firewall/router policy, local-network topology, operating-system lifecycle/background limits, and hardware.

The project does not guarantee uninterrupted transfer, universal device/network compatibility, permanent availability, or data recovery.

Keep independent backups of important data.

## Source builds and unofficial packages

The public repository can be built by third parties. A package using the SwiftDrop name is not automatically an official project release merely because its source originated from this repository.

Prefer project-documented distribution channels/releases for production use and do not install untrusted unsigned packages from unknown sources.

Hosted CI/source compilation is not the same as signed-device/store validation.

## Privacy

The current maintained source/application design is local-first and account-free for the local-transfer workflow. Its current data practices are described in `PRIVACY.md`.

Features that materially change data collection/storage/transmission behavior should update privacy documentation and store declarations before release.

## Diagnostics and support

When requesting support or filing a public issue, remove private transferred content, private keys, signing material, pairing capabilities/nonces, credentials, and unnecessary personal information.

See `docs/diagnostics-and-bug-reports.md` and `SUPPORT.md`.

## Security reports

Follow `SECURITY.md`. Security-sensitive details should be reported privately rather than posted publicly.

## Third-party content and services

SwiftDrop can interact with operating-system file providers, local networks, and user-selected content. Those third-party services/providers can have separate terms, permissions, privacy behavior, availability, and technical limits.

Optional Buy Me a Coffee support is external to the transfer protocol and does not unlock features, priority security handling, privileged support, or access to user transfer data.

## Open-source modifications

Apache-2.0 permits use/modification/distribution subject to its terms. Modified builds can behave differently from this repository's maintained source, including different privacy/security/network behavior.

The project documentation describes the maintained repository state, not every downstream fork or modified package.

## No change to Apache-2.0

This document does not replace, narrow, or expand the rights and obligations granted by the Apache License 2.0 for the source code itself. If this document appears to conflict with `LICENSE` regarding software-license rights/obligations, `LICENSE` controls those Apache-2.0 terms.

---

**Made by the Sanskar**
