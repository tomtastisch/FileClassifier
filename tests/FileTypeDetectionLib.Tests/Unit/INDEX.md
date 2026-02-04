# Index - Unit

## Zweck
Direkter Nachweis von API-Korrektheit, Security-Regeln und Determinismus.

## Testabdeckung
| Testdatei | Fokus |
|---|---|
| `FileTypeRegistryUnitTests.cs` | deterministisches Typ-/Alias-Mapping |
| `HeaderCoveragePolicyUnitTests.cs` | Header-/Content-Coverage-Policy |
| `HeaderOnlyPolicyUnitTests.cs` | Policy-Verhalten fuer Non-ZIP/ZIP |
| `HeaderDetectionWarningUnitTests.cs` | Logging-/Warnpfade |
| `ExtensionCheckUnitTests.cs` | Endungsregeln und fail-closed |
| `FileTypeSecurityBaselineUnitTests.cs` | Baseline-Defaults |
| `DetectionDetailAndZipValidationUnitTests.cs` | `DetectDetailed` + `TryValidateZip` |
| `ZipProcessingFacadeUnitTests.cs` | statische ZIP-Fassade |
| `ZipAdversarialTests.cs` | adversarial ZIP-Faelle |
| `ZipExtractionUnitTests.cs` | sichere Disk-/Memory-Extraktion |
