# Index - Property

## 1. Purpose
Eigenschaftsbasierte Verifikation von ZIP-Limits, Options-Invarianten und Materializer-Grenzen.

## 2. Inputs
- generierte/grenzwertige ZIP-Payloads
- deterministische numerische Optionswerte
- deterministische Byte-Payload-Laengen

## 3. Outputs
- Aussage zur Limit- und Invarianz-Stabilitaet

## 4. Failure Modes / Guarantees
- Limit-/Invarianz-Regressionen werden frueh erkannt.

## 5. Verification & Evidence
- `ZipGatePropertyTests.cs`
- `FileTypeOptionsPropertyTests.cs`
- `FileMaterializerPropertyTests.cs`
