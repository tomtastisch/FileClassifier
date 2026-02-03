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
- `DetectionDetailAndZipValidationUnitTests.cs`
- `HeaderCoveragePolicyUnitTests.cs`
- `HeaderDetectionWarningUnitTests.cs`
- `HeaderOnlyPolicyUnitTests.cs`
- `FileTypeRegistryUnitTests.cs`
- `FileTypeSecurityBaselineUnitTests.cs`
- `ZipAdversarialTests.cs`
- `ZipExtractionUnitTests.cs`
