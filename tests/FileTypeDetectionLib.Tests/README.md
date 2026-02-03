# FileTypeDetectionLib.Tests - DIN/NORM Teststruktur

## 1) Ziel und Geltungsbereich
Diese Test-Suite validiert die Basislogik der Dateityp-Erkennung deterministisch und fail-closed.
Der Fokus liegt auf BDD (Reqnroll/Gherkin, Sprache `de`) sowie auf eigenschaftsbasierten Grenzwerttests fuer ZIP-Gates.

## 2) Verzeichnisstruktur (kanonisch)
- `Features/` - fachliche Akzeptanzszenarien in Gherkin.
- `Steps/` - explizite Step-Bindings, keine komplexen Regex-Gruppen.
- `Support/` - geteilte Hilfen (Szenariozustand, Ressourcenauflosung, Testfabriken, Options-Scope).
- `Properties/` - eigenschafts- und grenzwertorientierte Regressionstests.
- `Performance/` - benchmark-orientierte Smoke-Tests ohne harte Timing-Grenzen.
- `resources/` - statische Testdateien (binaer), die zur Laufzeit kopiert werden.

## 3) Determinismus-Regeln
- Keine Netzwerkzugriffe.
- Keine Zeit-/Zufallsabhaengigkeit fuer fachliche Assertions.
- Globale Detector-Optionen werden je Testfall in den Originalzustand zurueckgesetzt.
- Testparallelisierung ist deaktiviert, um globalen Zustand deterministisch zu halten.
- Fail-closed Erwartungen sind explizit als `Unknown` modelliert.

## 4) Ausfuehrung (Pflicht)
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal
dotnet build FileClassifier.sln -v minimal
dotnet build FileClassifier.sln -v minimal -p:DefineConstants=USE_ASPNETCORE_MIME
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal -p:DefineConstants=USE_ASPNETCORE_MIME
```

## 5) Benchmark-Smoke (optional)
Der Benchmark-Smoke schreibt eine Messzeile in:
- `tests/FileTypeDetectionLib.Tests/bin/Debug/net10.0/benchmark_detect_ms.txt`

Die Messung dient der Trendbeobachtung. Es gibt keine harte Zeitgrenze.

## 6) Rollback (lokal, deterministisch)
Wenn eine Einzeldatei auf Baseline zurueckgesetzt werden soll:
```bash
cp /tmp/fileclassifier_baseline/src/FileTypeDetectionLib/FileTypeDetector.vb src/FileTypeDetectionLib/FileTypeDetector.vb
```
Analog fuer weitere Dateien mit identischem Muster.
