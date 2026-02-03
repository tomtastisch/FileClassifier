#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="${ROOT_DIR}/src/FileTypeDetectionLib"
OUT_DIR="${ROOT_DIR}/portable/FileTypeDetection"

rm -rf "${OUT_DIR}"
mkdir -p "${OUT_DIR}"

# Dynamically copy all VB sources from src/FileTypeDetectionLib preserving structure.
cd "${SRC_DIR}"
while IFS= read -r -d '' file; do
  rel="${file#./}"
  mkdir -p "${OUT_DIR}/$(dirname "${rel}")"
  cp "${file}" "${OUT_DIR}/${rel}"
done < <(find . \( -name bin -o -name obj \) -prune -o -type f -name "*.vb" -print0 | LC_ALL=C sort -z)

cat > "${OUT_DIR}/INDEX.md" <<'DOC'
# Index - portable/FileTypeDetection

## 1. Zweck
Portable, copy/paste-faehige Quellstruktur der Kernbibliothek ohne Build-Artefakte.

## 2. Voraussetzungen (Pflicht)
Siehe Root-`README.md`; benoetigte Abhaengigkeiten im Zielprojekt:
- `DocumentFormat.OpenXml` `3.4.1`
- `Mime` `3.8.0`
- `Microsoft.IO.RecyclableMemoryStream` `3.0.1`
- `FrameworkReference: Microsoft.AspNetCore.App`

## 3. Struktur
| Ordner/Datei | Inhalt |
|---|---|
| `FileTypeDetector.vb` | oeffentliche Detect-/Extract-API |
| `ZipProcessing.vb` | zentrale ZIP-SSOT (Pruefung + Extraktion) |
| `FileTypeDetectorOptions.vb` | Sicherheitslimits + Policy |
| `FileTypeSecurityBaseline.vb` | konservative Defaults |
| `Abstractions/` | Ergebnisobjekte |
| `Detection/` | Registry-SSOT |
| `Infrastructure/` | ZIP-/I/O-/MIME-Infrastruktur |

## 4. Portabilitaets-Check
```bash
bash tools/sync-portable-filetypedetection.sh
bash tools/check-portable-filetypedetection.sh --clean
```
DOC

mkdir -p "${OUT_DIR}/Abstractions" "${OUT_DIR}/Detection" "${OUT_DIR}/Infrastructure"

cat > "${OUT_DIR}/Abstractions/INDEX.md" <<'DOC'
# Index - Abstractions

## Dateien
- `DetectionDetail.vb`
- `FileKind.vb`
- `FileType.vb`
- `ZipExtractedEntry.vb`

## Verantwortung
- Stabile Ergebnisobjekte fuer Erkennung und sichere In-Memory-Extraktion.
DOC

cat > "${OUT_DIR}/Detection/INDEX.md" <<'DOC'
# Index - Detection

## Datei
- `FileTypeRegistry.vb`

## Parameter-/Policy-Tabelle
| Parameter | Bedeutung | Regel |
|---|---|---|
| `Kind` | kanonische Typ-ID | nur definierte Enum-Werte |
| `CanonicalExtension` | Endungs-Metadatum | kein Sicherheitsbeweis |
| `Aliases` | normalisierte Aliasliste | deterministisch normalisiert |
| `Allowed` | boolesche Freigabe | `Unknown=False`, bekannte Typen `True` |
| `HasDirectHeaderDetection` | Header-Signatur vorhanden | fuer Nicht-ZIP-Typen erforderlich |
| `HasStructuredContainerDetection` | Containerstruktur erkannt | `Docx/Xlsx/Pptx` via ZIP+Marker |
| `HasDirectContentDetection` | Header ODER strukturierte Erkennung | Vertrauensbasis fuer Typentscheidung |

## Fail-closed
- Nicht abgedeckte Inhalte => `Unknown`.
DOC

cat > "${OUT_DIR}/Infrastructure/INDEX.md" <<'DOC'
# Index - Infrastructure

## Dateien
- `Internals.vb`
- `MimeProvider.vb`
- `ZipProcessing.vb`

## Verantwortung
- Sicherheitskritische Stream- und ZIP-Verarbeitung in deterministischen Pfaden.
DOC

# Portable rule: README.md only at module root (portable/*), subfolders keep INDEX.md.
cp "${OUT_DIR}/INDEX.md" "${OUT_DIR}/README.md"

echo "Portable sources refreshed dynamically: ${OUT_DIR}"
