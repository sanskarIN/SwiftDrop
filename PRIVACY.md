# SwiftDrop Privacy

SwiftDrop is designed as a local-network, account-free transfer application.

## Current release

- No SwiftDrop account is required.
- SwiftDrop does not upload transferred file contents to a SwiftDrop cloud service.
- File and text content is sent directly to the selected nearby device over the local network.
- Device identity material is generated locally.
- The device private certificate is stored using platform secure storage.
- Transfer history is local metadata only and does not contain transferred file bytes.
- Privacy mode can hide filenames stored in local transfer history.
- SwiftDrop does not continuously monitor the clipboard.
- SwiftDrop does not automatically open received files.
- SwiftDrop does not intentionally collect contacts, microphone data, background location, advertising identifiers, or analytics in the current baseline.

## Data stored on the device

SwiftDrop may store:

- a random local device ID;
- the user-visible local device name;
- a local device certificate/private key in secure storage;
- app settings;
- explicitly trusted-device metadata when that feature is used;
- transfer history metadata such as direction, peer name, timestamp, size, status, integrity result, and optionally filename;
- incomplete `.swiftdrop.part` files required for resumable transfer;
- completed received files in the selected/application receive location.

## Pairing invitations

A pairing invitation contains temporary connection metadata, including the receiver LAN address, certificate fingerprint, expiration time, and random one-time nonce. It does not contain the receiver private key. Pairing invitations should still be treated as temporary sensitive capabilities and should not be published.

## Network visibility

Local discovery traffic may reveal that a device is running SwiftDrop to other devices on the same LAN when discovery is enabled. Local network administrators and operating systems can observe network metadata such as source/destination addresses and traffic volume even though TLS protects transfer contents in transit.

## Deleting data

Users can clear transfer history through the app. Received files remain normal user files and must be deleted from their receive location when no longer wanted. Resetting app storage through the operating system can remove app-local metadata and identity material, subject to platform behavior.

## Future features

If a future version adds accounts, relay transfer, cloud synchronization, crash reporting, analytics, or another remote service, that feature must be documented separately before release and must not silently change the privacy behavior described for the current local-only mode.

## Contact

- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
