# FileClassifier

## 1. Einstieg
Dieses Dokument ist der zentrale Einstiegspunkt für Nutzer und Entwickler.

## 2. Zielbild
FileClassifier bietet deterministische Dateityperkennung, sichere Archivverarbeitung und reproduzierbare Hash-Nachweise mit fail-closed Semantik.

## 3. Public API Surface
- `FileTypeDetector`: erkennt Dateitypen aus Pfad oder Bytes.
- `ArchiveProcessing`: validiert/extrahiert Archive über eine statische Fassade.
- `FileMaterializer`: persistiert Byte-Payloads und materialisiert Archive sicher auf Disk.
- `FileTypeOptions`: lädt/liest globale Laufzeitoptionen.
- `DeterministicHashing`: erzeugt Physical/Logical Evidence und RoundTrip-Berichte.

### FileMaterializer (prominent)
`FileMaterializer` ist die zentrale Persistenz-API für Payloads und sichere Archiv-Materialisierung.
- Raw-Persistenz: schreibt Byte-Daten fail-closed.
- Secure-Extract: validiert Archivpayloads und extrahiert nur sichere Inhalte.

## 4. Dokumentationspfad
- [Dokumentationsindex](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/001_INDEX_CORE.MD)
- [API-Kernübersicht](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/010_API_CORE.MD)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/020_ARCH_CORE.MD)
- [Policy und Governance](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/governance/001_POLICY_CI.MD)
- [Versioning-Policy](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/versioning/001_POLICY_VERSIONING.MD)

## 5. Modul-READMEs
- [FileTypeDetection Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/README.md)
- [Detektion](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Detection/README.md)
- [Infrastruktur](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Infrastructure/README.md)
- [Konfiguration](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Configuration/README.md)
- [Abstractions](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Abstractions/README.md)

## 6. Verifikation
```bash
python3 tools/check-docs.py
python3 tools/check-policy-roc.py --out artifacts/policy_roc_matrix.tsv
bash tools/versioning/check-versioning.sh
node tools/versioning/test-compute-pr-labels.js
```
