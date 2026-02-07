# 04 - DeterministicHashing API Contract

## 1. Zweck und Scope
Dieses Dokument friert den öffentlichen Vertrag von `DeterministicHashing` ein.
Es dient als Freigabebasis vor Merge und als Guard gegen unabsichtliche API-Drifts.

## 2. Vertragsprinzipien
- Deterministisch: gleicher Input + gleiche Optionen => gleiche Digests.
- Fail-closed: ungültige Eingaben liefern Nachweisobjekte mit `Unknown`/leeren Digests statt Ausnahmefluss.
- Security-SSOT:
  - `PhysicalSha256` = Rohbytes
  - `LogicalSha256` = kanonisierter Inhalt
- `Fast*XxHash3` ist optional und nicht-kryptografisch.
- Overloads ohne `options` verwenden `FileTypeOptions.GetSnapshot().DeterministicHash` als globale Default-Policy.

## 3. Public Surface (freigegeben)
| Methode | Rückgabe | Vertrag |
|---|---|---|
| `HashFile(path)` | `DeterministicHashEvidence` | Hash aus Datei, mit Archiv-Kanonisierung falls möglich |
| `HashFile(path, options)` | `DeterministicHashEvidence` | wie oben, optionsgesteuert |
| `HashBytes(data)` | `DeterministicHashEvidence` | Hash aus Payload |
| `HashBytes(data, label)` | `DeterministicHashEvidence` | wie oben, Label für Nachweis |
| `HashBytes(data, label, options)` | `DeterministicHashEvidence` | wie oben, optionsgesteuert |
| `HashEntries(entries)` | `DeterministicHashEvidence` | kanonischer Hash aus Entry-Liste |
| `HashEntries(entries, label)` | `DeterministicHashEvidence` | wie oben, Label für Nachweis |
| `HashEntries(entries, label, options)` | `DeterministicHashEvidence` | wie oben, optionsgesteuert |
| `VerifyRoundTrip(path)` | `DeterministicHashRoundTripReport` | h1-h4 Nachweis für Datei/Archiv |
| `VerifyRoundTrip(path, options)` | `DeterministicHashRoundTripReport` | wie oben, optionsgesteuert |

## 4. h1-h4 Semantik
- `h1`: Ausgangsinput (`HashFile`)
- `h2`: archiv-logische Einträge oder direkte Bytes
- `h3`: Hash auf kanonischen Bytes
- `h4`: Hash nach Materialisierung und Re-Read
- Strikter Gleichheitsvertrag:
  - `h1.logical == h2.logical == h3.logical == h4.logical`
  - `h1.physical` darf bei Archiven von `h2/h3/h4` abweichen.

## 5. Verifikation (Gate)
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "FullyQualifiedName~DeterministicHashingApiContractUnitTests" -v minimal
```

## 6. Freigabehinweis
Der Contract gilt als freigegeben, wenn:
1. Public-Surface-Tests grün sind,
2. h1-h4 Semantiktests grün sind,
3. CI Coverage Gates (line+branch) grün sind.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
