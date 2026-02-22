# CSCore Utilities Modul

## 1. Zweck
Zentrale stateless Utility-SSOT fuer wiederkehrende Guard-, Normalisierungs-, Materialisierungs- und Detection-Policy-Logik.

## 2. Inhalt
- `EnumUtility.cs`: deterministische Enum-Wertauflistung mit Sortierung/Slicing.
- `IterableUtility.cs`: defensive Array-Kopie.
- `GuardUtility.cs`: Argument-/Enum-/Laengenpruefungen.
- `ExceptionFilterUtility.cs`: standardisierte Exception-Filtermengen.
- `HashNormalizationUtility.cs`: Digest- und Dateiname-Normalisierung.
- `MaterializationUtility.cs`: Payload-Limit und Materialisierungsmodus.
- `ProjectOptionsUtility.cs`: Normalisierung von Option-Snapshots.
- `DetectionPolicyUtility.cs`: Endungs-Policy, Reason-Code-Mapping, Detection-Summary-Projektion.
- `OfficePolicyUtility.cs`: OpenXML/OpenDocument/Legacy-Office-Policy-Entscheidungen (kind-key-basiert, fail-closed).
- `EvidencePolicyUtility.cs`: Label-/Notes-Policy und HMAC-Env-Key-Resolution fuer Hash-Evidence.
- `ArchivePathPolicyUtility.cs`: Relative-Archivpfad-Normalisierung und Root-Path-Policy fuer Zielpfade.

## 3. Security-Charakteristik
- Utility-Funktionen sind deterministisch und ohne versteckte I/O-Seiteneffekte.
- Fail-Closed-Nutzung erfolgt ueber `CsCoreRuntimeBridge`; bei Delegationsausfall greift VB-Fallback.

## 4. Verweise
- [CSCore Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.CSCore/README.md)
- [Runtime Bridge (VB)](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/Utils/CsCoreRuntimeBridge.vb)
