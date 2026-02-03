Option Strict On
Option Explicit On

Namespace FileTypeDetection

    ''' <summary>
    ''' Liefert ein konservatives, deterministisches Sicherheitsprofil fuer die Dateityp-Erkennung.
    ''' </summary>
    Public NotInheritable Class FileTypeSecurityBaseline
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Erzeugt ein hartes Default-Profil fuer produktive, fail-closed Szenarien.
        ''' </summary>
        Public Shared Function CreateDeterministicDefaults() As FileTypeDetectorOptions
            Return New FileTypeDetectorOptions With {
                .MaxBytes = 128L * 1024L * 1024L,
                .SniffBytes = 64 * 1024,
                .MaxZipEntries = 3000,
                .MaxZipTotalUncompressedBytes = 300L * 1024L * 1024L,
                .MaxZipEntryUncompressedBytes = 64L * 1024L * 1024L,
                .MaxZipCompressionRatio = 30,
                .MaxZipNestingDepth = 2,
                .MaxZipNestedBytes = 32L * 1024L * 1024L
            }
        End Function

        ''' <summary>
        ''' Aktiviert das Sicherheitsprofil global als neue Default-Optionen.
        ''' </summary>
        Public Shared Sub ApplyDeterministicDefaults()
            FileTypeDetector.SetDefaultOptions(CreateDeterministicDefaults())
        End Sub
    End Class

End Namespace
