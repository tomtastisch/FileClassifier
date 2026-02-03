# FileTypeDetectionLib.Tests - README

## Zweck
Menschenlesbare, deterministische Verifikation der Dateityp-Erkennung.

## Testarten
- BDD (Reqnroll, deutsch): `Features/` + `Steps/`
- Unit/Property: Sicherheitsgrenzen, adversariale ZIP-Faelle
- Benchmarks: trendbasierte Smoke-Messung

## Menschenlesbare BDD-Ausgabe in der Konsole
```bash
bash tools/test-bdd-readable.sh
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --logger "console;verbosity=detailed"
```

## Wann welchen Test starten?
- Voller Check vor Merge: `dotnet test FileClassifier.sln -v minimal`
- Nur BDD fuer Fachabnahme: `bash tools/test-bdd-readable.sh`
- Nur schnelle Unit/Property-Runde: `dotnet test ... --filter "FullyQualifiedName!~Features"`
