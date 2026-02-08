Option Strict On
Option Explicit On
Option Infer On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Kanonische, in der Bibliothek unterstuetzte Dateitypen.
    '''     Fachlicher Kontext:
    '''     - DOCX/XLSX/PPTX sind fachlich ZIP-Container und werden erst nach ZIP-Gate verfeinert.
    '''     - Archiv-Aliase wie tar/tgz/gz/bz2/xz/7z/rar/zz werden auf Kind Zip normalisiert.
    ''' </summary>
    Public Enum FileKind
        Unknown = 0

        Pdf
        Png
        Jpeg
        Gif
        Webp
        Zip

        Docx
        Xlsx
        Pptx
    End Enum
End Namespace
