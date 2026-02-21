# FileTypeDetection Modul

## 1. Zweck
Dieses Verzeichnis stellt die öffentliche Bibliotheksoberfläche für Dateityperkennung, sichere Archivverarbeitung, deterministische Hashing-Nachweise und Byte-Materialisierung bereit.

## 2. Inhalt
- Öffentliche API-Einstiegspunkte: `FileTypeDetector`, `ArchiveProcessing`, `FileMaterializer`, `FileTypeOptions`, `EvidenceHashing`.
- Submodule für Modellklassen, Registry/Detection, Konfiguration, Infrastruktur, Provider-Abstraktionen und TFM-spezifische Provider.

## 3. API und Verhalten
- `FileTypeDetector`: Typdetektion aus Pfad/Bytes, Detailnachweise und sichere Archivpfade.
- `ArchiveProcessing`: statische Fassade für Validierung/Extraktion.
- `FileMaterializer`: persistiert Byte-Payloads, optional sichere Archiv-Materialisierung.
- `EvidenceHashing`: Physical/Logical Hash-Evidence und RoundTrip-Reports.

## 4. Verifikation
- Unit/Integration/BDD-Nachweise liegen unter dem Testprojekt.
- Dokumentations- und Link-Gates laufen über die zentralen Tools.

## 5. Diagramm
```mermaid
flowchart LR
    A[Consumer Input] --> B[Public API]
    B --> C[Detection and Archive Safety]
    C --> D[Typed Result or Persisted Output]
```

## 6. Verweise
- [Dokumentationsindex](https://github.com/tomtastisch/FileClassifier/blob/main/docs/001_INDEX_CORE.MD)
- [API-Kernübersicht](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
- [Audit Index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/000_INDEX.MD)
- [Detektion-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md)
- [Infrastruktur-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- [Infrastructure.Utils-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/Utils/README.md)
- [Konfiguration-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md)
- [Abstractions-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Composition-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Composition/README.md)
- [Providers-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Providers/README.md)

## 7. Provenance Verification
```bash
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
dotnet nuget verify "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```
