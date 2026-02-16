Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Quelle eines Hash-Nachweises innerhalb der Evidence-Pipeline.
    ''' </summary>
    ''' <remarks>
    '''     Die Werte dienen der auditierbaren Kennzeichnung, aus welchem Verarbeitungskanal ein Evidence-Objekt stammt.
    ''' </remarks>
    Public Enum HashSourceType
        ''' <summary>
        '''     Quelle konnte nicht zuverlässig bestimmt werden.
        ''' </summary>
        Unknown = 0

        ''' <summary>
        '''     Nachweis aus einem Dateipfad.
        ''' </summary>
        FilePath = 1

        ''' <summary>
        '''     Nachweis aus Rohbytes im Arbeitsspeicher.
        ''' </summary>
        RawBytes = 2

        ''' <summary>
        '''     Nachweis aus bereits extrahierten Archiveinträgen.
        ''' </summary>
        ArchiveEntries = 3

        ''' <summary>
        '''     Nachweis aus einer materialisierten Datei im RoundTrip.
        ''' </summary>
        MaterializedFile = 4
    End Enum
End Namespace
