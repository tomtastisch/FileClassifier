# Index - Detection

## 1. Zweck
Single Source of Truth (SSOT) fuer Dateitypen, Aliase, MIME-Metadaten und Header-Signaturen.

## 2. Datei
- [FileTypeRegistry.vb](./FileTypeRegistry.vb)

## 3. Kernfunktionen und Einsatz
| Funktion | Wann wird sie verwendet? | Warum |
|---|---|---|
| `DetectByMagic(header)` | in jeder Detektion nach Header-Read | erste deterministische Klassifikation |
| `Resolve(kind)` | nach erfolgreicher Klassifikation | kanonische Metadaten (Extension/MIME/Aliases) |
| `ResolveByAlias(alias)` | bei Alias-/Endungsnormalisierung | robustes Mapping ohne Duplikatlogik |
| `NormalizeAlias(raw)` | bei Endungs-/Aliasvergleich | case-insensitive, punkt-unabhaengige Normalisierung |
| `HasDirectHeaderDetection(kind)` | Policy-/Coverage-Pruefungen | erkennt reine Header-Matches |
| `HasStructuredContainerDetection(kind)` | OOXML-Refinement-Kontext | ZIP-Container-Typisierung |
| `KindsWithoutDirectContentDetection()` | Test-/Qualitaetsreporting | entdeckt Coverage-Luecken |

## 4. Datenmodell-Regeln
| Feld | Regel |
|---|---|
| `FileKind.Unknown` | immer fail-closed, `Allowed=False` |
| `CanonicalExtension` | Metadatum, kein Sicherheitsbeweis |
| `Aliases` | normalisiert, deterministisch, case-insensitive |
| `Mime` | informative Zuordnung, nicht sicherheitsentscheidend |

## 5. Ablaufdiagramm
```mermaid
flowchart TD
    H[Header Bytes] --> M[DetectByMagic]
    M -->|Unknown| U[Unknown]
    M -->|Direkter Typ| R[Resolve(kind)]
    M -->|Zip| Z[ZIP-Gate + Refinement ausserhalb Registry]
```

## 6. Testverknuepfungen
- [FileTypeRegistryUnitTests.cs](../../../tests/FileTypeDetectionLib.Tests/Unit/FileTypeRegistryUnitTests.cs)
- [HeaderCoveragePolicyUnitTests.cs](../../../tests/FileTypeDetectionLib.Tests/Unit/HeaderCoveragePolicyUnitTests.cs)

## 7. Siehe auch
- [Modulindex](../README.md)
- [Architektur und Ablaufe](../../../docs/02_ARCHITECTURE_AND_FLOWS.md)
- [Referenzen](../../../docs/03_REFERENCES.md)
