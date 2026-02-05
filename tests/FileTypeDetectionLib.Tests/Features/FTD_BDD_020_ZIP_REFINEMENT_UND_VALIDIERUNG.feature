# language: de
@integration @zip @refinement @detector @processing
Funktionalität: Integration von ZIP-Gate, strukturellem Refinement und Archiv-Fassade

  Hintergrund:
    Angenommen die folgenden Ressourcen existieren
      | ressource                    |
      | sample.zip                   |
      | sample.docx                  |
      | sample.xlsx                  |
      | sample.pptx                  |
      | invalid_docx_marker_only.zip |
      | invalid_xlsx_marker_only.zip |
      | invalid_pptx_marker_only.zip |

  @positiv @detector @zip
  Szenario: ZIP bleibt ZIP, wenn kein OOXML erkannt wird
    Angenommen die Datei "sample.zip"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Zip"

  @positiv @detector @zip
  Szenariogrundriss: OOXML-ZIP wird auf den konkreten Typ verfeinert
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "<typ>"

    Beispiele:
      | datei       | typ  |
      | sample.docx | Docx |
      | sample.xlsx | Xlsx |
      | sample.pptx | Pptx |

  @negativ @detector @zip
  Szenariogrundriss: Marker-only-ZIP wird nicht als OOXML verfeinert
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Zip"

    Beispiele:
      | datei                        |
      | invalid_docx_marker_only.zip |
      | invalid_xlsx_marker_only.zip |
      | invalid_pptx_marker_only.zip |
