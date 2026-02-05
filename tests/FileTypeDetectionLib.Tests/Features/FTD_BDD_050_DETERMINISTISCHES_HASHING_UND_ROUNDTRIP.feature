# language: de
@e2e @hashing @deterministic @roundtrip
Funktionalität: Deterministische Hash-Nachweise fuer Dateien und Archive

  Hintergrund:
    Angenommen die folgenden Ressourcen existieren
      | ressource      |
      | fx.sample_zip  |
      | fx.sample_pdf  |

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
