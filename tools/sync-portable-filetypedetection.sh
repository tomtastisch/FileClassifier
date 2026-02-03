#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="${ROOT_DIR}/src/FileTypeDetectionLib"
OUT_DIR="${ROOT_DIR}/portable/FileTypeDetection"

rm -rf "${OUT_DIR}"
mkdir -p "${OUT_DIR}/Abstractions" "${OUT_DIR}/Detection" "${OUT_DIR}/Infrastructure"

cp "${SRC_DIR}/FileTypeDetector.vb" "${OUT_DIR}/"
cp "${SRC_DIR}/FileTypeDetectorOptions.vb" "${OUT_DIR}/"
cp "${SRC_DIR}/Abstractions/FileKind.vb" "${OUT_DIR}/Abstractions/"
cp "${SRC_DIR}/Abstractions/FileType.vb" "${OUT_DIR}/Abstractions/"
cp "${SRC_DIR}/Detection/FileTypeRegistry.vb" "${OUT_DIR}/Detection/"
cp "${SRC_DIR}/Infrastructure/Internals.vb" "${OUT_DIR}/Infrastructure/"
cp "${SRC_DIR}/Infrastructure/MimeProvider.vb" "${OUT_DIR}/Infrastructure/"

cat > "${OUT_DIR}/README.md" <<'EOF'
# Portable FileTypeDetection (Deutsch)

Dieser Ordner ist bewusst copy/paste-freundlich aufgebaut und enthaelt nur
die benoetigten Quellcodedateien fuer `FileTypeDetector` (ohne Tests, ohne `bin/`, ohne `obj/`).

## Schnellstart im Zielprojekt
1. Ordner `FileTypeDetection` in dein Zielprojekt kopieren.
2. In der Ziel-`.vbproj` sicherstellen:
   - NuGet: `DocumentFormat.OpenXml` (3.4.1)
   - NuGet: `Mime` (3.8.0)
   - FrameworkReference: `Microsoft.AspNetCore.App`
3. Empfohlen: `<RootNamespace></RootNamespace>` setzen.
4. Projekt bauen.

## Dokumentstruktur
- [Abstractions/index.md](Abstractions/index.md) - fachliche Basistypen (`FileKind`, `FileType`)
- [Detection/index.md](Detection/index.md) - SSOT-Registry und Alias-Aufloesung
- [Infrastructure/index.md](Infrastructure/index.md) - Sicherheits- und Infrastrukturkomponenten

## Oeffentliche API (extern nutzbar)
### FileTypeDetector (Methoden)
| Methode | Zweck | Typischer Einsatz |
|---|---|---|
| `SetDefaultOptions(opt)` | Globale Optionen setzen (Snapshot). | Einmalig beim Start (z. B. strengere Limits). |
| `GetDefaultOptions()` | Aktuelle globale Optionen lesen (Kopie). | Vor Anpassungen/Debugging. |
| `LoadOptions(path)` | Optionen aus JSON laden (defensiv, mit Fallback). | Konfigurierbare Betriebsparameter aus Datei. |
| `ReadFileSafe(path)` | Datei bounded in Bytes lesen. | Sicheres Vorladen fuer `Detect(byte())`. |
| `Detect(path)` | Dateityp inhaltsbasiert bestimmen. | Standardfall fuer Pfadinput. |
| `Detect(path, verifyExtension)` | Erkennung + optionale Endungspruefung. | Bei Bedarf auf stricte Endungs-Konsistenz. |
| `DetectAndVerifyExtension(path)` | Nur Endungs-Validitaet gegen Inhalt pruefen. | Policy-/Compliance-Checks. |
| `Detect(data)` | Dateityp aus Byte-Array bestimmen. | Streams, Uploads, In-Memory-Workflows. |
| `IsOfType(data, kind)` | Shortcut fuer Typvergleich. | Schnelle Guard-Checks in Pipelines. |

### Wann nutze ich welche Methode?
1. Normalfall: `Detect(path)`
2. Strikte Endungspruefung: `Detect(path, verifyExtension:=True)`
3. Nur Endung validieren: `DetectAndVerifyExtension(path)`
4. In-Memory/Upload-Bytes: `Detect(data)` bzw. `IsOfType(data, kind)`
5. Sicherheitsgrenzen steuern: `LoadOptions(...)` + `SetDefaultOptions(...)`

## Architektur und Ablauf (Logik + Sicherheitsinstanzen)

### 1) Hauptablauf der Typentscheidung
#### 1.1 Vorentscheidung (Signatur + Fallback)
```mermaid
flowchart TD
    A[Input] --> B[Header lesen]
    B --> C{MagicDetect}
    C -->|Treffer != ZIP| D[Direkter Typ]
    C -->|Unknown oder ZIP| E[LibMagicSniffer]
    E --> F{ZIP-Kandidat?}
    F -->|Nein| G[Alias -> FileTypeRegistry]
    F -->|Ja| H[ZIP-Pfad]
```

#### 1.2 ZIP-Pfad (Sicherheitspruefung + Verfeinerung)
```mermaid
flowchart TD
    H[ZIP-Kandidat] --> I{ZipSafetyGate ok?}
    I -->|Nein| U[Unknown]
    I -->|Ja| J{OpenXmlRefiner}
    J -->|DOCX/XLSX/PPTX| K[OOXML-Typ]
    J -->|kein OOXML| L[Zip]
```

#### 1.3 Finale Policy (optionale Endungspruefung)
```mermaid
flowchart TD
    D[Direkter Typ] --> M{verifyExtension?}
    G[Alias-Typ] --> M
    K[OOXML-Typ] --> M
    L[Zip] --> M
    U[Unknown] --> M
    M -->|Nein| O[Ergebnis]
    M -->|Ja + Match| O
    M -->|Ja + Mismatch| U2[Unknown]
```

### 2) Sicherheitsinstanzen (Gate-Reihenfolge)
```mermaid
flowchart LR
    A[Input] --> B[MaxBytes / SniffBytes]
    B --> C[StreamBounds: bounded copy]
    C --> D[ZipSafetyGate]
    D --> E[OpenXmlRefiner]
    E --> F[Extension-Check optional]
    F --> G[Finales FileType]

    D -. Verletzt Limit .-> X[Unknown]
    B -. Fehler/Ungueltig .-> X
    C -. Ueberschreitung .-> X
    F -. Mismatch .-> X
```

## Unterordner-Indexe (normorientierte Detaildokumentation)
- [Abstractions/index.md](Abstractions/index.md)
- [Detection/index.md](Detection/index.md)
- [Infrastructure/index.md](Infrastructure/index.md)

## Grundlegende Sicherheitswirkungen der Umsetzung
- Zip-Bomb-Reduktion durch Limits fuer Entries, Groessen, Ratio und Nesting.
- Sicheres Byte-Handling ueber harte Bounded-I/O-Grenzen.
- Keine Codeausfuehrung bei Erkennung (Analyse auf Bytes/Metadatenebene).
- Fail-Closed bei Fehlern (`Unknown`/`False`).
- Optional stricte Endungs-Policy mit `verifyExtension`.

## Pflege / Aktualisierung des Portable-Ordners
Im Repo-Root ausfuehren: `./tools/sync-portable-filetypedetection.sh`
EOF

cat > "${OUT_DIR}/Abstractions/index.md" <<'EOF'
# Index - Abstractions

## 1. Zweck
Grundtypen der Dateityp-Erkennung: `FileKind` und `FileType`.

## 2. Dateien und Verantwortung
- `FileKind.vb`: kanonischer Enum-Katalog der unterstuetzten Typen.
- `FileType.vb`: unveraenderliches Metadatenobjekt pro erkannten Typ.

## 3. Normorientierte Regeln
1. `FileKind` ist die einzige fachliche Typ-ID.
2. `FileType` bleibt unveraenderlich (ReadOnly-Eigenschaften).
3. `Unknown` ist obligatorischer fail-closed Fallback.
EOF

cat > "${OUT_DIR}/Detection/index.md" <<'EOF'
# Index - Detection

## 1. Zweck
SSOT-Registry fuer Typmetadaten und Alias-Aufloesung.

## 2. Dateien und Verantwortung
- `FileTypeRegistry.vb`: `TypesByKind`, `KindByAlias`, `Resolve`, `ResolveByAlias`, `NormalizeAlias`.

## 3. Normorientierte Regeln
1. Neue Typen nur in `KnownTypeDefinitions()`.
2. Alias-Aufloesung ist deterministisch und case-insensitive.
3. Unbekannte Werte liefern `Unknown`.
EOF

cat > "${OUT_DIR}/Infrastructure/index.md" <<'EOF'
# Index - Infrastructure

## 1. Zweck
Technische und sicherheitsrelevante Hilfskomponenten fuer die Erkennung.

## 2. Dateien und Verantwortung
- `MimeProvider.vb`: MIME-Aufloesung und Backend-Toggle.
- `Internals.vb`: `StreamBounds`, `LibMagicSniffer`, `ZipSafetyGate`, `OpenXmlRefiner`, `LogGuard`.

## 3. Normorientierte Regeln
1. Fehler propagieren nicht als unsicheres Positivergebnis.
2. Bounded Processing ueber `FileTypeDetectorOptions`.
3. Logging darf Erkennung nicht beeinflussen.
EOF

echo "Portable sources refreshed: ${OUT_DIR}"
