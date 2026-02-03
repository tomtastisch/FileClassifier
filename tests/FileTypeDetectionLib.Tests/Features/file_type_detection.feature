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

  Szenario: Zu große Datei wird fail-closed als Unknown klassifiziert
    Angenommen die maximale Dateigroesse ist 32 Bytes
    Und die Datei "large_jpeg.bin"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Unknown"

  Szenario: MIME-Provider folgt dem Build-Toggle
    Dann ist der MIME-Provider build-konform aktiv
