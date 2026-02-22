# FileClassifier.CSCore Modul

## 1. Zweck
Dieses Verzeichnis enthaelt den C#-Core-Layer fuer compile-time optimierte Utility-, Mapping- und Modelllogik,
die vom VB-Kern ueber die Runtime-Bruecke genutzt wird.

## 2. Scope und Nicht-Ziele
- Scope:
  - Kapselung wiederverwendbarer, deterministischer Kernlogik in C#.
  - Boilerplate-Reduktion ueber `record`-Modelle, Auto-Properties und source-generated Mapper.
  - Security-konforme Fail-Closed-Nutzung ueber `CsCoreRuntimeBridge` (VB-Fallback bleibt aktiv).
- Nicht-Ziele:
  - Keine eigene Public API fuer NuGet-Konsumenten.
  - Keine direkte Entscheidungshoheit ausserhalb der vom VB-Core delegierten Pfade.

## 3. Struktur
- `Model/`: immutable Datentraeger fuer Snapshot- und Detection-Projektionen.
- `Mapping/`: compile-time Mapping (Mapperly) fuer explizite Projektionen/Klone.
- `Utilities/`: stateless Entscheidungs- und Normalisierungslogik.

## 4. Abhaengigkeiten
- `Riok.Mapperly` (Source Generator, compile-time Mapping)
- `PolySharp` (Language/TFM-Kompatibilitaet fuer moderne C#-Features)

## 5. Sicherheits- und Laufzeitmodell
- VB bleibt der produktive API-Rand.
- Delegation nach C# erfolgt ueber Reflection in `CsCoreRuntimeBridge`.
- Wenn C#-Assembly oder erwartete Members nicht verfuegbar sind:
  - deterministische Rueckgabe `False` in der Bridge,
  - anschliessend VB-Fallback (fail-closed).

## 6. Verifikation
```bash
dotnet restore src/FileClassifier.CSCore/FileClassifier.CSCore.csproj -v minimal
dotnet build src/FileClassifier.CSCore/FileClassifier.CSCore.csproj --no-restore -c Release -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --no-build -v minimal
```

## 7. Verweise
- [Library Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Runtime Bridge](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/Utils/CsCoreRuntimeBridge.vb)
- [Code Quality Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
