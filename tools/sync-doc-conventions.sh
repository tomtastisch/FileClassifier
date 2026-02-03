#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_ROOT="${ROOT_DIR}/src/FileTypeDetectionLib"
TEST_ROOT="${ROOT_DIR}/tests/FileTypeDetectionLib.Tests"

mkdir -p "${SRC_ROOT}/Abstractions" "${SRC_ROOT}/Detection" "${SRC_ROOT}/Infrastructure"
mkdir -p "${TEST_ROOT}/Features" "${TEST_ROOT}/Steps" "${TEST_ROOT}/Support" "${TEST_ROOT}/Unit" "${TEST_ROOT}/Property" "${TEST_ROOT}/Benchmarks"

# Rule: exactly one README.md at repository root. All subfolders use INDEX.md.
find "${ROOT_DIR}/src" "${ROOT_DIR}/tests" -type f -name "README.md" -delete
find "${ROOT_DIR}" -mindepth 2 -path "${ROOT_DIR}/portable/*" -prune -o -type f -name "README.md" -delete

cat > "${ROOT_DIR}/README.md" <<'DOC'
# FileClassifier

## 1. Ziel, Scope und Nicht-Ziele
### 1.1 Ziel
Deterministische, fail-closed Dateityp-Erkennung mit sicherer ZIP-Pruefung und sicherer ZIP-Extraktion.

### 1.2 Scope
- Content-basierte Erkennung (`Detect*`)
- ZIP-Sicherheits-Gate (Limits, nested, ratio)
- Sichere Extraktion auf Disk und in Memory
- Portable Kopierbarkeit nach `portable/FileTypeDetection`

### 1.3 Nicht-Ziele
- Vertrauen auf Dateinamen/Dateiendung als primaere Sicherheitsquelle
- "best effort"-Erkennung ohne fail-closed Verhalten

## 2. Sicherheitsprinzipien
- **Fail-closed:** Fehlerpfade liefern `Unknown` oder `False`.
- **Determinismus:** gleiche Eingabe -> gleiches Ergebnis.
- **Header-first:** Nicht-ZIP-Typen werden nur per Header erkannt (`HeaderOnlyNonZip=True`).
- **ZIP-Sonderregel:** ZIP-Header wird immer sicher inhaltlich geprueft und verfeinert.
- **Least trust:** Dateiname/Endung sind Metadaten, keine Vertrauensquelle.

## 3. Glossar
| Begriff | Definition |
|---|---|
| `Unknown` | fail-closed Rueckgabewert fuer nicht sicher zuordenbare Inhalte |
| Header-Erkennung | Typzuordnung ueber Magic-Bytes im Dateikopf |
| Struktur-Erkennung | Typzuordnung ueber sichere Container-Inhalte (z. B. OOXML-Marker) |
| ZIP-Gate | Sicherheitspruefung fuer ZIP-Container (Limits/Traversal/Nesting) |
| SSOT | Single Source of Truth; zentrale Definition ohne duplizierte Logik |
| `Allowed` | boolesches Policy-Flag im `FileType` (`Unknown=False`) |

## 4. Architekturuebersicht
```mermaid
flowchart TD
    A[Input: Path/Bytes] --> B[DetectByMagic]
    B --> C{ZIP-Header?}
    C -->|Nein| D[Direkter Header-Typ oder Unknown]
    C -->|Ja| E[ZipSafetyGate]
    E -->|Fail| U[Unknown]
    E -->|Pass| F[OpenXmlRefiner]
    F --> G[Docx/Xlsx/Pptx oder Zip]
```

## 5. Oeffentliche API
| API | Zweck | Sicherheitswirkung |
|---|---|---|
| `Detect(path)` | Inhaltserkennung auf Datei | kein Namensvertrauen |
| `Detect(path, verifyExtension)` | Erkennung + Endungs-Policy | optionale Policy-Haertung |
| `Detect(data)` | Inhaltserkennung fuer Bytes | geeignet fuer Upload-/Queue-Flows |
| `DetectAndVerifyExtension(path)` | Boolesche Endungspruefung | fail-closed bei Mismatch |
| `ExtractZipSafe(path, dest, verifyBeforeExtract)` | sichere ZIP-Extraktion auf Disk | Traversal-/Bomb-Schutz |
| `ExtractZipSafeToMemory(path, verifyBeforeExtract)` | sichere ZIP-Extraktion in Memory | keine persistente Speicherung |
| `FileTypeSecurityBaseline.ApplyDeterministicDefaults()` | konservative Defaults | reproduzierbare Baseline |

## 6. Abhaengigkeiten (Pflicht)
| Abhaengigkeit | Version | Zweck |
|---|---:|---|
| `DocumentFormat.OpenXml` | `3.4.1` | OOXML-Refinement |
| `Mime` | `3.8.0` | MIME-Metadatenaufloesung |
| `Microsoft.IO.RecyclableMemoryStream` | `3.0.1` | Memory-stabile Streamverarbeitung |
| `FrameworkReference: Microsoft.AspNetCore.App` | n/a | Logging und Runtime |

## 7. Betrieb und Verifikation
```bash
bash tools/sync-doc-conventions.sh
bash tools/sync-portable-filetypedetection.sh
dotnet test FileClassifier.sln -v minimal
bash tools/test-bdd-readable.sh
bash tools/check-portable-filetypedetection.sh --clean
```

## 8. Navigation
- `src/FileTypeDetectionLib/INDEX.md`
- `tests/FileTypeDetectionLib.Tests/INDEX.md`
- `portable/FileTypeDetection/INDEX.md`
DOC

cat > "${SRC_ROOT}/INDEX.md" <<'DOC'
# Index - src/FileTypeDetectionLib

## 1. Purpose
Deterministische Erkennung und sichere ZIP-Verarbeitung.

## 2. Inputs
- Datei- oder Byte-Input via `FileTypeDetector`
- Sicherheits-/Policy-Optionen via `FileTypeDetectorOptions`

## 3. Outputs
- `FileType` (`Kind`, `Allowed`, Metadaten)
- Sichere Extraktionsergebnisse (Disk/Memory)

## 3.1 Core-Dateien im Modul-Root
| Datei | Rolle |
|---|---|
| `FileTypeDetector.vb` | oeffentliche API |
| `ZipProcessing.vb` | zentrale ZIP-SSOT (Pruefung + Extraktion) |
| `FileTypeDetectorOptions.vb` | Sicherheits-/Policy-Optionen |
| `FileTypeSecurityBaseline.vb` | konservative Default-Konfiguration |

## 4. Failure Modes / Guarantees
- Fehler => fail-closed (`Unknown` / `False`)
- Keine Namens-basierte Vertrauensentscheidung

## 5. Verification & Evidence
- `dotnet test FileClassifier.sln -v minimal`
- `bash tools/test-bdd-readable.sh`
- `bash tools/check-portable-filetypedetection.sh --clean`

## 6. Architektur-Chart
```mermaid
flowchart LR
    API[FileTypeDetector] --> DET[Detection Registry]
    API --> INF[Infrastructure]
    INF --> DET
```
DOC

cat > "${SRC_ROOT}/Abstractions/INDEX.md" <<'DOC'
# Index - Abstractions

## 1. Purpose
Unveraenderliche Fachobjekte fuer stabile, portable Rueckgaben.

## 2. Inputs
- Werte aus Detection-/Infrastructure-Schicht

## 3. Outputs
- `FileKind`
- `FileType`
- `ZipExtractedEntry`

## 4. Failure Modes / Guarantees
- `Unknown` bleibt verpflichtender fail-closed Typ.
- Objekte sind immutable und serialisierbar nutzbar.

## 5. Verification & Evidence
- Unit-Tests in `tests/FileTypeDetectionLib.Tests/Unit/`
DOC

cat > "${SRC_ROOT}/Detection/INDEX.md" <<'DOC'
# Index - Detection

## 1. Purpose
SSOT fuer Typmetadaten, Policy-Regeln und Magic-Pattern.

## 2. Inputs
- Header-Bytes
- `FileKind`-Definitionen

## 3. Outputs
- `FileType`-Aufloesung (`Resolve*`)
- Header-/Content-Detektionsentscheidungen

## 4. Failure Modes / Guarantees
- Nicht erkennbare Inhalte => `Unknown`.
- Deterministische Normalisierung via `NormalizeAlias`.

## 5. Verification & Evidence
- `HeaderCoveragePolicyUnitTests.cs`
- `FileTypeRegistryUnitTests.cs`

## 6. Glossar
| Begriff | Definition |
|---|---|
| `HasDirectHeaderDetection` | direkter Header-Magic-Match vorhanden |
| `HasStructuredContainerDetection` | sichere Container-Spezifizierung (OOXML in ZIP) |
| `HasDirectContentDetection` | Header oder strukturierte Content-Erkennung |
| `Allowed` | Policy-Flag im `FileType` |

## 7. Parameter-/Policy-Tabelle
| Parameter | Quelle | Bedeutung | Regel |
|---|---|---|---|
| `Kind` | `FileKind` | kanonische Typ-ID | nur definierte Enum-Werte sind bekannt |
| `CanonicalExtension` | Registry | kanonische Endung | Metadatum, nicht Vertrauensquelle |
| `Aliases` | Registry | normalisierte Aliasnamen | deterministische, case-insensitive Normalisierung |
| `Mime` | `MimeProvider` | MIME-Metadatum | keine prim. Sicherheitsentscheidung |
| `Allowed` | `FileType` | boolesche Freigabe | `Unknown=False`, bekannte Typen `True` |
| `HasDirectHeaderDetection(kind)` | Registry | Header-signaturbasierte Erkennung | notwendig fuer direkte Nicht-ZIP-Typen |
| `HasStructuredContainerDetection(kind)` | Registry | strukturierte Content-Erkennung | `Docx/Xlsx/Pptx` via ZIP+Marker |
| `HasDirectContentDetection(kind)` | Registry | Header ODER strukturierte Erkennung | Vertrauensbasis fuer Typentscheidung |

## 8. Entscheidungsfluss
```mermaid
flowchart TD
    A[Header Bytes] --> B[DetectByMagic]
    B -->|Treffer != Zip| C[Direkter Typ]
    B -->|Zip| D[Container-Refinement]
    B -->|Unknown| U[Unknown]
```
DOC

cat > "${SRC_ROOT}/Infrastructure/INDEX.md" <<'DOC'
# Index - Infrastructure

## 1. Purpose
Sicherheitsnahe Stream-/ZIP-Infrastruktur.

## 2. Inputs
- Streams/Bytes aus API-Schicht
- Options-Limits

## 3. Outputs
- Validierte ZIP-Entscheidungen
- Sichere Extraktionsergebnisse

## 4. Failure Modes / Guarantees
- Traversal-/Bomb-Schutz aktiv
- deterministische Entry-Reihenfolge
- Ausnahmepfade fail-closed

## 5. Verification & Evidence
- `ZipAdversarialTests.cs`
- `ZipGatePropertyTests.cs`
- `ZipExtractionUnitTests.cs`

## 6. Sicherheitsfluss
```mermaid
flowchart TD
    A[ZIP Input] --> B[ZipSafetyGate]
    B -->|Fail| U[Unknown/False]
    B -->|Pass| C[ZipProcessingEngine]
    C --> D[Disk oder Memory Extraction]
```
DOC

cat > "${TEST_ROOT}/INDEX.md" <<'DOC'
# Index - tests/FileTypeDetectionLib.Tests

## 1. Purpose
Deterministische Verifikation von Sicherheit, Korrektheit und Regressionen.

## 2. Inputs
- Testressourcen (`resources/`)
- oeffentliche API der Library

## 3. Outputs
- Teststatus, BDD-Ausgabe, Regressionsevidenz

## 4. Failure Modes / Guarantees
- Fehlverhalten blockiert Pipeline direkt.
- Sicherheitsregeln werden als automatisierte Assertions erzwungen.

## 5. Verification & Evidence
- `dotnet test FileClassifier.sln -v minimal`
- `bash tools/test-bdd-readable.sh`
DOC

cat > "${TEST_ROOT}/Features/INDEX.md" <<'DOC'
# Index - Features

## 1. Purpose
Fachliche BDD-Spezifikation.

## 2. Inputs
- Gherkin-Szenarien

## 3. Outputs
- menschenlesbare Akzeptanzverifikation

## 4. Failure Modes / Guarantees
- Abweichende Fachlogik wird sofort sichtbar.

## 5. Verification & Evidence
- `file_type_detection.feature`
DOC

cat > "${TEST_ROOT}/Steps/INDEX.md" <<'DOC'
# Index - Steps

## 1. Purpose
Bindet Gherkin-Schritte an konkrete Testlogik.

## 2. Inputs
- Feature-Schritte

## 3. Outputs
- Testaktionen/Assertions

## 4. Failure Modes / Guarantees
- Inkonsistenzen zwischen Feature und Code schlagen fehl.

## 5. Verification & Evidence
- `FileTypeDetectionSteps.cs`
DOC

cat > "${TEST_ROOT}/Support/INDEX.md" <<'DOC'
# Index - Support

## 1. Purpose
Gemeinsame Test-Helfer fuer stabilen, deterministischen Testbetrieb.

## 2. Inputs
- Testzustand, Ressourcen, Options-Snapshots

## 3. Outputs
- reproduzierbare Testkontexte und BDD-Konsolenausgabe

## 4. Failure Modes / Guarantees
- Testisolation via Scope/State-Helfer

## 5. Verification & Evidence
- `DetectorOptionsScope.cs`
- `BddConsoleHooks.cs`
DOC

cat > "${TEST_ROOT}/Unit/INDEX.md" <<'DOC'
# Index - Unit

## 1. Purpose
Direkte Sicherheits-/Policy-/API-Verifikation.

## 2. Inputs
- API-Aufrufe, kontrollierte Payloads

## 3. Outputs
- deterministische Assertions auf Methodenebene

## 4. Failure Modes / Guarantees
- Policy-Verletzungen werden sofort als Testfehler gemeldet.

## 5. Verification & Evidence
- `ExtensionCheckUnitTests.cs`
- `HeaderCoveragePolicyUnitTests.cs`
- `HeaderDetectionWarningUnitTests.cs`
- `HeaderOnlyPolicyUnitTests.cs`
- `FileTypeRegistryUnitTests.cs`
- `FileTypeSecurityBaselineUnitTests.cs`
- `ZipAdversarialTests.cs`
- `ZipExtractionUnitTests.cs`
DOC

cat > "${TEST_ROOT}/Property/INDEX.md" <<'DOC'
# Index - Property

## 1. Purpose
Eigenschaftsbasierte Verifikation der ZIP-Limits.

## 2. Inputs
- generierte/grenzwertige ZIP-Payloads

## 3. Outputs
- Aussage zur Limit-Stabilitaet

## 4. Failure Modes / Guarantees
- Limit-Regressionen werden frueh erkannt.

## 5. Verification & Evidence
- `ZipGatePropertyTests.cs`
DOC

cat > "${TEST_ROOT}/Benchmarks/INDEX.md" <<'DOC'
# Index - Benchmarks

## 1. Purpose
Smoke-Benchmarks fuer relative Laufzeitentwicklung.

## 2. Inputs
- repraesentative Header-/ZIP-Faelle

## 3. Outputs
- trendbasierte Laufzeitbeobachtung

## 4. Failure Modes / Guarantees
- keine harten Performance-SLOs; Fokus auf Regressionstrends.

## 5. Verification & Evidence
- `DetectionBenchmarkSmokeTests.cs`
DOC

echo "Documentation conventions synced (single root README + INDEX-only subfolders)."
