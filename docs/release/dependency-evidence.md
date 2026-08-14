# SwiftDrop Dependency Evidence

Updated: 2026-08-14

This document defines the machine-readable dependency and vulnerability evidence produced by SwiftDrop's maintained CI/release gates. It describes **source/restored-graph evidence**. It does not claim that an unsigned hosted compile is equivalent to the dependency inventory of a final signed store artifact.

## Goals

The dependency evidence pipeline exists to make four release questions answerable from retained files rather than memory:

1. Which direct and transitive NuGet packages were present in the restored graph for a particular project/target?
2. Did NuGet's machine-readable vulnerable-package view report any known vulnerability entries at that point in time?
3. Which JSON files belong to one uploaded evidence bundle?
4. Have those JSON files changed since the bundle manifest was created?

## Stable JSON format

All maintained evidence commands request:

```text
--format json --output-version 1
```

Pinning the output version keeps the stored report schema intentional instead of silently following a future SDK default.

Vulnerability reports also use:

```text
--include-transitive --vulnerable
```

Target-specific app reports use the target framework that was restored/compiled by the corresponding hosted job.

## Vulnerability enforcement

`scripts/validate_nuget_vulnerability_report.py` validates the generated vulnerable-package report instead of accepting any syntactically valid JSON as a clean result.

The validator:

- accepts UTF-8 and UTF-8-with-BOM JSON;
- requires a top-level JSON object;
- recursively detects non-empty `vulnerabilities` arrays in direct or transitive package entries;
- reports package ID, resolved version, severity, and advisory URL when supplied by the report;
- exits nonzero when a vulnerability finding exists;
- exits nonzero for malformed/unexpected vulnerability-report structure.

Exit status semantics:

- `0` — report structure is valid and no vulnerability entries were found;
- `1` — one or more vulnerability entries were reported;
- `2` — report file/JSON/shape validation failed.

Repository-wide NuGet restore auditing remains enabled separately through `Directory.Build.props`. The JSON validator is an additional evidence check, not a substitute for restore-time audit enforcement.

## Evidence manifests

`scripts/create_dependency_evidence_manifest.py` creates a deterministic JSON manifest for each audit bundle.

Manifest schema version 1 contains:

- `schemaVersion`;
- `fileCount`;
- a path-sorted `files` array;
- each report's relative POSIX path;
- exact byte length;
- lowercase SHA-256 digest of the exact report bytes.

The generator refuses an output path outside the evidence root and fails when the root contains no report JSON files. An existing output manifest is excluded from its own digest list.

The manifest lets a reviewer verify that the retained JSON evidence set has not changed accidentally after manifest creation. It is **not** a cryptographic signature, software attestation, SBOM signature, or proof of who produced the files.

## Maintained artifact bundles

### Portable dependency audit

Artifact name:

`dependency-audit`

Produced by `release-readiness.yml` and contains package/vulnerability reports for:

- `SwiftDrop.Core`;
- `SwiftDrop.Core.Tests`;
- `SwiftDrop.Benchmarks`;
- `manifest.json` covering the JSON reports in the bundle.

### Android app dependency audit

Artifact name:

`android-dependency-audit`

Contains:

- `packages.json` for `SwiftDrop.App` / `net10.0-android`;
- `vulnerabilities.json` for the same restored target graph;
- `manifest.json`.

### Windows app dependency audit

Artifact name:

`windows-dependency-audit`

Contains:

- `packages.json` for the focused `net10.0-windows10.0.19041.0` app graph used by hosted validation;
- `vulnerabilities.json` for that graph;
- `manifest.json`.

The hosted Windows compile uses the repository's unpackaged/source-compile boundary. Final signed MSIX/package review must still inspect the exact signed release output and its real packaging/runtime dependencies.

### Apple dependency audit

Artifact name:

`apple-dependency-audit`

Contains subdirectories for:

- `maccatalyst/` — containing app package/vulnerability reports;
- `ios-app/` — iOS containing app package/vulnerability reports;
- `ios-share-extension/` — iOS Share Extension package/vulnerability reports;
- a root `manifest.json` covering the report JSON files across those subdirectories.

The iOS reports are produced from certificate-independent simulator restore/build graphs. Final signed iOS/App Store evidence must still be reviewed after real provisioning, App Group configuration, extension embedding/signing, and archive/package creation.

## Platform-build versus release-readiness evidence

`platform-builds.yml` emits the shipped-target audit artifacts during maintained hosted platform validation. This gives ordinary source/platform changes early dependency evidence.

`release-readiness.yml` emits equivalent candidate-oriented evidence and additionally includes portable Core/test/benchmark reports. Its release gate requires all portable and platform jobs to succeed.

For a production release, retain the **release-readiness artifacts for the exact candidate commit/tag**. Do not substitute an older successful platform-build artifact merely because the dependency versions appear unchanged.

## Verifying an evidence manifest

A reviewer can independently recompute each listed SHA-256 digest with standard platform tools or a small script and compare it with `manifest.json`.

The manifest should be checked before the files are copied into long-term release evidence. If any report changes, regenerate the complete bundle from the exact candidate rather than editing a manifest by hand.

## Required release review

For the exact candidate:

1. identify and freeze the commit SHA/tag;
2. require the relevant release-readiness jobs to succeed;
3. download all dependency-audit artifacts;
4. verify their `manifest.json` files against the retained JSON bytes;
5. review direct and transitive package identities/versions;
6. confirm vulnerable-package reports contain no findings;
7. review package provenance, licenses, notices, redistribution obligations, and platform/runtime components;
8. compare source/restored-graph evidence with the final signed/package output;
9. update `THIRD_PARTY_NOTICES.md`/release materials if the exact shipped graph requires additional attribution;
10. retain evidence with the release record.

## Evidence limitations

A green audit report or matching SHA-256 manifest does not prove:

- absence of every possible vulnerability;
- absence of vulnerabilities unknown to NuGet's advisory sources at report time;
- safety of application logic;
- authenticity of an unsigned evidence bundle;
- correspondence between a hosted simulator/unpackaged build and a separately produced signed store package;
- license compliance by itself;
- successful physical-device, signing, notarization, App Group, firewall, accessibility, or store validation.

These limitations are why `docs/release/release-checklist.md`, `THIRD_PARTY_NOTICES.md`, and the manual test matrix remain required release inputs.

---

**Made by the Sanskar**
