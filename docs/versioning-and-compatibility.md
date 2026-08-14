# SwiftDrop Versioning and Compatibility Policy

This document explains how application, protocol, storage, and package compatibility should be treated during development and release engineering.

## Application version

The current source project declares:

- display version: `1.0.0`;
- application build/version code: `1`.

These values are source metadata, not proof that a `1.0.0` production release has completed all signing/store/device gates.

## Semantic release intent

For public releases, prefer a semantic-versioning-style interpretation:

- **MAJOR** — intentionally incompatible user/protocol/data behavior requiring explicit migration/compatibility planning;
- **MINOR** — backward-compatible feature expansion;
- **PATCH** — backward-compatible fixes/security/quality updates.

Platform stores may impose separate numeric build/version-code rules; those identifiers must remain consistent with the selected release process.

## Exact candidate rule

Compatibility statements are tied to an exact source candidate. Do not infer compatibility solely from a display version when multiple development commits have used the same version metadata.

Record the full Git commit SHA in release evidence.

## Protocol versioning

Protocol compatibility is governed by the explicit wire/protocol version and strict parser behavior, not only the application marketing version.

Read:

- `docs/protocol/wire-format.md`;
- `docs/protocol/compatibility.md`;
- `docs/protocol/compatibility-matrix.md`;
- `docs/protocol/security.md`.

### Protocol change rules

A protocol change must define:

1. whether old senders can talk to new receivers;
2. whether new senders can talk to old receivers;
3. how unknown versions/types/properties are rejected;
4. whether canonical representation changes;
5. whether stored trust/resume/pairing state remains valid;
6. required negative/interoperability tests.

Do not silently reinterpret an older wire representation into a new security meaning.

## Canonical representation stability

Protocol-v1 canonical rules are part of compatibility, including:

- strict pairing capability representation;
- strict JSON members/framing;
- `/` protocol path separator;
- canonical portable filename/path behavior;
- bounded transfer-ID syntax;
- strict one-time authorization behavior.

Relaxing canonicality can create alias/replay/cross-platform ambiguity and must be treated as a security-sensitive compatibility change.

## Local database schema version

The current SQLite schema version is **3**.

Storage compatibility is handled through explicit migrations. Changes must update:

- migration code;
- migration/corruption tests;
- `docs/storage/database-schema.md`;
- privacy documentation if stored data categories change.

A schema bump should not be made casually; it must have a defined prior-version migration path or an explicit safe reset policy.

## Batch resume compatibility

Completed-batch resume metadata is security/correctness-sensitive. Compatibility requires agreement on:

- stable transfer ID;
- canonical source path identity;
- receive-root identity;
- expected length/SHA-256;
- destination confinement/content verification.

If a future version changes these semantics, prefer failing closed/restarting safely over incorrectly reusing stale completion state.

## Device identity and trust compatibility

Trusted-device state is certificate-bound. Rotating/resetting local device identity intentionally invalidates the previous trust relationship and can require re-pairing.

A future migration must not silently transfer trust to a different certificate identity without an explicit safe proof/transition design.

## Platform package compatibility

### Android

Keep version code/display version, min supported API, manifest/permissions, foreground-service behavior, and signing identity compatible with Play/update rules.

### iOS

Keep containing app/Share Extension versions/build numbers consistent as required by Apple packaging. Preserve bundle identifiers and App Group entitlement unless deliberately migrating them with a documented plan.

### Mac Catalyst

Preserve bundle/signing/sandbox expectations appropriate to the chosen distribution path. The current architecture has no Mac Catalyst Share Extension.

### Windows

Preserve package identity/protocol activation/update expectations. Changing package identity or protocol registration can break updates/activation and requires migration planning.

## Minimum platform versions

Current source declares:

- Android: 24.0;
- iOS: 15.0;
- Mac Catalyst: 15.0;
- Windows target platform minimum: 10.0.17763.0 (with a Windows target framework based on 10.0.19041.0 APIs).

Raising a minimum supported platform version is a compatibility decision and should be documented in README/build/release notes and validated against distribution policy.

## Dependency compatibility

Dependency updates must be evaluated for:

- target-framework compatibility;
- runtime behavior;
- analyzer/build/test behavior;
- security advisories;
- license/notice changes;
- platform workload compatibility.

A dependency update that compiles Core but breaks a MAUI target is not complete.

## Localization compatibility

English/Hindi resource key and placeholder parity is enforced. Removing or changing a formatted key can break runtime presentation.

Treat resource-key/placeholder changes as interface contracts inside the app and update all catalogs/call sites together.

## Configuration compatibility

Current settings have defined defaults and bounds. When adding/changing a setting:

- define a backward-compatible default for existing users;
- define serialization/migration behavior;
- update `docs/configuration.md`;
- test reset/persistence/update behavior.

## Deprecation policy

When retiring a source/API/protocol path:

1. remove obsolete call sites;
2. remove compatibility overloads that could preserve unsafe semantics;
3. add tests that prove the canonical path is used;
4. update docs/status/changelog;
5. avoid keeping dead behavior indefinitely “just in case” when it can bypass current security invariants.

## Compatibility evidence

Use both automated and manual evidence:

- portable unit/integration tests;
- protocol compatibility tests;
- SQLite migration tests;
- hosted target compile matrix;
- signed package update/install tests;
- cross-version/cross-platform physical transfer tests when releasing a compatibility-affecting change.

## Release-note requirements for compatibility changes

Clearly document:

- minimum OS changes;
- protocol incompatibility;
- database migration/reset implications;
- trust re-pair requirements;
- package identity/signing changes;
- removed features or deprecated workflows;
- known limitations.

## Fail-closed rule

When a new version cannot prove that old untrusted/stateful data is safe under the new contract, reject/re-pair/restart/re-stage rather than silently accepting ambiguous legacy state.

---

**Made by the Sanskar**
