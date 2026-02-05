# language: de
@integration @archive @refinement @detector @processing
Funktionalität: Integration von Archiv-Gate, strukturellem Refinement und Archiv-Fassade

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

  @positiv @detector @archive
  Szenario: Archiv bleibt beim generischen Archivtyp, wenn kein OOXML erkannt wird
    Angenommen die Datei "sample.zip"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Zip"

  @positiv @detector @archive
  Szenariogrundriss: OOXML-Archiv wird auf den konkreten Typ verfeinert
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "<typ>"

    Beispiele:
      | datei       | typ  |
      | sample.docx | Docx |
      | sample.xlsx | Xlsx |
      | sample.pptx | Pptx |

  @negativ @detector @archive
  Szenariogrundriss: Marker-only-Archiv wird nicht als OOXML verfeinert
    Angenommen die Datei "<datei>"
    Wenn ich den Dateityp ermittle
    Dann ist der erkannte Typ "Zip"

    Beispiele:
      | datei                        |
      | invalid_docx_marker_only.zip |
      | invalid_xlsx_marker_only.zip |
      | invalid_pptx_marker_only.zip |
