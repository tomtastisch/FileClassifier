# Index - Property

## 1. Zweck
Eigenschaftsbasierte Verifikation von Archiv-Limits, Options-Invarianten und Materializer-Grenzen.

## 2. Eingaben
- generierte/grenzwertige Archiv-Payloads
- deterministische numerische Optionswerte
- deterministische Byte-Payload-Laengen

## 3. Ergebnisse
- Aussage zur Limit- und Invarianz-Stabilitaet

## 4. Fehlerfaelle und Garantien
- Limit-/Invarianz-Regressionen werden frueh erkannt.

## 5. Verifikation und Nachweise
- `ArchiveGatePropertyTests.cs`
- `FileTypeOptionsPropertyTests.cs`
- `FileMaterializerPropertyTests.cs`

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
