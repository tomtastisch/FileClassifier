# Abstractions Hashing Modul

## 1. Zweck
Dieses Verzeichnis bündelt Modelle für deterministische Hash-Evidence und RoundTrip-Nachweise.

## 2. Inhalt
- `HashSourceType.vb`
- `HashDigestSet.vb`
- `HashEvidence.vb`
- `HashRoundTripReport.vb`
- `HashOptions.vb`

## 3. API und Verhalten
- Physical/Logical SHA-256 sind die zentralen Integritätsnachweise.
- Optionaler FastHash bleibt nicht-kryptografisch.

## 4. Verifikation
- Unit- und Integrationstests prüfen Stage-Konsistenz und RoundTrip-Verhalten.

## 5. Diagramm
```mermaid
flowchart LR
    A[Hash Input] --> B[Digest Set]
    B --> C[Evidence]
    C --> D[RoundTrip Report]
```

## 6. Verweise
- [Abstractions-Übersicht](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Hashing-Contract](https://github.com/tomtastisch/FileClassifier/blob/main/docs/contracts/001_CONTRACT_HASHING.MD)
- [Hashing-Testmatrix](https://github.com/tomtastisch/FileClassifier/blob/main/docs/tests/004_MATRIX_HASHING.MD)
