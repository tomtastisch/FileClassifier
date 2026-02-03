# D0) TARGET_ARCHITECTURE.md

## Zielmodell (kanonisch)

```mermaid
flowchart TD
    A[FileClassifier.App] --> B[FileTypeDetector API]
    B --> C[Detection Layer]
    B --> D[Zip Policy Layer]
    B --> E[Infrastructure Services]

    C --> C1[MagicDetect]
    C --> C2[FileTypeRegistry SSOT]
    C --> C3[OpenXmlRefiner]

    D --> D1[ZipSafetyGate Adapter]
    D --> D2[ZipExtractor Adapter]
    D1 --> D3[ZipProcessingEngine SSOT]
    D2 --> D3

    E --> E1[StreamBounds]
    E --> E2[MimeProvider/LibMagicSniffer]
    E --> E3[LogGuard]

    T[Tests] --> B
    T --> D
```

## Architekturprinzipien
1. **Facade only**: Oeffentliche API bleibt `FileTypeDetector`.
2. **SSOT fuer ZIP-Iteration**: genau eine Entry-Loop-Engine (`ZipProcessingEngine`).
3. **Adapter-Trennung**:
   - `ZipSafetyGate`: nur validieren
   - `ZipExtractor`: nur entpacken
4. **Fail-closed by default** in allen Schichten.
5. **Portable Sync**: `src` ist source-of-truth; `portable` wird generiert.

## Warum diese Struktur?
- Erhoehte Wartbarkeit ohne Redundanzverlust.
- Hoehere Testbarkeit auf Modulgrenzen.
- Deterministische Abhaengigkeitsrichtung (API -> Policies -> Infra).
