<!-- LANG_SWITCH:BEGIN -->
[DE](001_REFERENCES_CORE.MD) | [EN](101_REFERENCES_CORE.MD)
<!-- LANG_SWITCH:END -->

# 03 - Referenzen

## 1. Zweck & Scope
Dieses Dokument sammelt die statischen Referenzen zur API: interne Dateipfade, externe Abhängigkeiten, Rückgabemodelle und ReasonCodes.

## 2. Öffentliche Rückgabemodelle
| Typ | Öffentliche Member | Zweck |
|---|---|---|
| `FileKind` | Enum-Werte (u. a. `Unknown`, `Pdf`, `Zip`, `Docx`) | kanonische Typklassifikation |
| `FileType` | `Kind`, `CanonicalExtension`, `Mime`, `Allowed`, `Aliases` | inhaltsbasierte Typentscheidung |
| `DetectionDetail` | `DetectedType`, `ReasonCode`, `UsedZipContentCheck`, `UsedStructuredRefinement`, `ExtensionVerified` | auditierbares Detailergebnis |
| `ZipExtractedEntry` | `RelativePath`, `Content`, `Size`, `OpenReadOnlyStream()` | sicherer In-Memory-Extrakteintrag |
| `FileTypeProjectOptions` | `HeaderOnlyNonZip`, `MaxBytes`, `SniffBytes`, `MaxZipEntries`, `MaxZipTotalUncompressedBytes`, `MaxZipEntryUncompressedBytes`, `MaxZipCompressionRatio`, `MaxZipNestingDepth`, `MaxZipNestedBytes`, `RejectArchiveLinks`, `AllowUnknownArchiveEntrySize`, `DeterministicHash`, `Logger` | globales Optionsmodell |

## 2.1 Modellpfade im Repository (abstractions split)
| Bereich | README | Datei(en) |
|---|---|---|
| Detection | `src/FileTypeDetection/Abstractions/Detection/README.md` | `src/FileTypeDetection/Abstractions/Detection/FileKind.vb`, `src/FileTypeDetection/Abstractions/Detection/FileType.vb`, `src/FileTypeDetection/Abstractions/Detection/DetectionDetail.vb` |
| Archive | `src/FileTypeDetection/Abstractions/Archive/README.md` | `src/FileTypeDetection/Abstractions/Archive/ZipExtractedEntry.vb` |
| Hashing | `src/FileTypeDetection/Abstractions/Hashing/README.md` | `src/FileTypeDetection/Abstractions/Hashing/HashSourceType.vb`, `src/FileTypeDetection/Abstractions/Hashing/HashDigestSet.vb`, `src/FileTypeDetection/Abstractions/Hashing/HashEvidence.vb`, `src/FileTypeDetection/Abstractions/Hashing/HashRoundTripReport.vb`, `src/FileTypeDetection/Abstractions/Hashing/HashOptions.vb` |

## 3. ReasonCode-Referenz (DetectDetailed)
Quelle: `FileTypeDetector.vb` (Fallback) und `src/FileClassifier.CSCore/Utilities/DetectionPolicyUtility.cs` (delegierter CSCore-Pfad).

| ReasonCode | Bedeutung |
|---|---|
| `Unknown` | keine belastbare Erkennung |
| `FileNotFound` | Eingabedatei fehlt |
| `InvalidLength` | Dateilänge ist ungültig |
| `FileTooLarge` | `MaxBytes` wurde überschritten |
| `Exception` | Ausnahme im Detektionspfad |
| `ExceptionUnauthorizedAccess` | `UnauthorizedAccessException` im Detektionspfad |
| `ExceptionSecurity` | `System.Security.SecurityException` im Detektionspfad |
| `ExceptionIO` | `IOException` im Detektionspfad |
| `ExceptionInvalidData` | `InvalidDataException` im Detektionspfad |
| `ExceptionNotSupported` | `NotSupportedException` im Detektionspfad |
| `ExceptionArgument` | `ArgumentException` im Detektionspfad |
| `ExtensionMismatch` | Endung passt nicht zum erkannten Typ |
| `HeaderUnknown` | Header-Magic unzureichend/unbekannt |
| `HeaderMatch` | Header-Magic hat Typ direkt erkannt |
| `ArchiveGateFailed` | Archiv-Sicherheitsprüfung fehlgeschlagen |
| `ArchiveStructuredRefined` | Archiv wurde strukturiert (z. B. OOXML) verfeinert |
| `ArchiveRefined` | Archivtyp wurde inhaltlich verfeinert |
| `ArchiveGeneric` | Archiv blieb generisch |
| `OfficeBinaryRefined` | Legacy-OLE-Dokument wurde über Marker-Refinement als Office-Typ erkannt |

## 4. Interne Kernpfade (Leseführung)
| Interner Pfad | Datei | Bedeutung | Detail-README |
|---|---|---|---|
| Header/Typ-SSOT | `Detection/FileTypeRegistry.vb` | Header-Magic, Aliase, Canonical Extensions | [Detection Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md) |
| Core Guards | `Infrastructure/CoreInternals.vb` | Bounds, Security Gates, Path Guards | [Infrastructure Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md) |
| Managed archive internals | `Infrastructure/ArchiveManagedInternals.vb` | archivbasierte Iteration und Managed-Backend-Adapter (inkl. ZIP) | [Infrastructure Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md) |
| Archive internals | `Infrastructure/ArchiveInternals.vb` | Archiv-Dispatch, Entry-Adapter, Generic Extractor | [Infrastructure Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md) |
| MIME Auflösung | `Infrastructure/MimeProvider.vb` | MIME-Mapping | [Infrastructure Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md) |
| CSCore Runtime-Bridge | `Infrastructure/Utils/CsCoreRuntimeBridge.vb` | deterministische Delegation auf C#-Utilities mit VB-Fallback | CSCore Modul (ausserhalb dieses PR-Blocks) |
| CSCore Detection Policy | `src/FileClassifier.CSCore/Utilities/DetectionPolicyUtility.cs` | Endungs-Policy, Reason-Code-Mapping, Summary-Projektion | CSCore Utilities (ausserhalb dieses PR-Blocks) |
| CSCore Office Policy | `src/FileClassifier.CSCore/Utilities/OfficePolicyUtility.cs` | OpenXML/OpenDocument/Legacy-Office-Entscheidungslogik (kind-key-basiert) | CSCore Utilities (ausserhalb dieses PR-Blocks) |
| CSCore Evidence Policy | `src/FileClassifier.CSCore/Utilities/EvidencePolicyUtility.cs` | Label-/Notes-Policy und HMAC-Env-Key-Resolution fuer Hash-Evidence | CSCore Utilities (ausserhalb dieses PR-Blocks) |
| CSCore Archive/Path Policy | `src/FileClassifier.CSCore/Utilities/ArchivePathPolicyUtility.cs` | Relative-Archivpfad-Normalisierung und Root-Path-Policy fuer Zielpfade | CSCore Utilities (ausserhalb dieses PR-Blocks) |

## 5. Externe Abhängigkeiten
### 5.1 Diagramm
```mermaid
flowchart LR
    API["Public APIs"]
    INF["Infrastructure"]
    ZIP["System.IO.Compression"]
    MIME["Mime (HeyRed.Mime)"]
    RMS["Microsoft.IO.RecyclableMemoryStream"]
    SHARP["SharpCompress"]
    LOG["Microsoft.Extensions.Logging.Abstractions"]

    API --> INF
    INF --> ZIP
    INF --> MIME
    INF --> RMS
    INF --> SHARP
    API --> LOG
```

### 5.2 Tabelle
| Paket/Framework | Verwendet in | Zweck |
|---|---|---|
| `System.IO.Compression` (BCL) | `Infrastructure/CoreInternals.vb`, `Infrastructure/ArchiveManagedInternals.vb` | Archivdaten lesen/iterieren (ZIP-Backend via BCL) |
| `Mime` | `Infrastructure/MimeProvider.vb` | MIME-Auflösung aus Extension |
| `Microsoft.IO.RecyclableMemoryStream` | `Infrastructure/ArchiveManagedInternals.vb` | kontrollierte Memory-Streams |
| `SharpCompress` | `Infrastructure/ArchiveInternals.vb`, `FileMaterializer.vb` | Archive-Type-Erkennung, generische Archiv-Iteration, defensiver Lesbarkeits-Check |
| `Microsoft.Extensions.Logging.Abstractions` | `Configuration/FileTypeProjectOptions.vb`, `FileTypeOptions.vb`, `Infrastructure/CoreInternals.vb` | Logging-Interfaces fuer optionale Diagnostik ohne ASP.NET-FrameworkReference |
| `Riok.Mapperly` | `src/FileClassifier.CSCore/Mapping/*.cs` | compile-time Mapping-Generator fuer C#-Modelprojektionen |
| `PolySharp` | `src/FileClassifier.CSCore/FileClassifier.CSCore.csproj` | C# Sprach-/TFM-Kompatibilitaet fuer moderne Features |

## 6. Referenz-Index (wo finde ich was?)
| Thema | Primardokument |
|---|---|
| Public Signaturen + Beispiele | `docs/010_API_CORE.MD` |
| E2E-Architektur + Sequenzen | `docs/020_ARCH_CORE.MD` |
| Normative Anforderungen | `docs/specs/001_SPEC_DIN.MD` |
| Modul-Übersicht | [Modul-README: FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md) |
| Unterordner-Details | [Detection README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md), [Infrastructure README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md), [Configuration README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md), [Abstractions README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md), [Abstractions: Detection README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Detection/README.md), [Abstractions: Archive README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Archive/README.md), [Abstractions: Hashing README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Hashing/README.md), CSCore README (ausserhalb dieses PR-Blocks), [CLI README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.App/README.md) |

## 6.1 Change Playbooks
| Thema | Guide |
|---|---|
| Optionen anlegen/anpassen | `docs/guides/001_GUIDE_OPTIONS.MD` |
| Neue Datatypes/API-Modelle erweitern | `docs/guides/002_GUIDE_DATATYPE.MD` |
| Guide-Index | `docs/guides/000_INDEX_GUIDES.MD` |

## 7. Verifikationsreferenzen
Empfohlene Freigabe-Checks:
```bash
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --no-build -v minimal
```

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
