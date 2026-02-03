# language: de
Funktionalität: Dateityp-Erkennung über Inhaltsanalyse (fail-closed)

  Hintergrund:
    Angenommen die Ressource "sample.pdf" existiert
    Und die Ressource "sample.jpg" existiert
    Und die Ressource "broken_no_ext" existiert

  Szenario: PDF wird korrekt erkannt
    Gegeben sei die Datei "sample.pdf"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Pdf"

  Szenario: JPEG wird korrekt erkannt
    Gegeben sei die Datei "sample.jpg"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Jpeg"

  Szenario: Kaputte Datei wird fail-closed als Unknown klassifiziert
    Gegeben sei die Datei "broken_no_ext"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Unknown"
