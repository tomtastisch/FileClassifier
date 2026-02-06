# language: de
@unit
@api
@detector
Funktionalität: Unit-nahe API-Detektion und Policy-Verhalten

Hintergrund:
Angenommen die folgenden Ressourcen existieren
| ressource |
| sample.pdf |
| sample.jpg |
| sample.png |
| sample.gif |
| sample.webp |
| sample_pdf_as_txt.txt |
| sample_pdf_no_extension |
| broken_no_ext |
| empty.bin |
| large_jpeg.bin |

    @positiv
    @detektion
    @detector
    Szenariogrundriss: Bekannte Signatur wird korrekt erkannt
        Angenommen die Datei "<datei>"
        Wenn ich den Dateityp ermittle
        Dann ist der erkannte Typ "<typ>"

        Beispiele:
          | datei       | typ  |
          | sample.pdf  | Pdf  |
          | sample.jpg  | Jpeg |
          | sample.png  | Png  |
          | sample.gif  | Gif  |
          | sample.webp | Webp |

    @negativ
    @detektion
    @detector
    Szenario: Kaputte Datei wird fail-closed als Unknown klassifiziert
        Angenommen die Datei "broken_no_ext"
        Wenn ich den Dateityp ermittle
        Dann ist der erkannte Typ "Unknown"

    @negativ
    @detektion
    @detector
    Szenario: Leere Datei wird fail-closed als Unknown klassifiziert
        Angenommen die Datei "empty.bin"
        Wenn ich den Dateityp ermittle
        Dann ist der erkannte Typ "Unknown"

    @negativ
    @endung
    @detector
    Szenario: Endungspruefung liefert fail-closed Unknown bei Mismatch
        Angenommen die Datei "sample_pdf_as_txt.txt"
        Wenn ich den Dateityp mit Endungspruefung ermittle
        Dann ist der erkannte Typ "Unknown"

    @positiv
    @negativ
    @endung
    @detector
    Szenariogrundriss: Endungspruefung liefert boolesches Ergebnis
        Angenommen die Datei "<datei>"
        Wenn ich die Endung gegen den erkannten Typ pruefe
        Dann ist das Endungsergebnis "<ergebnis>"

        Beispiele:
          | datei                   | ergebnis |
          | sample.pdf              | True     |
          | sample_pdf_as_txt.txt   | False    |
          | sample_pdf_no_extension | True     |

    @positiv
    @lesen
    @detector
    Szenariogrundriss: ReadFileSafe liefert fuer vorhandene Dateien einen nicht-leeren Bytestrom
        Angenommen die Datei "<datei>"
        Wenn ich die Datei sicher in Bytes lese
        Dann ist der sicher gelesene Bytestrom nicht leer

        Beispiele:
          | datei      |
          | sample.pdf |
          | sample.jpg |

    @positiv
    @negativ
    @typpruefung
    @detector
    Szenariogrundriss: IsOfType prueft aktuelle Bytes gegen erwarteten Typ
        Angenommen ich lese die Datei "<datei>" als aktuelle Bytes
        Wenn ich pruefe ob die aktuellen Bytes vom Typ "<typ>" sind
        Dann ist das Typpruefungsergebnis "<ergebnis>"

        Beispiele:
          | datei      | typ  | ergebnis |
          | sample.pdf | Pdf  | True     |
          | sample.pdf | Zip  | False    |
          | sample.jpg | Jpeg | True     |

    @negativ
    @grenzen
    @detector
    Szenario: Zu grosse Datei wird fail-closed als Unknown klassifiziert
        Angenommen die maximale Dateigroesse ist 32 Bytes
        Und die Datei "large_jpeg.bin"
        Wenn ich den Dateityp ermittle
        Dann ist der erkannte Typ "Unknown"

    @positiv
    @konfiguration
    @processing
    Szenario: MIME-Provider folgt dem Build-Toggle
        Dann ist der MIME-Provider build-konform aktiv