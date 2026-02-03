#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="${ROOT_DIR}/src/FileTypeDetectionLib"
OUT_DIR="${ROOT_DIR}/portable/FileTypeDetection"

rm -rf "${OUT_DIR}"
mkdir -p "${OUT_DIR}/Abstractions" "${OUT_DIR}/Detection" "${OUT_DIR}/Infrastructure"

cp "${SRC_DIR}/FileTypeDetector.vb" "${OUT_DIR}/"
cp "${SRC_DIR}/FileTypeDetectorOptions.vb" "${OUT_DIR}/"
cp "${SRC_DIR}/FileTypeSecurityBaseline.vb" "${OUT_DIR}/"
cp "${SRC_DIR}/Abstractions/FileKind.vb" "${OUT_DIR}/Abstractions/"
cp "${SRC_DIR}/Abstractions/FileType.vb" "${OUT_DIR}/Abstractions/"
cp "${SRC_DIR}/Detection/FileTypeRegistry.vb" "${OUT_DIR}/Detection/"
cp "${SRC_DIR}/Infrastructure/Internals.vb" "${OUT_DIR}/Infrastructure/"
cp "${SRC_DIR}/Infrastructure/MimeProvider.vb" "${OUT_DIR}/Infrastructure/"

cat > "${OUT_DIR}/README.md" <<'EOF'
# Portable FileTypeDetection

Copy/paste-freundliche Struktur der Kernbibliothek (ohne Tests, ohne `bin/`, ohne `obj/`).

## 1) Schnellstart
1. Ordner `FileTypeDetection` in das Zielprojekt kopieren.
2. In der Ziel-`.vbproj` sicherstellen:
   - `DocumentFormat.OpenXml` `3.4.1`
   - `Mime` `3.8.0`
   - `Microsoft.IO.RecyclableMemoryStream` `3.0.1`
   - `FrameworkReference` auf `Microsoft.AspNetCore.App`
3. Empfohlen: `<RootNamespace></RootNamespace>`.
4. Projekt bauen.

## 2) Oeffentliche API (wann und warum)
| API | Zweck | Wann verwenden | Warum |
|---|---|---|---|
| `Detect(path)` | Inhaltsbasierte Dateityp-Erkennung | Standardfall fuer Dateien auf Disk | Header + Fallback + ZIP-Gate |
| `Detect(path, verifyExtension)` | Erkennung + optionale Endungs-Policy | Wenn Dateiendung sicherheitsrelevant ist | Fail-closed bei Mismatch |
| `Detect(data)` | Erkennung fuer In-Memory-Daten | Upload-Bytes, Queue-Workflows | Kein Dateisystem notwendig |
| `DetectAndVerifyExtension(path)` | Endungsvalidierung gegen Erkennung | Compliance/Policy-Pruefungen | Ergebnis als Bool |
| `ReadFileSafe(path)` | Begrenztes Datei-Einlesen | Vor `Detect(data)` | Bounded-I/O gegen DoS |
| `ExtractZipSafe(path, dest, verify)` | Sicheres ZIP-Entpacken | Kontrolliertes Entpacken in neue Zielstruktur | Traversal-Block, Limits, atomic stage |
| `FileTypeSecurityBaseline.ApplyDeterministicDefaults()` | Harte Sicherheitsdefaults global setzen | App-Startup | Einheitliches, reproduzierbares Profil |

## 3) Ablauf und Sicherheitsinstanzen
### 3.1 Typentscheidung
```mermaid
flowchart TD
    A[Input: Path/Bytes] --> B[Header lesen]
    B --> C{MagicDetect}
    C -->|Treffer != ZIP| D[Direkter Typ]
    C -->|Unknown/ZIP| E[Sniffer Alias]
    E --> F{ZIP-Kandidat?}
    F -->|Nein| G[Registry Alias -> FileType]
    F -->|Ja| H[ZipSafetyGate]
    H -->|Fail| U[Unknown]
    H -->|Pass| I[OpenXmlRefiner]
    I -->|Docx/Xlsx/Pptx| O[OOXML Typ]
    I -->|kein OOXML| Z[Zip]
```

### 3.2 Sicheres Entpacken
```mermaid
flowchart TD
    A[ExtractZipSafe] --> B{verifyBeforeExtract}
    B -->|True| C[Detect(path)]
    C --> D{Zip/OOXML?}
    D -->|Nein| X[False]
    D -->|Ja| E[TryExtractZipStream]
    B -->|False| E
    E --> F[ProcessZipStream SSOT]
    F --> G[Entry-Checks + Path-Checks]
    G --> H[Stage-Directory schreiben]
    H --> I[Atomarer Move]
    I --> J[True]
```

## 4) Sicherheitswirkungen
- fail-closed bei Fehlern (`Unknown` / `False`)
- ZIP-Limits: Entries, Gesamtgroesse, Entry-Groesse, Ratio, Nesting
- Path-Traversal-Blockade beim Entpacken
- deterministic ordering bei ZIP-Entry-Verarbeitung
- stream-bounded copy fuer harte Byte-Grenzen

## 5) Dokumentnavigation
- [Abstractions/INDEX.md](Abstractions/INDEX.md)
- [Detection/INDEX.md](Detection/INDEX.md)
- [Infrastructure/INDEX.md](Infrastructure/INDEX.md)

## 6) Pflege
- Portable-Struktur neu erzeugen: `./tools/sync-portable-filetypedetection.sh`
- Portable Smoke-Check: `./tools/check-portable-filetypedetection.sh --clean`
EOF


cat > "${OUT_DIR}/Abstractions/INDEX.md" <<'EOF'
# Index - Abstractions

## Zweck
Definiert die fachlichen Basistypen, die als stabile, oeffentliche Ergebnisse der Bibliothek dienen.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileKind.vb` | Kanonischer Typkatalog (`Unknown`, Medienformate, ZIP/OOXML). |
| `FileType.vb` | Unveraenderliche Metadaten (`Kind`, Extension, Mime, Allowed, Aliases). |

## Regeln
1. `FileKind` ist die einzige fachliche Typ-ID.
2. `FileType` bleibt immutable.
3. `Unknown` bleibt verpflichtender fail-closed Rueckgabewert.
4. Keine I/O- oder Netzwerkabhaengigkeit in diesem Ordner.
EOF

cat > "${OUT_DIR}/Detection/INDEX.md" <<'EOF'
# Index - Detection

## Zweck
SSOT fuer Typdefinitionen und Aliasauflosung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileTypeRegistry.vb` | Typdefinitionen, Aliasnormalisierung, `Resolve`, `ResolveByAlias`. |

## Schluesselfunktionen
- `NormalizeAlias(raw)`
- `Resolve(kind)`
- `ResolveByAlias(aliasKey)`

## SSOT-Regel
- Typ- und Aliasdaten werden ausschliesslich hier gepflegt.
EOF

cat > "${OUT_DIR}/Infrastructure/INDEX.md" <<'EOF'
# Index - Infrastructure

## Zweck
Sicherheits- und Infrastrukturkomponenten fuer Erkennung und ZIP-Verarbeitung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `Internals.vb` | `StreamBounds`, `LibMagicSniffer`, `ZipSafetyGate`, `OpenXmlRefiner`, `LogGuard`. |
| `MimeProvider.vb` | MIME-Aufloesung und Backend-Diagnostik. |

## Sicherheitsrelevante Punkte
1. ZIP-Sicherheitspruefung + Extraktion teilen dieselbe Kernlogik
2. Path-Traversal-Blockade beim Entpacken
3. Nested-ZIP-Limits und bounded copy
4. Atomare Zielverzeichnis-Erstellung (stage + move)
EOF

echo "Portable sources refreshed: ${OUT_DIR}"
