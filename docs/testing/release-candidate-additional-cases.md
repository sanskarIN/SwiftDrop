# SwiftDrop additional release-candidate validation cases

Updated: 2026-08-10

This file supplements the existing manual test matrix with cases added by the latest hardening work. It does not mark any case as passed; results must be recorded against the exact signed or release-candidate build.

## External activation and staging

Validate on every platform that supports the applicable input surface:

- cold-start pairing link activation reaches the Main review/confirmation flow;
- warm pairing-link activation while MainPage is visible reaches review immediately;
- multiple warm activations arriving close together are drained without duplicate automatic sends;
- warm shared text updates the editor for review and never auto-sends;
- warm shared/open file activation updates batch selection for review and never auto-sends;
- cancellation/failure during cache staging leaves no partial staged file;
- oversized external file is rejected before staging;
- insufficient staging free space fails before large cache copy;
- staged file source changing size during copy fails safely;
- stale staged cache pruning does not remove a currently selected fresh staged source;
- Apple security-scoped document access is released after staging;
- Apple file-provider/iCloud/third-party document-provider activation behaves correctly under a signed build.

## Receive path and final promotion

Validate:

- `../`, `..\\`, rooted slash/backslash, Windows drive-letter, UNC and device-path syntax are rejected on every target;
- mixed `/` and `\\` separators remain confined beneath the receive root;
- portable Unicode/case collisions are rejected/deconflicted correctly;
- a pre-existing symlink/junction/reparse component is evaluated as part of platform path-safety testing;
- another local writer creating the final destination during an active receive never gets overwritten by SwiftDrop;
- failed final promotion preserves the competing external file;
- partial/resume data behaves safely after a final-promotion collision;
- a staged partial truncated after resume negotiation is rejected;
- a same-length staged partial content mutation is caught by final SHA-256 verification;
- an unexpected staged tail is truncated to the negotiated offset before resuming.

## Protocol sequencing

Validate with a peer/test harness:

- exactly one framed JSON message is consumed at each protocol transition;
- connection close between frames fails the current transition without corrupting the previous one;
- connection close after a valid first frame but before the next frame fails safely;
- file resume offset outside `0..length` is rejected by sender;
- file/item/batch completion length mismatch is rejected by sender;
- accepted text acknowledgement with nonzero offset is rejected;
- reordered batch item-start path is rejected by receiver;
- unknown/duplicate/missing receiver batch plans are rejected by sender;
- one-time authorization replay is rejected after the first accepted consume.

## Privacy and local database resilience

Validate:

- enabling privacy mode hides both peer label and file/description label for old and new history rows;
- disabling privacy mode can reveal pre-existing non-private history again, while rows created during privacy mode remain stored with the private marker;
- history private marker is displayed in the selected UI language rather than as a stored English sentence;
- diagnostics privacy mode redacts IPv4/IPv6 endpoints, GUIDs, certificate fingerprints, paths, email-like tokens and pairing URIs;
- safe diagnostic export applies the same read-time redaction as the Diagnostics screen;
- malformed trusted-device fingerprint rows are never treated as trusted;
- malformed history/diagnostic rows do not prevent valid rows from loading;
- stale queue metadata never restores source paths, text contents or reusable pairing authorization.

## Platform capability checks

Android:

- confirm app data backup is disabled as intended for the release package;
- verify share intents, foreground transfer service, notification permission and multicast behavior.

Windows:

- verify local peer networking works with `privateNetworkClientServer` and without general `internetClient` capability;
- verify Windows Firewall behavior for private/public profiles;
- verify packaged file/folder/text/pairing-link drag-and-drop.

Mac Catalyst:

- verify app sandbox plus network client/server entitlements are present in the signed artifact;
- verify inbound/outbound local peer sockets under sandbox;
- verify document/open-file security-scoped staging.

iOS:

- verify local-network permission, Bonjour, protocol URL activation and document/open-file staging;
- do not mark Share Extension testing complete because a dedicated Share Extension target is not present in current source.

## Result recording

For every case record:

- exact git commit;
- app version/build;
- OS/device version;
- network topology;
- source/receiver direction;
- expected result;
- actual result;
- pass/fail/block status;
- sanitized diagnostic code where applicable;
- issue/commit fixing any failure.
