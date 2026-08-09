# Third-Party Notices

SwiftDrop is licensed under Apache-2.0. It also depends on third-party packages and platform SDK components that remain governed by their own licenses.

## Direct NuGet dependencies

Runtime projects currently reference:

- Microsoft.Data.Sqlite
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Maui.Controls
- Microsoft.Extensions.Logging.Debug
- QRCoder

The test project currently references:

- Microsoft.NET.Test.Sdk
- xunit
- xunit.runner.visualstudio
- coverlet.collector

## Platform SDKs

Building SwiftDrop can also use the Android SDK, Apple Xcode/iOS/macOS SDKs, the Windows App SDK, .NET, and .NET MAUI. Those SDKs are not redistributed by this repository merely because the source project targets them.

## Release requirement

Before publishing a binary release, generate the final dependency graph from the exact locked/restored package set, review each package's license and notice requirements, and include any attribution or license text required for redistribution. Do not treat this file as a substitute for that release-time dependency audit.

No third-party license grants rights to transferred user content.
