# Phase 1 Commands (Operability)

## 1) Build/Test/Restore path

### Restore (solution)
```bash
dotnet restore FileClassifier.sln -v minimal
```
Observed:
- command did not produce completion output within repeated polls (`session_id 5293`), while process remained active.
- process observed:
```bash
ps aux | rg "dotnet restore FileClassifier.sln" | rg -v rg
```
Output:
```text
tomwerner ... /opt/homebrew/.../dotnet restore FileClassifier.sln -v minimal
```
- process was terminated manually to continue phase analysis:
```bash
kill 43731
```

### Build (solution)
```bash
dotnet build FileClassifier.sln -v minimal
```
Output:
```text
Der Buildvorgang wurde erfolgreich ausgeführt.
0 Warnung(en)
0 Fehler
```

### Test (solution)
```bash
dotnet test FileClassifier.sln -v minimal
```
Output:
```text
Bestanden! : Fehler: 0, erfolgreich: 38, übersprungen: 0, gesamt: 38
```

## 2) Runtime path

### Run app without args
```bash
dotnet run --project src/FileClassifier.App/FileClassifier.App.csproj; echo EXIT:$?
```
Output:
```text
usage: FileClassifier.App <path>
EXIT:2
```

### Run app with sample file
```bash
dotnet run --project src/FileClassifier.App/FileClassifier.App.csproj -- tests/FileTypeDetectionLib.Tests/resources/sample.pdf; echo EXIT:$?
```
Output:
```text
Pdf
EXIT:0
```

## 3) BDD readability path

```bash
bash tools/test-bdd-readable.sh
```
Output (excerpt):
```text
[BDD] Szenario startet: ...
[BDD] Given: ...
[BDD] Ergebnis: OK
...
Der Testlauf war erfolgreich.
Gesamtzahl Tests: 38
```

## 4) Portable compatibility path

```bash
bash tools/check-portable-filetypedetection.sh --clean
```
Output (excerpt):
```text
warning NU1900: Fehler beim Abrufen von Paketsicherheitsrisikodaten ... https://api.nuget.org/v3/index.json
...
Portable integration check passed. Workdir removed.
```

## 5) Documentation discoverability checks

### Repo root README
```bash
LC_ALL=C find . -maxdepth 1 -type f -name 'README.md' -o -name 'readme.md' | LC_ALL=C sort
```
Output:
```text
# no output
```

### Source for extraction architecture
```bash
nl -ba src/FileTypeDetectionLib/Infrastructure/Internals.vb | sed -n '73,287p'
nl -ba src/FileTypeDetectionLib/FileTypeDetector.vb | sed -n '215,246p'
```
Observed:
- ZIP safety and ZIP extraction logic co-located inside `ZipSafetyGate` in `Internals.vb`.
- public external entrypoint calls that internal logic via `FileTypeDetector.ExtractZipSafe`.

