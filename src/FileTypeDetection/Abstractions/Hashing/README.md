# Index - Abstractions/Hashing

## 1. Zweck

Deterministische Hash-Evidence-Modelle für Physical/Logical-Nachweise und RoundTrip-Reports.

## 2. Dateien

- [DeterministicHashSourceType.vb](./DeterministicHashSourceType.vb)
- [DeterministicHashDigestSet.vb](./DeterministicHashDigestSet.vb)
- [DeterministicHashEvidence.vb](./DeterministicHashEvidence.vb)
- [DeterministicHashRoundTripReport.vb](./DeterministicHashRoundTripReport.vb)
- [DeterministicHashOptions.vb](./DeterministicHashOptions.vb)

## 3. Vertragsregeln

- `PhysicalSha256` und `LogicalSha256` sind SSOT für Integritätsnachweise.
- Fast-Hash-Felder sind optional und nicht-kryptografisch.
- RoundTrip-Report bildet h1-h4 konsistent für Audit/Tests ab.

## 4. Siehe auch

- [Abstractions-Index](../README.md)
- [API-Contract](../../../../docs/04_DETERMINISTIC_HASHING_API_CONTRACT.md)
- [Test-Matrix Hashing](../../../../docs/TEST_MATRIX_HASHING.md)

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
