# Tomtastisch.FileClassifier

## 1. Einstieg
Dieses Dokument ist der zentrale Einstiegspunkt für Nutzer und Entwickler.

## 2. Zielbild
Tomtastisch.FileClassifier liefert deterministische Dateityperkennung, sichere Archivverarbeitung und reproduzierbare Nachweise mit fail-closed Semantik.

## 3. Public API Surface
- `FileTypeDetector`: inhaltsbasierte Erkennung aus Pfad/Bytes, optional mit Endungsprüfung und Detailtrace.
- `ArchiveProcessing`: statische Fassade für Archiv-Validierung und sichere Memory-Extraktion.
- `FileMaterializer`: persistiert ausschließlich `Byte[]` (raw write oder sichere Archiv-Extraktion nach Zielpfad).
- `EvidenceHashing`: `HashFile`/`HashBytes`/`HashEntries` sowie `VerifyRoundTrip` mit deterministischer Evidence (optional mit HMAC-SHA256 über `HashOptions.IncludeSecureHash` und `FILECLASSIFIER_HMAC_KEY_B64`).
- `FileTypeOptions`: globaler Konfigurations-Snapshot für alle Pfade (wird von den Kernklassen gelesen).

## 4. Installation (NuGet)
- PackageId: `Tomtastisch.FileClassifier`
- Feeds:
  - NuGet.org (online such- und installierbar)
  - GitHub Packages (Repository Feed)
- Installation:
```bash
dotnet add package Tomtastisch.FileClassifier --version X.Y.Z
```
- `PackageReference`:
```xml
<ItemGroup>
  <PackageReference Include="Tomtastisch.FileClassifier" Version="X.Y.Z" />
</ItemGroup>
```
- SVT (Single Version Truth):
  - Release-Tag `vX.Y.Z` erzeugt NuGet-Version `X.Y.Z`.
  - CI erzwingt `git version == nupkg version == nuget version` über SVT-Gates.
- Verfügbarkeit/Konsistenz lokal prüfen:
```bash
EXPECTED_VERSION=X.Y.Z bash tools/ci/verify_nuget_release.sh
```
- Release-Ablauf: `Release Publish` blockiert post-publish auf `registration + flatcontainer`; die vollständige Online-Konvergenz inkl. `search` läuft asynchron über `NuGet Online Convergence` (Details: `docs/ci/002_NUGET_TRUSTED_PUBLISHING.MD`).
- Details: [NuGet Usage Guide](https://github.com/tomtastisch/FileClassifier/blob/main/docs/021_USAGE_NUGET.MD)
- Portable-Integration: [Portable Adoption Guide](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/003_GUIDE_PORTABLE.MD)
- Maintainer-Hinweis: Das Publish-Helper-Skript nutzt `NUGET_API_KEY` aus dem Keychain und gibt den Token nicht aus.
- Migration: Verwende ausschließlich `Tomtastisch.FileClassifier` (Details in `docs/guides/004_GUIDE_MIGRATE_LEGACY_NUGET.MD`).

## 5. Compatibility / TFMs
- Library-Zielplattformen: `netstandard2.0`, `net8.0` und `net10.0`
- Release-Versioning: Git-Tag `vX.Y.Z` (optional `-prerelease`) ist SSOT

## 6. Architekturüberblick
### 6.1 Kernklassen (Datenfluss)
| Kernklasse | Primäre Inputs | Primäre Outputs | Kernlogik |
|---|---|---|---|
| `FileTypeDetector` | `path`, `byte[]`, `verifyExtension` | `FileType`, `DetectionDetail`, `bool`, `IReadOnlyList<ZipExtractedEntry>` | Header/Magic (`FileTypeRegistry`) plus Archiv-Gate (`ArchiveTypeResolver` + `ArchiveSafetyGate`) und optionales OOXML-Refinement (`OpenXmlRefiner`). |
| `ArchiveProcessing` | `path`, `byte[]` | `bool`, `IReadOnlyList<ZipExtractedEntry>` | Fassade: path-basierte Validierung/Extraktion delegiert an `FileTypeDetector` (`TryValidateArchive` / `ExtractArchiveSafeToMemory`); byte-basierte Pfade nutzen `ArchivePayloadGuard` und `ArchiveEntryCollector`. |
| `FileMaterializer` | `byte[]`, `destinationPath`, `overwrite`, `secureExtract` | `bool` | Nur Byte-basierte Persistenz: raw write oder (bei `secureExtract=true` und archivfähigem Payload) sichere Extraktion via `ArchiveExtractor`. |
| `EvidenceHashing` | `path`, `byte[]`, `IReadOnlyList<ZipExtractedEntry>`, optionale Hash-Optionen | `HashEvidence`, `HashRoundTripReport` | Erkennung + Archivsammlung (`ArchiveEntryCollector`) und deterministische Manifest-/Payload-Hashes, inkl. RoundTrip über `FileMaterializer`. |

Hinweis zur Typdomäne: `DetectedType.Kind` ist nicht nur "Datei roh", sondern kann auch Archiv/Container-Typen tragen (`Zip`, `Docx`, `Xlsx`, `Pptx`).

### 6.2 Diagramm (kompakt)
```mermaid
flowchart LR

%% =========================
%% Spalte 1: NUR Kernklassen
%% =========================
subgraph C["Kernklassen"]
direction TB
  OPT["FileTypeOptions"]
  DET["FileTypeDetector"]
  AP["ArchiveProcessing"]
  MAT["FileMaterializer"]
  DH["EvidenceHashing"]
end

%% =========================================
%% Spalte 2: Output-Art (ohne Datentyp-Text)
%% =========================================
subgraph O["Output-Art"]
direction TB
  
  O_OPT_EXPORT["Export options (JSON)"]
  O_DET_READ["Read file safe (bounded)"]
  O_DET_DETECT["Detect type (path|bytes)"]
  O_DET_DETAIL["Detect detailed"]
  O_DET_EXT["Verify extension"]
  O_DET_ARCH["Validate archive (path)"]

  O_AP_VALIDATE["Validate archive (path|bytes)"]
  O_AP_EXTRACT["Extract to memory (path|bytes)"]

  O_MAT_PERSIST["Persist bytes (raw|secureExtract)"]

  O_DH_HASH["Hash (file|bytes|entries)"]
  O_DH_RTR["Verify round-trip"]
end

%% =========================
%% Spalte 3: Datentypen (LETZTE Spalte)
%% =========================
subgraph T["Datentypen"]
direction TB
  T_BOOL["Boolean"]
  T_BYTES["Byte[]"]
  T_STR["String (JSON)"]

  T_FILETYPE["FileType (inkl. Archive/Container-Kinds)"]
  T_DETAIL["DetectionDetail"]
  T_ENTRIES["IReadOnlyList<ZipExtractedEntry>"]

  T_HASH["HashEvidence"]
  T_RTR["HashRoundTripReport"]
end

%% =======
%% Kanten
%% =======

%% FileTypeOptions
OPT --> O_OPT_EXPORT --> T_STR

%% FileTypeDetector
DET --> O_DET_READ --> T_BYTES
DET --> O_DET_DETECT --> T_FILETYPE
DET --> O_DET_DETAIL --> T_DETAIL
DET --> O_DET_EXT --> T_BOOL
DET --> O_DET_ARCH --> T_BOOL

%% ArchiveProcessing
AP --> O_AP_VALIDATE --> T_BOOL
AP --> O_AP_EXTRACT --> T_ENTRIES

%% FileMaterializer
MAT --> O_MAT_PERSIST --> T_BOOL

%% EvidenceHashing
DH --> O_DH_HASH --> T_HASH
DH --> O_DH_RTR --> T_RTR

%% Options werden gelesen (Snapshot)
OPT -. snapshot/read .-> DET
OPT -. snapshot/read .-> AP
OPT -. snapshot/read .-> MAT
OPT -. snapshot/read .-> DH
```

Detaillierte Ablaufdiagramme liegen in [Architektur und Flows (Detail)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD).

Delegationshinweis: `ArchiveProcessing` ist bei path-basierten Archivpfaden eine Fassade auf `FileTypeDetector`; nur die byte-basierten Archivpfade laufen direkt über `ArchivePayloadGuard`/`ArchiveEntryCollector`.


## 7. Dokumentationspfad
- [Dokumentationsindex](https://github.com/tomtastisch/FileClassifier/blob/main/docs/001_INDEX_CORE.MD)
- [API-Kernübersicht](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
- [Audit Index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/000_INDEX.MD)
- [Security Assurance Index](https://github.com/tomtastisch/FileClassifier/blob/main/SECURITY_ASSURANCE_INDEX.md)
- [HMAC Key Setup (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/secure/001_HMAC_KEY_SETUP.MD)
- [Migration: Hashing Rename](https://github.com/tomtastisch/FileClassifier/blob/main/docs/migrations/001_HASHING_RENAME.MD)
- [Governance und Policies](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/001_POLICY_CI.MD)
- [Versioning-Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)

## Security Assurance & Evidence
- [Security Assurance Index](https://github.com/tomtastisch/FileClassifier/blob/main/SECURITY_ASSURANCE_INDEX.md)
- [Claim Traceability](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD)
- [Attestation Roadmap](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD)
- [Threat Model](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/007_THREAT_MODEL.MD)
- [Incident Response Runbook](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD)

## 8. Modul-READMEs
- [Bibliotheksmodul Index](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Detektion](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md)
- [Infrastruktur](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- [Konfiguration](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md)
- [Abstractions](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)

## 9. Verifikation
Alle Befehle werden vom Repository-Root ausgeführt.
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
python3 tools/check-docs.py
bash tools/audit/verify-security-claims.sh
bash tools/audit/generate-code-analysis-json.sh
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
python3 tools/check-policy-roc.py --out artifacts/policy_roc_matrix.tsv
bash tools/ci/bin/run.sh versioning-svt
bash tools/ci/bin/run.sh naming-snt
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --filter "Category=ApiContract" -v minimal
dotnet pack src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -o artifacts/nuget -v minimal
EXPECTED_VERSION=X.Y.Z bash tools/ci/verify_nuget_release.sh
EXPECTED_VERSION=X.Y.Z bash tools/ci/publish_nuget_local.sh
node tools/versioning/test-compute-pr-labels.js
```
