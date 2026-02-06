Option Strict On
Option Explicit On

Namespace FileTypeDetection
    ''' <summary>
    '''     Liefert ein konservatives, deterministisches Sicherheitsprofil für die Dateityp-Erkennung.
    ''' </summary>
    Public NotInheritable Class FileTypeProjectBaseline
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Erzeugt ein hartes Default-Profil fuer produktive, fail-closed Szenarien.
        ''' </summary>
        Private Shared Function CreateDeterministicDefaults() As FileTypeProjectOptions
            Return New FileTypeProjectOptions With {
                .MaxBytes = 128L * 1024L * 1024L,
                .SniffBytes = 64 * 1024,
                .MaxZipEntries = 3000,
                .MaxZipTotalUncompressedBytes = 300L * 1024L * 1024L,
                .MaxZipEntryUncompressedBytes = 64L * 1024L * 1024L,
                .MaxZipCompressionRatio = 30,
                .MaxZipNestingDepth = 2,
                .MaxZipNestedBytes = 32L * 1024L * 1024L,
                .RejectArchiveLinks = True,
                .AllowUnknownArchiveEntrySize = False,
                .DeterministicHash = New DeterministicHashOptions With {
                    .IncludePayloadCopies = False,
                    .IncludeFastHash = True,
                    .MaterializedFileName = "deterministic-roundtrip.bin"
                    }
                }
        End Function

        ''' <summary>
        '''     Aktiviert das Sicherheitsprofil global als neue Default-Optionen.
        ''' </summary>
        Public Shared Sub ApplyDeterministicDefaults()
            FileTypeOptions.SetSnapshot(CreateDeterministicDefaults())
        End Sub
    End Class
End Namespace
