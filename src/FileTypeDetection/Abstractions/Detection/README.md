# Index - Abstractions/Detection

## 1. Zweck
Detektions-Rueckgabemodelle der Public API.

## 2. Dateien
- [FileKind.vb](./FileKind.vb)
- [FileType.vb](./FileType.vb)
- [DetectionDetail.vb](./DetectionDetail.vb)

## 3. Vertragsregeln
- `FileKind.Unknown` bleibt fail-closed Fallback.
- Modelle sind immutable ausgelegt und enthalten keine I/O-Logik.
- `DetectionDetail` ist der auditierbare Trace fuer `DetectDetailed(...)`.

## 4. Siehe auch
- [Abstractions-Index](../README.md)
- [Funktionsreferenz](../../../../docs/01_FUNCTIONS.md)
