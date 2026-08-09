# Troubleshooting

## Cannot connect

- Confirm both devices are on the same LAN/Wi-Fi.
- Guest Wi-Fi and access-point/client isolation can prevent peer-to-peer traffic.
- Ensure the receiving app remains open during the transfer.
- Check Windows/macOS firewall rules for SwiftDrop.
- On Apple platforms, confirm local-network access is allowed.
- Generate a fresh pairing link; pairing invitations expire after five minutes and are one-time use.

## Pairing link expired

Create a new pairing link on the receiving device and connect again.

## Integrity check failed

SwiftDrop deletes the incomplete partial file when the final SHA-256 does not match. Retry the transfer on a stable network.

## Resume does not happen

Resume works when a matching `.swiftdrop.part` file remains in the receive directory. If it was deleted or the receiver storage was cleaned, the transfer starts from byte zero.

## Port conflict

SwiftDrop uses TCP port 47821 by default and UDP port 47822 for the discovery helper. Another process or restrictive network policy can block those ports.
