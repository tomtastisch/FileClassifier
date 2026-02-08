Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Quelle eines Hash-Nachweises.
    ''' </summary>
    Public Enum DeterministicHashSourceType
        Unknown = 0
        FilePath = 1
        RawBytes = 2
        ArchiveEntries = 3
        MaterializedFile = 4
    End Enum
End Namespace
