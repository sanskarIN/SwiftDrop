# SwiftDrop Networking Guide

SwiftDrop protocol v1 is designed for direct nearby-device communication over local/private networks. This document explains discovery, pairing connectivity, address scope, ports, firewall behavior, and network troubleshooting boundaries.

## Network model

SwiftDrop is not designed as a public-Internet file relay. The maintained pairing/connection policy accepts local/private/link-local/unique-local address scopes and rejects public Internet targets/arbitrary peer DNS names for protocol v1.

A successful transfer therefore requires a real local network path between sender and receiver.

## Discovery

SwiftDrop uses two local discovery mechanisms:

1. mDNS/DNS-SD as the primary discovery path;
2. a bounded UDP fallback/helper path.

Discovery is convenience, not authorization. A discovered peer still goes through pairing/identity/authorization rules before transfer.

## Default ports

Maintained defaults:

- TCP **47821** — transfer/pairing transport listener path.
- UDP **47822** — discovery helper/fallback path.

Future protocol versions may deliberately change/extend networking behavior; check source/docs for the exact candidate being tested.

## Pairing without discovery

When automatic discovery is unavailable, SwiftDrop supports explicit alternatives such as:

- QR/deep link;
- nearby pairing request where reachable;
- one-time 8-digit code;
- manual local IP.

These methods can avoid dependence on multicast discovery, but they cannot overcome router/firewall policy that blocks peer-to-peer traffic.

## Address scope

Examples of expected local address categories include:

- RFC1918 IPv4 private ranges;
- IPv4 link-local where applicable;
- IPv6 link-local;
- IPv6 unique-local addresses.

The application does not intentionally accept arbitrary public Internet addresses as protocol-v1 peer targets.

## Guest Wi-Fi and client isolation

Many guest networks allow devices to reach the Internet but intentionally prevent devices from reaching one another. Symptoms include:

- no nearby peers;
- manual IP connects timing out;
- QR pairing parses successfully but transport connection fails.

Use a normal LAN/private Wi-Fi where peer-to-peer traffic is allowed.

## Multicast-blocked networks

Enterprise routers/VLANs may block mDNS/multicast while still allowing direct unicast traffic.

In that situation:

- automatic discovery may fail;
- manual IP/explicit pairing may still work if direct TCP traffic is allowed.

Test both discovery and direct connectivity independently.

## Windows firewall

For real packaged/signed Windows validation:

- use the intended private/local network profile;
- allow SwiftDrop's signed package/app where the firewall prompts/policy requires it;
- verify both allowed and blocked cases;
- do not instruct users to disable Windows Defender Firewall globally.

The package source requests private-network client/server capability rather than a general public-Internet transfer capability for protocol v1.

Hosted Windows compile cannot prove installed firewall/package behavior.

## macOS / Mac Catalyst firewall and sandbox

Real signed Mac Catalyst validation must cover:

- app sandbox network client/server entitlements;
- local listener/client behavior;
- macOS firewall allowed/blocked states;
- security-scoped file/drop access;
- signing/notarization/distribution environment.

A hosted Mac Catalyst compile proves source/toolchain compatibility, not these runtime policies.

## Apple local-network permission

iOS/macOS can gate local-network discovery/connectivity behind platform privacy controls. A user denial can cause discovery/connectivity failure even when the LAN itself is healthy.

Test first-run allow/deny/re-enable behavior on signed physical targets.

## Android multicast behavior

Android discovery can require multicast-related platform handling. SwiftDrop contains Android multicast-lock management around discovery behavior.

Real validation should test:

- lock acquisition/release;
- app lifecycle changes;
- Wi-Fi network changes;
- OEM/device power-management behavior;
- UDP fallback when multicast discovery is unavailable.

## VPNs and security software

VPNs, endpoint firewalls, DNS/security filters, and network protection tools can alter local routing/interface selection or block inbound traffic.

For diagnosis:

1. record the device's active local IP/interface;
2. test a known normal LAN;
3. compare behavior with/without the local-network-affecting software if safe/authorized;
4. restore normal security controls after testing.

Do not permanently disable security software to make SwiftDrop work.

## IPv4 and IPv6

Release validation should cover:

- IPv4-only LANs;
- IPv6-capable LANs;
- link-local/unique-local behavior where used;
- network/interface changes during transfer.

Do not assume a successful IPv4 home-Wi-Fi test proves every IPv6/enterprise environment.

## Network change during transfer

If the device changes Wi-Fi, IP address, interface, or route during an active transfer, the connection may fail. Retry/re-pair under the current network state as needed.

Resume must still follow SwiftDrop's safe authorization and partial/completed-item verification rules; network failure is not a reason to trust stale state blindly.

## Slow or unstable LAN

SwiftDrop uses bounded framing, cancellation, and idle-timeout behavior. Very slow or interrupted links may fail rather than hang indefinitely.

Test:

- large files;
- long pauses;
- intermittent Wi-Fi;
- sender/receiver sleep/lock;
- route changes;
- receiver storage pressure.

## Discovery security boundary

Discovery advertisements are untrusted hints. They must not be treated as proof of identity.

Identity comes from the pairing/certificate/fingerprint/TLS authorization path. Do not add platform shortcuts that auto-trust a device solely because it appeared in mDNS/UDP discovery.

## Firewall rule guidance for users

When a firewall prompt appears:

- prefer allowing SwiftDrop only on trusted/private/local networks;
- avoid enabling unnecessary public-network access;
- revoke the rule if the installation is removed or no longer trusted.

## Diagnostic checklist

When connectivity fails, capture non-sensitive information:

- sender/receiver platform and OS version;
- local IP ranges (redact exact values if publishing publicly);
- network type (home, guest, enterprise, hotspot);
- whether discovery works;
- whether manual IP works;
- firewall/private-public profile state;
- whether local-network permission is granted;
- whether VPN/security software is active;
- exact SwiftDrop commit/version;
- sanitized diagnostic error.

Never publish pairing capabilities/nonces, private keys, or unrelated private network credentials.

## Related documents

- [User guide](user-guide.md)
- [Troubleshooting](troubleshooting.md)
- [Protocol security](protocol/security.md)
- [Threat model](security/THREAT_MODEL.md)
- [Platform permissions](platform-permissions.md)
- [Manual test matrix](testing/manual-test-matrix.md)

---

**Made by the Sanskar**
