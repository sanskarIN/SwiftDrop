# Architecture

SwiftDrop separates platform UI from transfer/security logic.

## Projects

- `src/SwiftDrop.App` — .NET MAUI UI, platform manifests, secure device identity, QR pairing, receive-server lifecycle, file picker and sharing integration.
- `src/SwiftDrop.Core` — protocol models, pairing codec, certificate fingerprinting, TLS client/server, UDP discovery, transfer framing, chunked transfer engine, hashing, path safety, and metadata storage.
- `tests/SwiftDrop.Core.Tests` — protocol and security unit tests.

## Transfer flow

1. Receiver creates a short-lived pairing payload containing LAN address, port, certificate SHA-256 fingerprint, expiration and random nonce.
2. Sender validates the payload and connects to the receiver through TLS.
3. Sender pins the certificate fingerprint from the pairing payload.
4. Sender presents the one-time nonce in the first bounded protocol frame.
5. Receiver atomically consumes the nonce. Reuse is rejected.
6. Receiver returns a resume offset for an existing `.swiftdrop.part` file.
7. File bytes stream in bounded chunks directly to disk.
8. Receiver computes SHA-256 over the complete partial file.
9. Only after a successful integrity check is the partial file atomically renamed to the final name.

## Data storage

SwiftDrop does not store user file contents in SQLite. Transfer bytes are streamed directly between the network and filesystem. Trusted-peer metadata can be stored in SQLite. Device certificate material is stored with MAUI secure storage.
