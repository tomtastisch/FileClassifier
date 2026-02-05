# Index - Abstractions/Archive

## 1. Zweck
Archiv-Eintragsmodell fuer sichere In-Memory-Extraktion.

## 2. Datei
- [ZipExtractedEntry.vb](./ZipExtractedEntry.vb)

## 3. Vertragsregeln
- Typname `ZipExtractedEntry` ist historisch und bleibt aus Kompatibilitaetsgruenden bestehen.
- Verwendung ist archivformat-generisch (ZIP/TAR/GZIP/7z/RAR via einheitliche Pipeline).
- Modell stellt Entry-Pfad, Content und Groesse deterministisch bereit.

## 4. Siehe auch
- [Abstractions-Index](../README.md)
- [Funktionsreferenz](../../../../docs/01_FUNCTIONS.md)

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
