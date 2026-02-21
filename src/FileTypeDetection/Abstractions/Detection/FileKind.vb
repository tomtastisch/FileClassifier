' ============================================================================
' FILE: FileKind.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

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
        '''     Office-Word-Dokument (DOC).
        ''' </summary>
        Doc

        ''' <summary>
        '''     Office-Excel-Dokument (XLS).
        ''' </summary>
        Xls

        ''' <summary>
        '''     Office-PowerPoint-Dokument (PPT).
        ''' </summary>
        Ppt
    End Enum
End Namespace
