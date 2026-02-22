<!-- LANG_SWITCH:BEGIN -->
[DE](001_CONTRACT_HASHING.MD) | [EN](101_CONTRACT_HASHING.MD)
<!-- LANG_SWITCH:END -->

# 04 - EvidenceHashing API Contract

## 1. Zweck und Scope
Dieses Dokument friert den öffentlichen Vertrag von `EvidenceHashing` ein.
Es dient als Freigabebasis vor Merge und als Guard gegen unabsichtliche API-Drifts.

## 2. Vertragsprinzipien
- Deterministisch: gleicher Input + gleiche Optionen => gleiche Digests.
- Fail-closed: ungültige Eingaben liefern Nachweisobjekte mit `Unknown`/leeren Digests statt Ausnahmefluss.
- Security-SSOT:
  - `PhysicalSha256` = Rohbytes
  - `LogicalSha256` = kanonisierter Inhalt
- `Fast*XxHash3` ist optional und nicht-kryptografisch.
- `Hmac*Sha256` ist optionaler keyed Digest (HMAC-SHA256) für Authentizität/Tamper-Evidence.
  - Aktivierung: `HashOptions.IncludeSecureHash = true`
  - Key: Environment Variable `FILECLASSIFIER_HMAC_KEY_B64` (Base64)
  - Wenn Key fehlt/ungültig: HMAC-Digests bleiben leer und Evidence-Notes enthalten einen Hinweis.
- Overloads ohne `options` verwenden `FileTypeOptions.GetSnapshot().DeterministicHash` als globale Default-Policy.

## 3. Public Surface (freigegeben)
| Methode | Rückgabe | Vertrag |
|---|---|---|
| `HashFile(path)` | `HashEvidence` | Hash aus Datei, mit Archiv-Kanonisierung falls möglich |
| `HashFile(path, options)` | `HashEvidence` | wie oben, optionsgesteuert |
| `HashBytes(data)` | `HashEvidence` | Hash aus Payload |
| `HashBytes(data, label)` | `HashEvidence` | wie oben, Label für Nachweis |
| `HashBytes(data, label, options)` | `HashEvidence` | wie oben, optionsgesteuert |
| `HashEntries(entries)` | `HashEvidence` | kanonischer Hash aus Entry-Liste |
| `HashEntries(entries, label)` | `HashEvidence` | wie oben, Label für Nachweis |
| `HashEntries(entries, label, options)` | `HashEvidence` | wie oben, optionsgesteuert |
| `VerifyRoundTrip(path)` | `HashRoundTripReport` | h1-h4 Nachweis für Datei/Archiv |
| `VerifyRoundTrip(path, options)` | `HashRoundTripReport` | wie oben, optionsgesteuert |

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
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "FullyQualifiedName~HashingEvidenceApiContractTests" -v minimal
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
