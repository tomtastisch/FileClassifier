# CI Pipeline - FileClassifier

## 1. Zweck
Diese CI-Pipeline liefert einen auditierbaren Nachweis der Produktionsreife:
- Inkonsistenzen/Format
- Build-Fehler/Warnungen
- NuGet-Sicherheitslage (Vulnerabilities)
- BDD-Readable Tests + Coverage-Gate
- Dokumentationskonsistenz
- Statische Analyse via Qodana (separater Workflow)

Alle Checks laufen deterministisch in getrennten Steps. Jeder Check schreibt Artefakte in stabile Pfade und wird pro Job hochgeladen (auch bei Fehlern).

## 2. Jobs und Checks (1 Step = 1 Check)

### Job: preflight (fail-fast)
1. Docs-Check
2. Versioning-Guard
3. Format-Check

### Job: build
1. Restore
2. Build (Warnings as Errors)

### Job: security-nuget
1. NuGet Vulnerability Scan (inkl. transitive)
2. NuGet Deprecated Packages

### Job: tests-bdd-coverage
1. BDD-Readable Tests + Coverage-Gate (Single Run)

### Job: summary (optional)
- Job Summary (nur Report, kein Gate)

## 3. Fail Conditions
- Docs-Check: Fehlende lokale Links oder fehlende Versioning-Referenzen.
- Versioning-Guard: Policy-Verletzungen.
- Format-Check: Abweichungen von dotnet format.
- Build: Fehler oder Warnungen (warnaserror).
- NuGet Vulnerabilities: High/Critical in `dotnet list package --vulnerable`.
- Tests + Coverage: Testfehler oder Unterschreiten der Coverage-Grenzen.
Hinweis: `dotnet list package --deprecated` ist ein Report-Only Check (kein Gate).

## 4. Coverage-Gate
- Line >= 85%
- Branch >= 69%
- Durchsetzung im BDD-Teststep (Single-Run) via Coverlet.

## 5. Artefakt-Pfade (stabil)
- `artifacts/docs/doc-check.txt`
- `artifacts/versioning/versioning-check.txt`
- `artifacts/format/format-check.txt`
- `artifacts/build/build-log.txt`
- `artifacts/security/nuget-vuln.txt`
- `artifacts/security/nuget-deprecated.txt`
- `artifacts/tests/**` (TRX + readable Logs + dotnet-test.log)
- `artifacts/coverage/coverage.cobertura.xml`
- `artifacts/coverage/coverage-summary.txt`

## 6. Qodana (separater Workflow)
Qodana ist bewusst vom CI-Workflow getrennt und token-abhängig.

Coverage in Qodana:
- Die CI erzeugt den Cobertura-Report unter `artifacts/coverage/coverage.cobertura.xml`.
- Der Qodana-Workflow versucht, dieses Artefakt zu importieren, indem er es nach `.qodana/code-coverage/coverage.cobertura.xml` lädt.
- Falls Qodana Coverage dennoch als "Not set" anzeigt, ist CI die Source of Truth (SSOT). Das ist akzeptiert, solange das CI-Coverage-Gate greift und die Artefakte vorhanden sind.

## 7. Lokale Reproduktion
```bash
python3 tools/check-docs.py
bash tools/versioning/check-versioning.sh
dotnet format FileClassifier.sln --verify-no-changes
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal

dotnet list FileClassifier.sln package --vulnerable --include-transitive
dotnet list FileClassifier.sln package --deprecated

TEST_BDD_OUTPUT_DIR=artifacts/tests bash tools/test-bdd-readable.sh -- \
  /p:CollectCoverage=true \
  /p:Include="[FileTypeDetectionLib]*" \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="$(pwd)/artifacts/coverage/coverage" \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
```
