#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_ROOT="${ROOT_DIR}/src/FileTypeDetectionLib"
TEST_ROOT="${ROOT_DIR}/tests/FileTypeDetectionLib.Tests"

mkdir -p "${SRC_ROOT}/Abstractions" "${SRC_ROOT}/Detection" "${SRC_ROOT}/Infrastructure"
mkdir -p "${TEST_ROOT}/Features" "${TEST_ROOT}/Steps" "${TEST_ROOT}/Support" "${TEST_ROOT}/Unit" "${TEST_ROOT}/Property" "${TEST_ROOT}/Benchmarks"

# Enforce: only one README.md in the module root; subfolders use INDEX.md only.
find "${SRC_ROOT}" -mindepth 2 -type f -name "README.md" -delete
find "${TEST_ROOT}" -mindepth 2 -type f -name "README.md" -delete
rm -f "${SRC_ROOT}/INDEX.md" "${TEST_ROOT}/INDEX.md"

cat > "${SRC_ROOT}/README.md" <<'EOF'
# FileTypeDetectionLib - README

## Zweck
Deterministische Dateityp-Erkennung mit fail-closed Sicherheitsgrenzen.

## Oeffentliche API (wann und warum)
| API | Zweck | Wann verwenden |
|---|---|---|
| `Detect(path)` | Inhaltsbasierte Erkennung | Standardfall |
| `Detect(path, verifyExtension)` | Erkennung + Endungs-Policy | Strikte Dateiendungsregeln |
| `Detect(data)` | Erkennung aus Bytes | Upload/In-Memory |
| `ExtractZipSafe(path, dest, verify)` | Sicheres Entpacken | Kontrolliertes ZIP-Handling |
| `SetDefaultOptions` / `GetDefaultOptions` | Optionen global verwalten | App-Start / Diagnostik |
| `FileTypeSecurityBaseline.ApplyDeterministicDefaults()` | Konservatives Sicherheitsprofil | Einheitlicher Produktiv-Start |

## Ablauf (kurz)
```mermaid
flowchart TD
    A[Input] --> B[Magic + Sniffer]
    B --> C{ZIP-Kandidat?}
    C -->|Nein| D[Registry Resolve]
    C -->|Ja| E[ZipSafetyGate]
    E -->|Fail| U[Unknown]
    E -->|Pass| F[OpenXmlRefiner]
```

## Navigation
- [Abstractions/INDEX.md](Abstractions/INDEX.md)
- [Detection/INDEX.md](Detection/INDEX.md)
- [Infrastructure/INDEX.md](Infrastructure/INDEX.md)
EOF

cat > "${SRC_ROOT}/Abstractions/INDEX.md" <<'EOF'
# Index - Abstractions

## Zweck
Fachliche Basistypen fuer stabile Rueckgabewerte.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileKind.vb` | Kanonische Typ-ID fuer alle Erkennungsresultate. |
| `FileType.vb` | Unveraenderliches Ergebnisobjekt (`Kind`, `Mime`, `Allowed`, `Aliases`). |

## Verwendung
- Verwende `FileKind` fuer fachliche Entscheidungen im Anwendungsflow.
- Verwende `FileType` fuer Ausgabe, Logging und Weitergabe in Pipelines.
EOF

cat > "${SRC_ROOT}/Detection/INDEX.md" <<'EOF'
# Index - Detection

## Zweck
Single Source of Truth fuer Typdefinitionen und Aliasauflosung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileTypeRegistry.vb` | Typkatalog, Aliasnormalisierung, `Resolve`, `ResolveByAlias`. |

## Wann welche Funktion?
- `Resolve(kind)`: wenn bereits ein `FileKind` bestimmt wurde.
- `ResolveByAlias(alias)`: wenn Dateiendung/Alias in Typ ueberfuehrt werden soll.
- `NormalizeAlias(raw)`: vor jeder Aliasverarbeitung fuer deterministische Ergebnisse.
EOF

cat > "${SRC_ROOT}/Infrastructure/INDEX.md" <<'EOF'
# Index - Infrastructure

## Zweck
Sicherheits- und Infrastrukturkomponenten fuer Erkennung und ZIP-Verarbeitung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `Internals.vb` | Bounded-I/O, Sniffer-Adapter, ZIP-Gate, OOXML-Refinement, Logging-Guard. |
| `MimeProvider.vb` | Diagnose und Auswahl des MIME-Backends. |

## Sicherheitsbeitrag
1. Harte Byte-Grenzen gegen Ressourcen-DoS
2. ZIP-Validierung und sichere Extraktion ueber SSOT-Logik
3. Fail-closed Fehlerpfade (`Unknown`/`False`)
EOF

cat > "${TEST_ROOT}/README.md" <<'EOF'
# FileTypeDetectionLib.Tests - README

## Zweck
Menschenlesbare, deterministische Verifikation der Dateityp-Erkennung.

## Testarten
- BDD (Reqnroll, deutsch): `Features/` + `Steps/`
- Unit/Property: Sicherheitsgrenzen, adversariale ZIP-Faelle
- Benchmarks: trendbasierte Smoke-Messung

## Menschenlesbare BDD-Ausgabe in der Konsole
```bash
bash tools/test-bdd-readable.sh
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --logger "console;verbosity=detailed"
```

## Wann welchen Test starten?
- Voller Check vor Merge: `dotnet test FileClassifier.sln -v minimal`
- Nur BDD fuer Fachabnahme: `bash tools/test-bdd-readable.sh`
- Nur schnelle Unit/Property-Runde: `dotnet test ... --filter "FullyQualifiedName!~Features"`
EOF

cat > "${TEST_ROOT}/Features/INDEX.md" <<'EOF'
# Index - Features

## Zweck
Fachliche BDD-Szenarien in Gherkin (`# language: de`).

## Inhalte
- `file_type_detection.feature`: End-to-End-Erwartungen fuer Erkennung, Endungs-Policy und fail-closed Verhalten.
EOF

cat > "${TEST_ROOT}/Steps/INDEX.md" <<'EOF'
# Index - Steps

## Zweck
Reqnroll-Step-Bindings fuer die Feature-Saetze.

## Inhalte
- `FileTypeDetectionSteps.cs`: Given/When/Then-Implementierungen inkl. Optionen und Assertions.
EOF

cat > "${TEST_ROOT}/Support/INDEX.md" <<'EOF'
# Index - Support

## Zweck
Geteilte Test-Helfer, Zustandsverwaltung und BDD-Console-Hooks.

- `BddConsoleHooks.cs`
- `DetectionScenarioState.cs`
- `DetectorOptionsScope.cs`
- `TestAssemblyConfig.cs`
- `TestResources.cs`
- `ZipPayloadFactory.cs`

## Rollen
- Ressourcenaufloesung und Testdatenaufbau
- Optionen-Snapshot fuer deterministische Ruecksetzung
- menschenlesbare BDD-Console-Ausgabe
EOF

cat > "${TEST_ROOT}/Unit/INDEX.md" <<'EOF'
# Index - Unit

## Zweck
Direkte Funktions-, Regressions- und Sicherheitsfaelle.

- `ExtensionCheckUnitTests.cs`
- `FileTypeSecurityBaselineUnitTests.cs`
- `ZipAdversarialTests.cs`
- `ZipExtractionUnitTests.cs`

## Schwerpunkte
- Security-Baseline-Defaults
- sichere ZIP-Extraktion inkl. Traversal-Schutz
- adversariale ZIP-Faelle (Limits, Nested, fail-closed)
EOF

cat > "${TEST_ROOT}/Property/INDEX.md" <<'EOF'
# Index - Property

## Zweck
Grenzwert- und Eigenschaften-Tests fuer Sicherheitslimits.

## Inhalte
- `ZipGatePropertyTests.cs`: Verifikation von Entries-, Ratio-, Size- und Nested-Grenzen.
EOF

cat > "${TEST_ROOT}/Benchmarks/INDEX.md" <<'EOF'
# Index - Benchmarks

## Zweck
Smoke-Benchmarks ohne harte Timing-Grenzen.

## Inhalte
- `DetectionBenchmarkSmokeTests.cs`: Vergleich Header-lastiger vs. ZIP-lastiger Erkennungsfaelle.
EOF

echo "Documentation conventions synced for src/ and tests/."
