' ============================================================================
' FILE: FileTypeProjectBaseline.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.md
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Stellt ein konservatives, deterministisches Sicherheitsprofil für die Bibliothek bereit.
    ''' </summary>
    ''' <remarks>
    '''     Das Profil bündelt fail-closed Standardgrenzen für Detektion, Archivverarbeitung und Hashing, die auf
    '''     produktionsnahe Sicherheitsanforderungen ausgerichtet sind.
    ''' </remarks>
    Public NotInheritable Class FileTypeProjectBaseline
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Erzeugt ein hartes Default-Profil für produktive, fail-closed Szenarien.
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
                .DeterministicHash = New HashOptions With {
                    .IncludePayloadCopies = False,
                    .IncludeFastHash = True,
                    .MaterializedFileName = "deterministic-roundtrip.bin"
                    }
                }
        End Function

        ''' <summary>
        '''     Aktiviert das deterministische Sicherheitsprofil global als neuen Default-Snapshot.
        ''' </summary>
        ''' <remarks>
        '''     Nebenwirkung: Nach erfolgreichem Aufruf verwenden nachfolgende Bibliotheksoperationen die gesetzten
        '''     Baseline-Werte, sofern sie keinen expliziten Snapshot erhalten.
        ''' </remarks>
        Public Shared Sub ApplyDeterministicDefaults()
            FileTypeOptions.SetSnapshot(CreateDeterministicDefaults())
        End Sub
    End Class
End Namespace
