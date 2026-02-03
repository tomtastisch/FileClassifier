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
