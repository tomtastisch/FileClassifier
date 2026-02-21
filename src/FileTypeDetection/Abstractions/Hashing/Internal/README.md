# Abstractions Hashing Internal Modul

## 1. Zweck
Dieses Verzeichnis enthaelt interne, zustandslose Hashing-Bausteine hinter der oeffentlichen Fassade `EvidenceHashing`.

## 2. Inhalt
- `EvidenceHashingCore.vb`
- `EvidenceHashingRoundTrip.vb`
- `EvidenceHashingIO.vb`

## 3. API und Verhalten
- Keine Public API in diesem Verzeichnis.
- Fail-closed Fehlerpfade und deterministische Digest-Bildung werden zentral gekapselt.
- Die RoundTrip-Pipeline materialisiert temporaere Dateien und bereinigt best-effort.

## 4. Verifikation
- Nutzung wird ueber `EvidenceHashing` sowie Unit-/Integrationstests in `tests/FileTypeDetectionLib.Tests` verifiziert.

## 5. Diagramm
N/A

## 6. Verweise
- [Hashing-Abstractions](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Hashing/README.md)
- [Hashing-Contract](https://github.com/tomtastisch/FileClassifier/blob/main/docs/contracts/001_CONTRACT_HASHING.MD)
