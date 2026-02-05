# Index - Support

## 1. Zweck
Gemeinsame Test-Helfer fuer stabilen, deterministischen Testbetrieb.

## 2. Eingaben
- Testzustand, Ressourcen, Options-Snapshots

## 3. Ergebnisse
- reproduzierbare Testkontexte und BDD-Konsolenausgabe

## 4. Fehlerfaelle und Garantien
- Testisolation via Scope/State-Helfer

## 5. Verifikation und Nachweise
- `DetectorOptionsScope.cs`
- `BddConsoleHooks.cs`
- `FixtureManifestCatalog.cs` (Manifest-Load + Hash-Validierung)
- `TestResources.cs` (Lookup via `fixtureId` oder Dateiname)

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
