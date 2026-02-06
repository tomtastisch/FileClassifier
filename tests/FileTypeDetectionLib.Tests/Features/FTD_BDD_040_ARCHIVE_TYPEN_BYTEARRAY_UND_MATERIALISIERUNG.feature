# language: de
@integration
@processing
@materializer
@archive
Funktionalität: Einheitliches Verhalten fuer Archivtypen aus Byte-Arrays

Hintergrund:
Angenommen die folgenden Ressourcen existieren
| ressource |
| fx.sample_7z |
| fx.sample_rar |

    @positiv
    @detector
    @processing
    @materializer
    Szenariogrundriss: Archiv-Bytes werden einheitlich erkannt, validiert, extrahiert und als Entry-Bytes gespeichert
        Angenommen ein leeres temporäres Zielverzeichnis
        Und ich erzeuge aktuelle Archiv-Bytes vom Typ "<archivtyp>"
        Wenn ich den Dateityp der aktuellen Bytes ermittle
        Und ich die aktuellen Archiv-Bytes validiere
        Und ich die aktuellen Archiv-Bytes sicher in den Speicher extrahiere
        Und ich den ersten extrahierten Eintrag als aktuelle Bytes uebernehme
        Und ich speichere die aktuellen Bytes als "entry-<slug>.txt"
        Dann ist der erkannte Typ "Zip"
        Und ist das Archiv-Validierungsergebnis "True"
        Und ist der extrahierte Eintragssatz nicht leer
        Und existiert die gespeicherte Datei "entry-<slug>.txt"
        Und entspricht die gespeicherte Datei "entry-<slug>.txt" den aktuellen Bytes

        Beispiele:
          | archivtyp | slug   |
          | zip       | zip    |
          | tar       | tar    |
          | tar.gz    | targz  |
          | 7z        | sevenz |
          | rar       | rar    |

    @positiv
    @materializer
    @processing
    Szenariogrundriss: Archiv-Bytes werden mit secureExtract deterministisch als Verzeichnis materialisiert
        Angenommen ein leeres temporäres Zielverzeichnis
        Und ich erzeuge aktuelle Archiv-Bytes vom Typ "<archivtyp>"
        Wenn ich die aktuellen Archiv-Bytes sicher als Verzeichnis "out-<slug>" materialisiere
        Dann ist der letzte Speicherversuch erfolgreich
        Und enthaelt das materialisierte Verzeichnis "out-<slug>" mindestens eine Datei

        Beispiele:
          | archivtyp | slug   |
          | zip       | zip    |
          | tar       | tar    |
          | tar.gz    | targz  |
          | 7z        | sevenz |
          | rar       | rar    |
