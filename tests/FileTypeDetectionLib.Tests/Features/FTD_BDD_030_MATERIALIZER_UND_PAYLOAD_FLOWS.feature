# language: de
@e2e @materializer @processing @archive
Funktionalität: End-to-End-Workflows fuer Extraktion, Payload-Uebernahme und Materialisierung

  Hintergrund:
    Angenommen die folgenden Ressourcen existieren
      | ressource   |
      | sample.zip  |
      | sample.pdf  |

  @positiv @materializer @archive
  Szenario: ZIP-Entries werden extrahiert, als Bytes uebernommen und ueber FileMaterializer gespeichert
    Angenommen ein leeres temporäres Zielverzeichnis
    Und die Datei "sample.zip"
    Wenn ich die ZIP-Datei sicher in den Speicher extrahiere
    Und ich übernehme den ersten extrahierten Eintrag als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "zip-entry-note.txt"
    Dann ist der extrahierte Eintragssatz nicht leer
    Und existiert die gespeicherte Datei "zip-entry-note.txt"
    Und entspricht die gespeicherte Datei "zip-entry-note.txt" den aktuellen Bytes

  @positiv @materializer
  Szenario: Originaldatei-Bytes werden iterativ gespeichert und als letzter Stand weiterverwendet
    Angenommen ein leeres temporäres Zielverzeichnis
    Und ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich die aktuellen Bytes als "chain-original-step1.bin" speichere
    Und ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "chain-original-step2.bin"
    Dann existiert die gespeicherte Datei "chain-original-step1.bin"
    Und existiert die gespeicherte Datei "chain-original-step2.bin"
    Und entspricht die gespeicherte Datei "chain-original-step2.bin" den aktuellen Bytes

  @positiv @materializer @archive
  Szenario: Extrahierte ZIP-Entry-Bytes werden iterativ gespeichert und als letzter Stand weiterverwendet
    Angenommen ein leeres temporäres Zielverzeichnis
    Und die Datei "sample.zip"
    Wenn ich die ZIP-Datei sicher in den Speicher extrahiere
    Und ich übernehme den ersten extrahierten Eintrag als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "chain-zip-step1.bin"
    Und ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "chain-zip-step2.bin"
    Dann ist der extrahierte Eintragssatz nicht leer
    Und existiert die gespeicherte Datei "chain-zip-step1.bin"
    Und existiert die gespeicherte Datei "chain-zip-step2.bin"
    Und entspricht die gespeicherte Datei "chain-zip-step2.bin" den aktuellen Bytes

  @negativ @materializer
  Szenario: Materializer lehnt Speichern ohne overwrite bei bestehender Datei ab
    Angenommen ein leeres temporäres Zielverzeichnis
    Und es existiert bereits eine gespeicherte Datei "conflict.bin"
    Und ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich versuche, die aktuellen Bytes als "conflict.bin" ohne overwrite zu speichern
    Dann ist der letzte Speicherversuch fehlgeschlagen
    Und bleibt die bestehende Datei "conflict.bin" unveraendert

  @negativ @materializer
  Szenario: Materializer lehnt ungueltigen Zielpfad ab
    Angenommen ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich versuche, die aktuellen Bytes in den Zielpfad "   " zu speichern
    Dann ist der letzte Speicherversuch fehlgeschlagen
    Und existiert keine Datei im ungueltigen Zielpfad

  @negativ @materializer
  Szenario: Materializer lehnt zu grosse Payload gegen MaxBytes ab
    Angenommen ein leeres temporäres Zielverzeichnis
    Und die maximale Dateigroesse ist 16 Bytes
    Und ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich versuche, die aktuellen Bytes als "too-large.bin" ohne overwrite zu speichern
    Dann ist der letzte Speicherversuch fehlgeschlagen
    Und existiert die gespeicherte Datei "too-large.bin" nicht
