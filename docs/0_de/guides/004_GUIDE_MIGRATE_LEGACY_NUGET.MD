<!-- LANG_SWITCH:BEGIN -->
[DE](004_GUIDE_MIGRATE_LEGACY_NUGET.MD) | [EN](../../1_en/guides/004_GUIDE_MIGRATE_LEGACY_NUGET.MD)
<!-- LANG_SWITCH:END -->

# Migrationsleitfaden: Legacy-NuGet-ID zum kanonischen Paket

## Geltungsbereich
Dieser Leitfaden beschreibt die Migration vom veralteten Paket `Tomtastisch.FileTypeDetection` auf das kanonische Paket `Tomtastisch.FileClassifier`.

## Warum
- Das Legacy-Paket ist veraltet und auf NuGet.org unlisted.
- Das kanonische Paket erhaelt laufende Updates und Policy-gestuetzte CI-Validierung.
- Der oeffentliche Root-Namespace ist auf `Tomtastisch.FileClassifier` vereinheitlicht (Level-3 Public-Surface-Rename).

## PackageReference-Migration
Ersetze:
```xml
<PackageReference Include="Tomtastisch.FileTypeDetection" Version="X.Y.Z" />
```
durch:
```xml
<PackageReference Include="Tomtastisch.FileClassifier" Version="X.Y.Z" />
```

CLI-Installationskommando:
```bash
dotnet add package Tomtastisch.FileClassifier --version X.Y.Z
```

## Namespace-Migration
Passe Source-Imports/Usings vom Legacy-Root-Namespace auf `Tomtastisch.FileClassifier` an.

## Unlist- und Deprecation-Policy
- Legacy-Versionen werden mit `dotnet nuget delete` unlisted (NuGet.org-Unlist-Semantik).
- Deprecation soll auf `Tomtastisch.FileClassifier` verweisen.
- Falls CLI-Deprecation nicht verfuegbar ist, Deprecation fuer alle Legacy-Versionen in der NuGet.org-UI setzen.
