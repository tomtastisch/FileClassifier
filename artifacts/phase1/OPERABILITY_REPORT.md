# B0) OPERABILITY_REPORT.md

## Scope
- Phase 1: Vollstaendigkeits- und Umgaenglichkeitstest (Operability).
- Bewertet wurden Build/Test/Run-Pfade, Doku-Einstieg, externe Abhaengigkeiten und Fehlermeldungen.

## Happy Path Check (evidenzbasiert)

1) Build funktioniert lokal
- Command: `dotnet build FileClassifier.sln -v minimal`
- Ergebnis: erfolgreich, `0 Warnung(en)`, `0 Fehler`
- Evidenz: `artifacts/phase1/COMMANDS_RUN.md`

2) Tests funktionieren lokal
- Command: `dotnet test FileClassifier.sln -v minimal`
- Ergebnis: `38` erfolgreich, `0` Fehler
- Evidenz: `artifacts/phase1/COMMANDS_RUN.md`

3) App-Run ist nutzbar
- Ohne Argument: klare Usage + Exit `2`
  - Evidenz: `src/FileClassifier.App/Program.cs:4-8` und Run-Output in `artifacts/phase1/COMMANDS_RUN.md`
- Mit Dateiargument: gibt erkannten Typ aus (`Pdf`) + Exit `0`
  - Evidenz: `src/FileClassifier.App/Program.cs:10-14` und Run-Output in `artifacts/phase1/COMMANDS_RUN.md`

4) BDD-Ausgabe ist menschenlesbar
- Command: `bash tools/test-bdd-readable.sh`
- Ergebnis: Szenario-/Schritt-/Ergebnis-Ausgaben (`[BDD] ...`) sichtbar
- Evidenz: `tests/FileTypeDetectionLib.Tests/Support/BddConsoleHooks.cs:13-43` + Test-Output in `artifacts/phase1/COMMANDS_RUN.md`

## Findings (P0/P1/P2)

### P1-01: Restore-Pfad ist nicht voll deterministisch reproduzierbar
- Symptom:
  - `dotnet restore FileClassifier.sln -v minimal` lief in der Session ohne Abschluss-Output weiter; Prozess musste manuell beendet werden.
  - Portable-Check zeigt wiederholt `NU1900` wegen nicht erreichbarer NuGet Security-Feed-Quelle.
- Ursache:
  - Netzwerk-/Feed-Abhaengigkeit (`https://api.nuget.org/v3/index.json`) beeinflusst Restore-Qualitaet.
- Evidenz:
  - Restore-Haenger + Prozessbeobachtung in `artifacts/phase1/COMMANDS_RUN.md`.
  - `tools/check-portable-filetypedetection.sh` Output mit `NU1900` ebenfalls in `artifacts/phase1/COMMANDS_RUN.md`.
- Fix-Strategie:
  1. Offline-/CI-stabilen Restore-Pfad dokumentieren (`--ignore-failed-sources` nur fuer Security-Feed-Warnungen).
  2. Optional Paketlocking einfuehren (`packages.lock.json`) zur Reproduzierbarkeit.
  3. Troubleshooting-Abschnitt in Root-README fuer NU1900/Feed-Errors.
- Akzeptanzkriterium:
  - In 3 aufeinanderfolgenden Runs auf frischem Clone beendet `dotnet restore` innerhalb definiertem Zeitfenster und ohne manuelles Eingreifen.

### P1-02: Kein Repo-Root README als zentraler Einstieg fuer neue Entwickler
- Symptom:
  - Im Repo-Root existiert kein `README.md`.
- Ursache:
  - Dokumentation liegt nur modulbezogen (`src/.../README.md`, `tests/.../README.md`, `portable/.../README.md`).
- Evidenz:
  - Root-README-Check in `artifacts/phase1/COMMANDS_RUN.md` mit leerem Output.
- Fix-Strategie:
  - `README.md` im Repo-Root mit Quickstart-Matrix (Build/Test/Run/BDD/Portable).
- Akzeptanzkriterium:
  - Neuer Entwickler kann mit 1 Dokument den Standard-Workflow reproduzieren.

### P2-01: Sichere ZIP-Extraktion ist logisch korrekt, aber architektonisch in Sammeldatei gebuendelt
- Symptom:
  - ZIP-Validierung und ZIP-Extraktion liegen beide in `Infrastructure/Internals.vb` in `ZipSafetyGate`.
- Ursache:
  - Funktionale Erweiterung wurde in bestehende Internals-Klasse integriert statt in separates Modul.
- Evidenz:
  - `src/FileTypeDetectionLib/Infrastructure/Internals.vb:82-288` (`ZipSafetyGate`, inkl. `TryExtractZipStream`, `ProcessZipStream`, `ExtractEntryToDirectory`).
  - Externer Einstiegspunkt `src/FileTypeDetectionLib/FileTypeDetector.vb:215-246` (`ExtractZipSafe`).
- Fix-Strategie (ohne Redundanz):
  1. Neue Datei `src/FileTypeDetectionLib/Infrastructure/ZipProcessing.vb`.
  2. SSOT-Engine `ZipProcessingEngine.ProcessZipStream(...)` zentral halten.
  3. Zwei schmale Adapterklassen:
     - `ZipSafetyGate` -> `IsZipSafe*` ruft Engine ohne Extract-Callback.
     - `ZipExtractor` -> `TryExtractZip*` ruft dieselbe Engine mit Extract-Callback.
  4. `FileTypeDetector.ExtractZipSafe` auf `ZipExtractor` routen.
- Akzeptanzkriterium:
  - Keine duplizierte ZIP-Iterationslogik im Code (ein SSOT-Ort fuer Entry-Schleife/Limits/Nesting).
  - Vorhandene ZIP-Tests bleiben gruen (`ZipGatePropertyTests`, `ZipAdversarialTests`, `ZipExtractionUnitTests`).

## Entscheidung zur Benutzerfrage (eigene Datei fuer sichere Extraktion)
- Ja, architektonisch ist eine Auslagerung in eigene Datei/Klasse sinnvoll.
- Bedingung fuer Determinismus + Redundanzfreiheit: Die Entry-Iteration und Sicherheitschecks muessen als **eine** zentrale Engine erhalten bleiben.

