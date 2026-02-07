# Index - src/FileClassifier.App

## 1. Zweck

Kompakter CLI-Einstiegspunkt für die Bibliothek `FileTypeDetectionLib`.

## 2. Dateien

- [Program](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileClassifier.App/Program.cs)
- [FileClassifier.App.csproj](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileClassifier.App/FileClassifier.App.csproj)

## 3. Verhalten

- Erwartet genau ein Argument: Dateipfad.
- Ermittelt den Typ über `FileTypeDetector.Detect(path)`.
- Gibt `FileKind` nach stdout aus und beendet sich mit Exit Code `0`.
- Bei falscher Argumentanzahl: Usage auf stderr und Exit Code `2`.

## 4. Beispiel

```bash
dotnet run --project src/FileClassifier.App -- ./tests/FileTypeDetectionLib.Tests/resources/sample.pdf
```

## 5. Siehe auch

- [Modulindex FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/README.md)
- [Funktionsreferenz](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/010_API_CORE.MD)

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
