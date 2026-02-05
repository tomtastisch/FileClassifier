# language: de
@e2e @hashing @deterministic @roundtrip
Funktionalität: Deterministische Hash-Nachweise fuer Dateien und Archive

  Hintergrund:
    Angenommen die folgenden Ressourcen existieren
      | ressource      |
      | fx.sample_zip  |
      | fx.sample_rar  |
      | fx.sample_7z   |
      | fx.sample_pdf  |
      | fx.corrupt_noext |

  @positiv @archive
  Szenario: Archivdatei liefert logischen RoundTrip-Nachweis ueber h1-h4
    Angenommen die Datei "fx.sample_zip"
    Wenn ich den deterministischen Hashbericht der aktuellen Datei berechne
    Dann ist der Hashbericht als Archiv klassifiziert "True"
    Und ist der Hashbericht logisch konsistent

  @positiv @raw
  Szenario: Direkte Datei-Bytes liefern deterministische Hash-Nachweise
    Angenommen ich lese die Datei "fx.sample_pdf" als aktuelle Bytes
    Wenn ich den deterministischen Hash der aktuellen Bytes berechne
    Dann ist im letzten Hashnachweis ein logischer Hash vorhanden
    Und ist im letzten Hashnachweis ein physischer Hash vorhanden
    Und entsprechen sich logischer und physischer Hash im letzten Nachweis "True"

  @positiv @archive @materializer
  Szenariogrundriss: Archiv-Entry bleibt beim Byte->Materializer->Byte Zyklus hash-stabil
    Angenommen ich erzeuge aktuelle Archiv-Bytes vom Typ "<archivtyp>"
    Und ein leeres temporäres Zielverzeichnis
    Wenn ich die aktuellen Archiv-Bytes validiere
    Dann ist das Archiv-Validierungsergebnis "True"
    Wenn ich die aktuellen Archiv-Bytes sicher in den Speicher extrahiere
    Dann ist der extrahierte Eintragssatz nicht leer
    Wenn ich übernehme den ersten extrahierten Eintrag als aktuelle Bytes
    Und ich den deterministischen Hash der aktuellen Bytes berechne
    Und ich den letzten logischen Hash als Referenz speichere
    Und ich den letzten physischen Hash als Referenz speichere
    Und ich speichere die aktuellen Bytes als "entry.bin"
    Und ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes
    Und ich den deterministischen Hash der aktuellen Bytes berechne
    Dann entspricht der letzte logische Hash der gespeicherten Referenz
    Und entspricht der letzte physische Hash der gespeicherten Referenz

    Beispiele:
      | archivtyp |
      | zip       |
      | rar       |
      | 7z        |

  @negativ @archive
  Szenariogrundriss: Nicht-Archiv- oder defekte Bytes schlagen bei Archiv-Validierung fail-closed fehl
    Angenommen ich lese die Datei "<ressource>" als aktuelle Bytes
    Wenn ich die aktuellen Archiv-Bytes validiere
    Dann ist das Archiv-Validierungsergebnis "False"
    Wenn ich die aktuellen Archiv-Bytes sicher in den Speicher extrahiere
    Dann ist der extrahierte Eintragssatz leer
    Wenn ich den deterministischen Hash der aktuellen Bytes berechne
    Dann entsprechen sich logischer und physischer Hash im letzten Nachweis "True"

    Beispiele:
      | ressource       |
      | fx.sample_pdf   |
      | fx.corrupt_noext |
