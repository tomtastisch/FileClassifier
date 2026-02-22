' ============================================================================
' FILE: PathResolutionGuard.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports Tomtastisch.FileClassifier

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     Zentrale FullPath-Auflösung mit fail-closed Fehlerbehandlung und konfigurierbarer Protokollstufe.
    ''' </summary>
    Friend NotInheritable Class PathResolutionGuard
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Ermittelt einen vollqualifizierten Pfad mit fail-closed Fehlerbehandlung.
        ''' </summary>
        ''' <param name="rawPath">Unaufgelöster Eingabepfad.</param>
        ''' <param name="opt">Laufzeitoptionen inklusive Logger.</param>
        ''' <param name="logPrefix">Präfix für Logmeldungen.</param>
        ''' <param name="warnLevel"><c>True</c> für Warn-Logging, sonst Debug-Logging.</param>
        ''' <param name="fullPath">Ausgabeparameter für den aufgelösten Pfad.</param>
        ''' <returns><c>True</c>, wenn die Auflösung erfolgreich war.</returns>
        Friend Shared Function TryGetFullPath _
            (
                rawPath As String,
                opt As FileTypeProjectOptions,
                logPrefix As String,
                warnLevel As Boolean,
                ByRef fullPath As String
            ) As Boolean

            Dim message As String

            fullPath = String.Empty

            Try
                fullPath = Path.GetFullPath(rawPath)
                Return True
            Catch ex As Exception When ExceptionFilterGuard.IsPathResolutionException(ex)

                If opt IsNot Nothing Then
                    message = $"{logPrefix}: {ex.Message}"
                    If warnLevel Then
                        LogGuard.Warn(opt.Logger, message)
                    Else
                        LogGuard.Debug(opt.Logger, message)
                    End If
                End If

                Return False
            End Try
        End Function
    End Class

End Namespace
