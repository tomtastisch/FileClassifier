' ============================================================================
' FILE: LogGuard.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System
Imports Microsoft.Extensions.Logging

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     Defensiver Logger-Schutz.
    '''     Logging darf niemals zu Erkennungsfehlern oder Exceptions führen.
    ''' </summary>
    Friend NotInheritable Class LogGuard
        Private Sub New()
        End Sub

        Friend Shared Sub Debug _
            (
                logger As ILogger,
                message As String
            )
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Debug) Then Return
            Try
                logger.LogDebug("{Message}", message)
            Catch ex As Exception When _
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException OrElse
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub

        Friend Shared Sub Warn _
            (
                logger As ILogger,
                message As String
            )
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Warning) Then Return
            Try
                logger.LogWarning("{Message}", message)
            Catch ex As Exception When _
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException OrElse
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub

        Friend Shared Sub [Error] _
            (
                logger As ILogger,
                message As String,
                ex As Exception
            )
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Error) Then Return
            Try
                logger.LogError(ex, "{Message}", message)
            Catch logEx As Exception When _
                TypeOf logEx Is InvalidOperationException OrElse
                TypeOf logEx Is ObjectDisposedException OrElse
                TypeOf logEx Is FormatException OrElse
                TypeOf logEx Is ArgumentException
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub
    End Class

End Namespace
