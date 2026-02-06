# Index - Abstractions/Archive

## 1. Zweck

Archiv-Eintragsmodell für sichere In-Memory-Extraktion.

## 2. Datei

- [ZipExtractedEntry.vb](./ZipExtractedEntry.vb)

## 3. Vertragsregeln

- Typname `ZipExtractedEntry` ist historisch und bleibt aus Kompatibilitätsgründen bestehen.
- Verwendung ist archivformat-generisch (ZIP/TAR/GZIP/7z/RAR via einheitliche Pipeline).
- Modell stellt Entry-Pfad, Content und Grösse deterministisch bereit.

## 4. Siehe auch

- [Abstractions-Index](../README.md)
- [Funktionsreferenz](../../../../docs/01_FUNCTIONS.md)

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
