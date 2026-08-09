# Security Policy

## Supported versions

Security fixes are applied to the latest release line. During pre-1.0 development, users should update to the newest commit/release before reporting a reproducible issue.

## Reporting a vulnerability

Please do **not** publish a security vulnerability, exploit, secret, certificate, or private transfer data in a public issue.

Report privately to: **sanskarin@outlook.in**

Include the affected version/commit, platform, concise reproduction steps, impact, and any logs after removing personal data. Do not include transferred file contents unless they are synthetic test data.

## Security design

SwiftDrop uses TLS from the operating system/.NET cryptography stack, SHA-256 integrity verification, short-lived one-time pairing nonces, receiver certificate fingerprint pinning, bounded protocol frames, path traversal protection, transfer size limits, and partial-file staging.

SwiftDrop does not implement a custom encryption algorithm. The project does not claim that local-network transfer is risk-free; a compromised endpoint can still access data that endpoint is allowed to read.
