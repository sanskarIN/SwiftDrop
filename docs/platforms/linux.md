# SwiftDrop on Linux

SwiftDrop's maintained Linux application is `src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj`.

The Linux host uses Avalonia for the desktop UI and references the same `SwiftDrop.Core` project used by the rest of the repository. Linux therefore does **not** implement a second transfer protocol: framing, pairing validation, TLS, certificate pinning, manifest validation, source/path safety, hashing, collision handling, local-address policy, mDNS/UDP discovery, and transfer/resume primitives remain shared.

## Supported Linux architectures

The maintained desktop project and CI release matrix cover:

- `linux-x64` — 64-bit Intel/AMD Linux;
- `linux-arm64` — 64-bit ARM Linux.

The application targets .NET 10. Release packaging is self-contained, so the generated archive carries the required .NET runtime for its target architecture.

## Desktop environments and display servers

SwiftDrop uses Avalonia's normal Linux desktop backend. X11 is the conservative/default runtime path and works under XWayland on Wayland desktops. Native Wayland support can evolve independently in Avalonia; SwiftDrop does not require users to opt into an experimental backend to use the maintained Linux package.

The app should be validated on representative GNOME/KDE installations before a tagged Linux release. A successful hosted compile/publish is not a substitute for physical desktop testing of file pickers, clipboard integration, URI activation, notifications supplied by the desktop environment, firewall prompts/policies, display scaling, or accessibility behavior.

## Build from source

Requirements:

- .NET 10 SDK matching `global.json` roll-forward policy;
- Git;
- a Linux desktop session for actually launching the GUI.

Restore and build:

```bash
dotnet restore src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj
dotnet build src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj -c Release --no-restore
```

Run from source:

```bash
dotnet run --project src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj -c Release
```

Core verification remains available independently:

```bash
bash scripts/verify-core.sh
```

## Produce a self-contained Linux package

For x64:

```bash
bash scripts/publish-linux.sh linux-x64
```

For ARM64:

```bash
bash scripts/publish-linux.sh linux-arm64
```

The script creates:

```text
artifacts/linux/SwiftDrop-linux-x64.tar.gz
artifacts/linux/SwiftDrop-linux-arm64.tar.gz
```

Each archive contains:

- the self-contained `SwiftDrop.Desktop` executable and runtime files;
- `install.sh` for user-local installation;
- `in.sanskar.swiftdrop.desktop`;
- the SwiftDrop scalable application icon.

## Install a generated archive

Example for x64:

```bash
mkdir -p /tmp/swiftdrop-linux

tar -xzf artifacts/linux/SwiftDrop-linux-x64.tar.gz -C /tmp/swiftdrop-linux

bash /tmp/swiftdrop-linux/install.sh
```

The user-local installer uses:

- executable/runtime directory: `${XDG_DATA_HOME:-$HOME/.local/share}/swiftdrop`;
- command symlink: `$HOME/.local/bin/swiftdrop`;
- desktop entry: `${XDG_DATA_HOME:-$HOME/.local/share}/applications/in.sanskar.swiftdrop.desktop`;
- icon: `${XDG_DATA_HOME:-$HOME/.local/share}/icons/hicolor/scalable/apps/swiftdrop.svg`.

Make sure `$HOME/.local/bin` is in the interactive/desktop-session `PATH` if your distribution does not add it automatically.

## `swiftdrop:` protocol activation

The Linux desktop entry declares:

```text
x-scheme-handler/swiftdrop
```

and launches:

```text
swiftdrop %u
```

The installer asks `xdg-mime` to make SwiftDrop the user-default handler when that command is available. The desktop application accepts launch arguments only when they begin with `swiftdrop://pair`; the value is then passed through the normal strict `PairingCodec` decoder before it becomes an active remote authorization.

Do not broaden this handler to arbitrary URLs, shell fragments, or filesystem commands.

## Local application data

Linux follows the XDG base-directory model.

Unless the corresponding XDG variable is explicitly set, SwiftDrop uses:

```text
~/.config/swiftdrop/
~/.local/share/swiftdrop/
~/.cache/swiftdrop/
```

Identity settings and the local device certificate/private key are stored below the private configuration directory. SwiftDrop attempts to restrict the directory to the current user and private files to user read/write permissions on Unix platforms.

The desktop host deliberately does not persist pairing invitations, one-time transfer authorization, remote endpoints, transferred text, or transferred file contents as reusable credentials.

## Default receive location

When `$HOME/Downloads` exists, the default receive directory is:

```text
$HOME/Downloads/SwiftDrop
```

Otherwise SwiftDrop falls back to its XDG data directory.

The receive directory can be changed from the **Receive** tab. Changing it restarts the local TLS receiver against the selected path.

## Discovery and networking

Linux uses the same local discovery stack as the other SwiftDrop clients:

1. mDNS/DNS-SD;
2. bounded UDP fallback;
3. manual numeric local-IP pairing when discovery is unavailable.

SwiftDrop protocol v1 remains local-network only. Public Internet destinations and arbitrary DNS peer names are not accepted by the shared local-address policy.

The app listens on SwiftDrop's configured local transfer/discovery ports. Host firewall rules, AP/client isolation, guest Wi-Fi policies, multicast filtering, containers/sandboxes, VPN routing, and enterprise network policy can prevent peer discovery or connections even when both applications are functioning correctly.

Do not disable the system firewall globally to make SwiftDrop work. If a distribution firewall blocks the app, allow only the SwiftDrop application/local-network ports needed on the trusted LAN and follow that distribution's normal firewall-management policy.

## Pairing options

The Linux host supports:

- discovered-device pairing with certificate fingerprint pinning;
- `swiftdrop://pair` links;
- manual local-IP pairing with an eight-digit, short-lived one-time pairing code;
- receiving nearby pairing requests after explicit user approval.

A transfer pairing capability is one-time. After a send attempt, the desktop UI discards that remote authorization and requires a fresh pairing capability for the next transfer.

## Sending

The **Send** tab supports:

- a single file;
- multiple files;
- a folder/batch through the shared deterministic batch source builder;
- text snippets;
- progress reporting;
- cancellation;
- resume offsets offered by the receiver.

Source files and recursively enumerated folder items still pass the shared regular-file/link/reparse safety rules before transfer.

## Receiving

The Linux TLS receiver supports:

- explicit accept/reject for an incoming file;
- explicit accept/reject for incoming batches;
- explicit accept/reject for text;
- explicit approval for nearby pairing requests;
- collision-safe destination reservation;
- storage-capacity preflight;
- `.swiftdrop.part` staging;
- SHA-256 verification before final promotion;
- verified batch completion metadata for safe retry/resume;
- re-verification of already-completed batch items before a zero-byte completion acknowledgement.

Incoming text is copied to the clipboard only after the user accepts the request and only when a desktop clipboard is available.

## Security boundary

The Linux UI is a platform host. Security-sensitive behavior remains concentrated in `SwiftDrop.Core` wherever possible.

Important invariants include:

- local/private/link-local/unique-local peer address enforcement;
- strict canonical pairing payloads;
- mutual TLS with receiver fingerprint pinning;
- authenticated sender certificate presence;
- one-time authorization consumption;
- strict bounded JSON/framing;
- canonical cross-platform manifest paths;
- symlink/reparse traversal rejection;
- SHA-256 final integrity checks;
- non-overwrite final promotion.

Linux-specific code should not weaken these rules for convenience.

## CI validation

`.github/workflows/desktop-linux.yml` validates both maintained Linux architectures. For each RID it:

1. validates the repository's Linux integration contracts;
2. restores and builds `SwiftDrop.Desktop` in Release configuration;
3. executes `scripts/publish-linux.sh` to produce the real self-contained package;
4. checks the package executable, desktop entry, and archive;
5. captures direct/transitive dependency JSON;
6. rejects vulnerable dependency evidence through the shared validator;
7. creates a deterministic dependency-evidence manifest;
8. uploads the package archive and audit evidence as a workflow artifact.

## Release validation still required

Before describing a tagged Linux build as production-ready, test the actual package on representative physical Linux systems, including at least:

- x64 launch/install/update/remove behavior;
- ARM64 launch/install when ARM64 hardware is available;
- GNOME and KDE file/folder pickers;
- X11/XWayland operation;
- HiDPI scaling;
- clipboard acceptance flow;
- `swiftdrop:` activation from the desktop/browser;
- local firewall behavior;
- discovery with Android/iOS/macOS/Windows peers;
- file, folder, batch-resume, text, cancel, rejection, collision, low-disk, and integrity-failure paths.

Source compilation and CI packaging are necessary evidence, not a claim that every distribution/desktop combination has been physically certified.
