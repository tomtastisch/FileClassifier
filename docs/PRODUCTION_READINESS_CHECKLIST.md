# Production Readiness Checklist - FileTypeDetection

## 1. Build and Test Gate
- [ ] `dotnet restore FileClassifier.sln -v minimal`
- [ ] `dotnet format FileClassifier.sln --verify-no-changes`
- [ ] `dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal`
- [ ] `dotnet test FileClassifier.sln -v minimal`
- [ ] Coverage-Gate: `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal /p:CollectCoverage=true /p:Include="[FileTypeDetectionLib]*" /p:CoverletOutputFormat=cobertura /p:Threshold=75%2c60 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total`
- [ ] BDD-Tag-Filter pruefen (z. B. `Category=e2e`, `Category=materializer`)

## 2. Security and Limits
- [ ] `FileTypeProjectBaseline.ApplyDeterministicDefaults()` beim Start setzen
- [ ] Falls noetig: `FileTypeOptions.LoadOptions(json)` fuer Umgebungslimits
- [ ] ZIP-Grenzen (`MaxZipEntries`, `MaxZipCompressionRatio`, `MaxZipNestingDepth`) mit Ops abstimmen
- [ ] Root-/Traversal-Schutz im Zielsystem validieren (Dateisystemrechte, Container-Mounts)

## 3. Runtime Configuration
- [ ] Konfigurationsquelle je Umgebung festlegen (embedded, file, secret/config service)
- [ ] Snapshot der aktiven Optionen per `FileTypeOptions.GetOptions()` protokollieren
- [ ] Aenderungsprozess fuer Grenzwerte (Review + Freigabe) dokumentieren

## 4. Operational Readiness
- [ ] Logging-Level und Ziel (stdout/file/collector) festlegen
- [ ] Alarmierung auf fail-closed-Haeufung einrichten (Unknown/False/empty spikes)
- [ ] Durchsatz-/Latenz-Baseline erfassen (z. B. Benchmark-Smoke in CI)
- [ ] Incident-Runbook fuer ZIP-Angriffsfaelle (Bomb/Traversal) verfuegbar machen

## 5. Integration Contract
- [ ] Aufrufervertrag klarstellen: keine Ausnahme als Kontrollfluss erwarten
- [ ] Rueckgabe-Semantik dokumentieren:
  - `Unknown` bei unsicherer/ungueltiger Erkennung
  - `False` bei fehlerhaften Guard-/Persistenzpfaden
  - leere Liste bei fehlgeschlagener Memory-Extraktion
- [ ] Consumer-Reaktion auf fail-closed im Service-Workflow definiert

## 6. Release and Rollback
- [ ] Release-Version/Changelog erstellt
- [ ] Canary/gestaffeltes Rollout geplant
- [ ] Rollback-Strategie definiert (Artefakt + Konfiguration + Datenpfade)
- [ ] Nach-Release-Checks (Smoke + kritische BDD-Filter) ausgefuehrt

## 7. Quick Commands
```bash
dotnet restore FileClassifier.sln -v minimal
dotnet format FileClassifier.sln --verify-no-changes
dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal
dotnet test FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal /p:CollectCoverage=true /p:Include="[FileTypeDetectionLib]*" /p:CoverletOutputFormat=cobertura /p:Threshold=75%2c60 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "Category=e2e" -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "Category=materializer" -v minimal
```
