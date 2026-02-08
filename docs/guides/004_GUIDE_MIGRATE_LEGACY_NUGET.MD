# Migration Guide: Legacy NuGet ID to Canonical Package

## Scope
This guide covers migration from deprecated package `Tomtastisch.FileTypeDetection` to canonical package `Tomtastisch.FileClassifier`.

## Why
- Legacy package is deprecated and unlisted on NuGet.org.
- Canonical package receives ongoing updates and policy-backed CI validation.
- Public root namespace is unified to `Tomtastisch.FileClassifier` (Level 3 public surface rename).

## PackageReference Migration
Replace:
```xml
<PackageReference Include="Tomtastisch.FileTypeDetection" Version="X.Y.Z" />
```
with:
```xml
<PackageReference Include="Tomtastisch.FileClassifier" Version="X.Y.Z" />
```

CLI install command:
```bash
dotnet add package Tomtastisch.FileClassifier --version X.Y.Z
```

## Namespace Migration
Update source imports/usings from legacy root namespace to `Tomtastisch.FileClassifier`.

## Unlist + Deprecate Policy
- Legacy versions are unlisted using `dotnet nuget delete` (NuGet.org unlist semantics).
- Deprecation should point users to `Tomtastisch.FileClassifier`.
- If CLI deprecation is unavailable, apply deprecation in NuGet.org UI for all legacy versions.
