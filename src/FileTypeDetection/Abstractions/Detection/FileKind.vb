Option Strict On
Option Explicit On
Option Infer On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Kanonische, in der Bibliothek unterstützte Dateitypen.
    ''' </summary>
    ''' <remarks>
    '''     DOCX/XLSX/PPTX sind fachlich ZIP-Container und werden nach Archiv-Gate über strukturiertes Refinement
    '''     verfeinert. Archiv-Aliase werden intern auf <see cref="Zip"/> normalisiert.
    ''' </remarks>
    Public Enum FileKind
        ''' <summary>
        '''     Unbekannter oder nicht sicher klassifizierbarer Typ (fail-closed).
        ''' </summary>
        Unknown = 0

        ''' <summary>
        '''     PDF-Dokument.
        ''' </summary>
        Pdf

        ''' <summary>
        '''     PNG-Bilddatei.
        ''' </summary>
        Png

        ''' <summary>
        '''     JPEG-Bilddatei.
        ''' </summary>
        Jpeg

        ''' <summary>
        '''     GIF-Bilddatei.
        ''' </summary>
        Gif

        ''' <summary>
        '''     WebP-Bilddatei.
        ''' </summary>
        Webp

        ''' <summary>
        '''     ZIP-Container oder auf ZIP normalisierte Archivfamilie.
        ''' </summary>
        Zip

        ''' <summary>
        '''     Office Open XML Word-Dokument (DOCX).
        ''' </summary>
        Docx

        ''' <summary>
        '''     Office Open XML Excel-Dokument (XLSX).
        ''' </summary>
        Xlsx

        ''' <summary>
        '''     Office Open XML PowerPoint-Dokument (PPTX).
        ''' </summary>
        Pptx
    End Enum
End Namespace
