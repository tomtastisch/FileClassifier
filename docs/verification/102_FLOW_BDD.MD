# BDD Execution and Gherkin Flow

## 1. Purpose
This document describes the end-to-end flow of the BDD tests: from the Gherkin sentence through step binding to the executed domain method.

## 2. Scope
- Feature sentences under `tests/FileTypeDetectionLib.Tests/Features/`
- Step bindings in `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs`
- Execution via `tools/test-bdd-readable.sh`

## 3. Flow Model
```mermaid
flowchart TD
  A[".feature Satz (Given/When/Then)"] --> B["Reqnroll Matcher"]
  B --> C["Binding-Methode in FileTypeDetectionSteps.cs"]
  C --> D["Aufruf Fach-API (Detector/Archive/Hashing/Materializer)"]
  D --> E["Ergebnis im DetectionScenarioState speichern"]
  E --> F["Then-Assertion validiert Zustand"]
```

## 4. Gherkin -> Method -> Function
```mermaid
sequenceDiagram
  participant F as "Feature-Datei"
  participant R as "Reqnroll"
  participant S as "FileTypeDetectionSteps"
  participant A as "FileTypeDetection API"
  participant T as "xUnit Assertion"

  F->>R: "Wenn ich den Dateityp ermittle"
  R->>S: WhenIDetectTheFileType()
  S->>A: new FileTypeDetector().Detect(path)
  A-->>S: FileTypeResult
  S-->>R: state.LastResult setzen
  F->>R: "Dann ist der erkannte Typ \"Pdf\""
  R->>S: ThenTheDetectedKindIs("Pdf")
  S->>T: Assert.Equal(expected, state.LastResult.Kind)
```

## 5. Sentence Types and Responsibilities
| Sentence type | Responsibility target | Typical content |
|---|---|---|
| `Given` | set up initial state | load resource, create temp directory, set limits |
| `When` | execute domain action | detect, validate, extract, persist, hashing |
| `Then` | verify result | kind, bool result, file existence, hash consistency |

## 6. Why the Approach Is Deterministic
- Feature sentences are canonical and unambiguous.
- Each sentence maps to exactly one binding method.
- Result state is held centrally in `DetectionScenarioState`.
- Assertions validate only that state, not implicit side effects.

## 7. CI Integration
```mermaid
flowchart LR
  A["CI Job tests-bdd-coverage"] --> B["bash tools/test-bdd-readable.sh"]
  B --> C["dotnet test + Coverage Threshold"]
  C --> D["artifacts/tests/results.trx"]
  C --> E["artifacts/tests/bdd-readable.txt"]
  C --> F["artifacts/coverage/coverage.cobertura.xml"]
  C --> G["artifacts/coverage/coverage-summary.txt"]
```

## 8. Verification
```bash
bash tools/test-bdd-readable.sh
TEST_BDD_OUTPUT_DIR=artifacts/tests bash tools/test-bdd-readable.sh -- \
  /p:CollectCoverage=true \
  /p:Include="[FileTypeDetectionLib]*" \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="$(pwd)/artifacts/coverage/coverage" \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
```

## 9. Linked SSOT Sources
- [BDD catalog (German, canonical)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/103_CATALOG_BDD.MD)
- [CI pipeline (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/ci/101_PIPELINE_CI.MD)
- [FileTypeDetectionSteps](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs)
