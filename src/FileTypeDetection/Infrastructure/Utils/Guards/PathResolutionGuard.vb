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
        Private Sub New()
        End Sub

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
