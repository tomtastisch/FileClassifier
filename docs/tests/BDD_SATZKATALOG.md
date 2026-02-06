# BDD-Satzkatalog (Deutsch, kanonisch)

## 1. Zweck
Dieser Katalog ist die verbindliche Satzliste fuer die Reqnroll-BDD-Features im Projekt.
Ziel ist eine einheitliche, wiederverwendbare und grammatikalisch klare Form ohne redundante Synonyme.

## 2. Geltungsbereich
- `tests/FileTypeDetectionLib.Tests/Features/*.feature`
- `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs`

## 3. Verwendung
- In `.feature`-Dateien nur die unten gelisteten Satzmuster verwenden.
- Keine alternativen Formulierungen fuer denselben technischen Schritt einfuehren.
- Neue Saetze nur bei neuem fachlichem Verhalten und immer mit zugehoeriger Step-Methode.

## 4. Uebersicht
| ID | Schrittart | Satzmuster (Gherkin) | Zweck im Test | Uebergaben (Parameter) | Methodenreferenz |
|---|---|---|---|---|---|
| G01 | Given | `die Ressource {string} existiert` | Einzelne Testressource vorab verifizieren | `name: string` (Ressourcenname) | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenTheResourceExists` |
| G02 | Given | `die folgenden Ressourcen existieren` | Ressourcenliste aus Tabelle vorab verifizieren | `table` mit Spalte `ressource` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenTheFollowingResourcesExist` |
| G03 | Given | `die Datei {string}` | Aktuellen Dateipfad im Szenario setzen | `name: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenTheFile` |
| G04 | Given | `ein leeres temporaeres Zielverzeichnis` | Isoliertes Temp-Ziel fuer Materialisierung erzeugen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenAnEmptyTemporaryTargetDirectory` |
| G05 | Given | `ich lese die Datei {string} als aktuelle Bytes` | Dateiinhalt als aktuelles Payload-Bytearray setzen | `name: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenIReadFileAsCurrentBytes` |
| G06 | Given | `ich erzeuge aktuelle Archiv-Bytes vom Typ {string}` | Deterministisches Archiv-Bytearray fuer Typ erzeugen | `archiveType: string` (`zip`,`tar`,`tar.gz`,`7z`,`rar`) | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenICreateCurrentArchiveBytesOfType` |
| G07 | Given | `es existiert bereits eine gespeicherte Datei {string}` | Konfliktdatei fuer Overwrite-/Fail-Tests anlegen | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenExistingMaterializedFile` |
| G08 | Given | `die maximale Dateigroesse ist {long} Bytes` | MaxBytes-Limit fuer fail-closed Szenarien setzen | `maxBytes: long` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `GivenTheMaximumSizeInBytes` |
| W01 | When | `ich den Dateityp ermittle` | Dateityp fuer `CurrentPath` erkennen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIDetectTheFileType` |
| W02 | When | `ich den deterministischen Hashbericht der aktuellen Datei berechne` | RoundTrip-Hashbericht fuer Datei erzeugen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenICalculateDeterministicHashReportForCurrentFile` |
| W03 | When | `ich den deterministischen Hash der aktuellen Bytes berechne` | Hash-Evidence fuer aktuelles Payload erzeugen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenICalculateDeterministicHashForCurrentBytes` |
| W04 | When | `ich den letzten logischen Hash als Referenz speichere` | Logischen Digest fuer spaeteren Vergleich speichern | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIStoreLastLogicalHashAsReference` |
| W05 | When | `ich den letzten physischen Hash als Referenz speichere` | Physischen Digest fuer spaeteren Vergleich speichern | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIStoreLastPhysicalHashAsReference` |
| W06 | When | `ich den Dateityp der aktuellen Bytes ermittle` | Dateityp direkt aus Payload erkennen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIDetectTheCurrentPayloadType` |
| W07 | When | `ich die aktuellen Archiv-Bytes validiere` | Archiv-Validierung fuer aktuelles Payload ausfuehren | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIValidateCurrentArchiveBytes` |
| W08 | When | `ich die Archiv-Datei sicher in den Speicher extrahiere` | Archivdatei vom Pfad sicher extrahieren | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIExtractArchiveFileSafelyToMemory` |
| W09 | When | `ich die aktuellen Archiv-Bytes sicher in den Speicher extrahiere` | Archiv-Payload sicher extrahieren | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIExtractCurrentArchiveBytesSafelyToMemory` |
| W10 | When | `ich den ersten extrahierten Eintrag als aktuelle Bytes uebernehme` | Ersten extrahierten Entry als neues Payload uebernehmen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIUseFirstExtractedEntryAsCurrentBytes` |
| W11 | When | `ich speichere die aktuellen Bytes als {string}` | Aktuelles Payload persistieren | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIPersistCurrentBytesAs` |
| W12 | When | `ich versuche, die aktuellen Bytes als {string} ohne Overwrite zu speichern` | Persistieren ohne Overwrite pruefen | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenITryPersistCurrentBytesWithoutOverwrite` |
| W13 | When | `ich versuche, die aktuellen Bytes in den Zielpfad {string} zu speichern` | Persistieren in rohen Zielpfad testen | `destinationPath: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenITryPersistCurrentBytesToRawDestination` |
| W14 | When | `ich die aktuellen Archiv-Bytes sicher als Verzeichnis {string} materialisiere` | Archiv-Payload in Verzeichnis materialisieren | `directoryName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIMaterializeCurrentArchiveBytesSecurely` |
| W15 | When | `ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes` | Letzte Materialisierung wieder als Payload laden | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenILoadLastMaterializedBytesAsCurrentBytes` |
| W16 | When | `ich den Dateityp mit Endungspruefung ermittle` | Dateityp inkl. Extension-Verification erkennen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIDetectTheFileTypeWithExtensionVerification` |
| W17 | When | `ich die Endung gegen den erkannten Typ pruefe` | Explizite Extension-Match-Pruefung ausfuehren | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIVerifyExtensionAgainstDetectedType` |
| W18 | When | `ich die Datei sicher in Bytes lese` | Datei fail-closed in Bytearray lesen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenIReadTheFileSafelyAsBytes` |
| W19 | When | `ich pruefe, ob die aktuellen Bytes vom Typ {string} sind` | Typvergleich fuer aktuelles Payload pruefen | `expectedKind: string` (`FileKind`) | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `WhenICheckCurrentBytesType` |
| T01 | Then | `ist der erkannte Typ {string}` | Erkannten Dateityp verifizieren | `expectedKind: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenTheDetectedKindIs` |
| T02 | Then | `ist das Endungsergebnis {string}` | Boolean-Ergebnis der Endungspruefung verifizieren | `expectedBoolean: string` (`True`/`False`) | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenTheExtensionResultIs` |
| T03 | Then | `ist der MIME-Provider build-konform aktiv` | Build-bedingten MIME-Backend-Switch pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenTheMimeProviderIsBuildConform` |
| T04 | Then | `ist der sicher gelesene Bytestrom nicht leer` | Safe-Read liefert verwertbaren Inhalt | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenSafelyReadByteStreamIsNotEmpty` |
| T05 | Then | `ist das Typpruefungsergebnis {string}` | Ergebnis von `IsOfType` verifizieren | `expectedBoolean: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenTypeCheckResultIs` |
| T06 | Then | `ist das Archiv-Validierungsergebnis {string}` | Ergebnis von `TryValidate` verifizieren | `expectedBoolean: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenArchiveValidationResultIs` |
| T07 | Then | `ist der extrahierte Eintragssatz nicht leer` | Erfolgreiche Extraktion mit Entries pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenExtractedEntrySetIsNotEmpty` |
| T08 | Then | `ist der extrahierte Eintragssatz leer` | Fail-closed Extraktion ohne Entries pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenExtractedEntrySetIsEmpty` |
| T09 | Then | `existiert die gespeicherte Datei {string}` | Persistierte Datei vorhanden | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenMaterializedFileExists` |
| T10 | Then | `enthaelt das materialisierte Verzeichnis {string} mindestens eine Datei` | Verzeichnis-Materialisierung erfolgreich | `directoryName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenMaterializedDirectoryContainsAtLeastOneFile` |
| T11 | Then | `entspricht die gespeicherte Datei {string} den aktuellen Bytes` | Byteidentitaet zwischen Payload und Datei pruefen | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenMaterializedFileEqualsCurrentBytes` |
| T12 | Then | `ist der letzte Speicherversuch fehlgeschlagen` | Erwarteter Persist-Fehler verifizieren | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLastPersistAttemptFailed` |
| T13 | Then | `ist der letzte Speicherversuch erfolgreich` | Erwarteten Persist-Erfolg verifizieren | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLastPersistAttemptSucceeded` |
| T14 | Then | `ist der Hashbericht logisch konsistent` | H1-H4 Konsistenz fuer RoundTrip pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenHashReportIsLogicallyConsistent` |
| T15 | Then | `ist der Hashbericht als Archiv klassifiziert {string}` | Archivklassifikation im Hashbericht pruefen | `expectedBoolean: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenHashReportArchiveClassificationIs` |
| T16 | Then | `ist im letzten Hashnachweis ein logischer Hash vorhanden` | Vorhandensein logischer Digest pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLastHashEvidenceContainsLogicalDigest` |
| T17 | Then | `ist im letzten Hashnachweis ein physischer Hash vorhanden` | Vorhandensein physischer Digest pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLastHashEvidenceContainsPhysicalDigest` |
| T18 | Then | `entsprechen sich logischer und physischer Hash im letzten Nachweis {string}` | Gleichheit logischer/physischer Hash pruefen | `expectedBoolean: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLogicalAndPhysicalDigestEqualityIs` |
| T19 | Then | `entspricht der letzte logische Hash der gespeicherten Referenz` | Stabile logische Hash-Referenz pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLastLogicalHashMatchesStoredReference` |
| T20 | Then | `entspricht der letzte physische Hash der gespeicherten Referenz` | Stabile physische Hash-Referenz pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenLastPhysicalHashMatchesStoredReference` |
| T21 | Then | `bleibt die bestehende Datei {string} unveraendert` | No-Overwrite-Invariante pruefen | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenExistingFileRemainsUnchanged` |
| T22 | Then | `existiert keine Datei im Zielpfad {string}` | Kein Artefakt an rohem Zielpfad pruefen | `destinationPath: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenNoFileExistsAtRawDestination` |
| T23 | Then | `existiert keine Datei im ungueltigen Zielpfad` | Kein Artefakt am zuletzt ungueltigen Zielpfad pruefen | keine | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenNoFileExistsAtInvalidDestination` |
| T24 | Then | `existiert die gespeicherte Datei {string} nicht` | Nicht-Existenz einer erwarteten Ausgabe pruefen | `fileName: string` | `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs` `ThenMaterializedFileDoesNotExist` |

## 5. Verifikation/Nachweise
```bash
bash tools/test-bdd-readable.sh
python3 tools/check-docs.py
```

## 6. Regeln fuer neue Saetze
1. Grammatik: einheitlich im Muster `ich ...` mit klarer Verbposition.
2. Scope: genau ein fachlicher Zweck pro Satz.
3. Parameter: nur explizit benoetigte Platzhalter erlauben.
4. Mapping: jeder Satz hat genau eine Methode im Step-Binding.
5. Regression: nach Aenderungen immer `bash tools/test-bdd-readable.sh` ausfuehren.

## 7. Verlinkte SSOT-Quellen
- [README.md](./README.md)
- [BDD_EXECUTION_AND_GHERKIN_FLOW.md](./BDD_EXECUTION_AND_GHERKIN_FLOW.md)
- [../CI_PIPELINE.md](../CI_PIPELINE.md)
