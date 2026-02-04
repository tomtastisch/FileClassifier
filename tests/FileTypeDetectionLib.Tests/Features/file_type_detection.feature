# language: de
Funktionalität: Dateityp-Erkennung über Inhaltsanalyse (fail-closed)

  Hintergrund:
    Angenommen die Ressource "sample.pdf" existiert
    Und die Ressource "sample.jpg" existiert
    Und die Ressource "sample.png" existiert
    Und die Ressource "sample.gif" existiert
    Und die Ressource "sample.webp" existiert
    Und die Ressource "sample.zip" existiert
    Und die Ressource "sample.docx" existiert
    Und die Ressource "sample.xlsx" existiert
    Und die Ressource "sample.pptx" existiert
    Und die Ressource "sample_pdf_as_txt.txt" existiert
    Und die Ressource "sample_pdf_no_extension" existiert
    Und die Ressource "invalid_docx_marker_only.zip" existiert
    Und die Ressource "invalid_xlsx_marker_only.zip" existiert
    Und die Ressource "invalid_pptx_marker_only.zip" existiert
    Und die Ressource "broken_no_ext" existiert
    Und die Ressource "empty.bin" existiert
    Und die Ressource "large_jpeg.bin" existiert

  Szenariogrundriss: Bekannte Signatur wird korrekt erkannt
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "<typ>"

    Beispiele:
      | datei       | typ     |
      | sample.pdf  | Pdf     |
      | sample.jpg  | Jpeg    |
      | sample.png  | Png     |
      | sample.gif  | Gif     |
      | sample.webp | Webp    |

  Szenario: Kaputte Datei wird fail-closed als Unknown klassifiziert
    Angenommen die Datei "broken_no_ext"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Unknown"

  Szenario: Leere Datei wird fail-closed als Unknown klassifiziert
    Angenommen die Datei "empty.bin"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Unknown"

  Szenario: ZIP bleibt ZIP, wenn kein OOXML erkannt wird
    Angenommen die Datei "sample.zip"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Zip"

  Szenariogrundriss: OOXML ZIP wird auf den konkreten Typ verfeinert
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "<typ>"

    Beispiele:
      | datei       | typ  |
      | sample.docx | Docx |
      | sample.xlsx | Xlsx |
      | sample.pptx | Pptx |

  Szenariogrundriss: Marker-only ZIP wird nicht als OOXML verfeinert
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Zip"

    Beispiele:
      | datei                      |
      | invalid_docx_marker_only.zip |
      | invalid_xlsx_marker_only.zip |
      | invalid_pptx_marker_only.zip |

  Szenario: Endungspruefung liefert fail-closed Unknown bei Mismatch
    Angenommen die Datei "sample_pdf_as_txt.txt"
    Wenn ich den Dateityp mit Endungspruefung ermittle
    Dann ist der erkannte Typ "Unknown"

  Szenariogrundriss: Endungspruefung liefert boolesches Ergebnis
    Angenommen die Datei "<datei>"
    Wenn ich die Endung gegen den erkannten Typ pruefe
    Dann ist das Endungsergebnis "<ergebnis>"

    Beispiele:
      | datei                 | ergebnis |
      | sample.pdf            | True     |
      | sample_pdf_as_txt.txt | False    |
      | sample_pdf_no_extension | True   |

  Szenario: Zu große Datei wird fail-closed als Unknown klassifiziert
    Angenommen die maximale Dateigroesse ist 32 Bytes
    Und die Datei "large_jpeg.bin"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Unknown"

  Szenario: MIME-Provider folgt dem Build-Toggle
    Dann ist der MIME-Provider build-konform aktiv

  @materializer
  Szenario: ZIP-Entries werden extrahiert, als Bytes uebernommen und via FileMaterializer gespeichert
    Angenommen ein leeres temporäres Zielverzeichnis
    Und die Datei "sample.zip"
    Wenn ich extrahiere die ZIP-Datei sicher in Memory
    Und ich übernehme den ersten extrahierten Eintrag als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "zip-entry-note.txt"
    Dann ist der extrahierte Eintragssatz nicht leer
    Und existiert die gespeicherte Datei "zip-entry-note.txt"
    Und entspricht die gespeicherte Datei "zip-entry-note.txt" den aktuellen Bytes

  @materializer
  Szenario: Originaldatei-Bytes werden iterativ gespeichert und als letzter Stand weiterverwendet
    Angenommen ein leeres temporäres Zielverzeichnis
    Und ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich speichere die aktuellen Bytes als "chain-original-step1.bin"
    Und ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "chain-original-step2.bin"
    Dann existiert die gespeicherte Datei "chain-original-step1.bin"
    Und existiert die gespeicherte Datei "chain-original-step2.bin"
    Und entspricht die gespeicherte Datei "chain-original-step2.bin" den aktuellen Bytes

  @materializer
  Szenario: Extrahierte ZIP-Entry-Bytes werden iterativ gespeichert und als letzter Stand weiterverwendet
    Angenommen ein leeres temporäres Zielverzeichnis
    Und die Datei "sample.zip"
    Wenn ich extrahiere die ZIP-Datei sicher in Memory
    Und ich übernehme den ersten extrahierten Eintrag als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "chain-zip-step1.bin"
    Und ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes
    Und ich speichere die aktuellen Bytes als "chain-zip-step2.bin"
    Dann ist der extrahierte Eintragssatz nicht leer
    Und existiert die gespeicherte Datei "chain-zip-step1.bin"
    Und existiert die gespeicherte Datei "chain-zip-step2.bin"
    Und entspricht die gespeicherte Datei "chain-zip-step2.bin" den aktuellen Bytes

  @materializer @negative
  Szenario: Materializer lehnt Speichern ohne overwrite bei bestehender Datei ab
    Angenommen ein leeres temporäres Zielverzeichnis
    Und es existiert bereits eine gespeicherte Datei "conflict.bin"
    Und ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich versuche die aktuellen Bytes als "conflict.bin" ohne overwrite zu speichern
    Dann ist der letzte Speicherversuch fehlgeschlagen
    Und bleibt die bestehende Datei "conflict.bin" unveraendert

  @materializer @negative
  Szenario: Materializer lehnt ungueltigen Zielpfad ab
    Angenommen ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich versuche die aktuellen Bytes in den Zielpfad "   " zu speichern
    Dann ist der letzte Speicherversuch fehlgeschlagen
    Und existiert keine Datei im Zielpfad "   "

  @materializer @negative
  Szenario: Materializer lehnt zu grosse Payload gegen MaxBytes ab
    Angenommen ein leeres temporäres Zielverzeichnis
    Und die maximale Dateigroesse ist 16 Bytes
    Und ich lese die Datei "sample.pdf" als aktuelle Bytes
    Wenn ich versuche die aktuellen Bytes als "too-large.bin" ohne overwrite zu speichern
    Dann ist der letzte Speicherversuch fehlgeschlagen
    Und existiert die gespeicherte Datei "too-large.bin" nicht
