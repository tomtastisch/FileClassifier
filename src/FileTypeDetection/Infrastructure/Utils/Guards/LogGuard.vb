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
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Schreibt eine Debug-Meldung fail-closed.
        ''' </summary>
        ''' <param name="logger">Ziel-Logger.</param>
        ''' <param name="message">Zu schreibende Meldung.</param>
        Friend Shared Sub Debug _
            (
                logger As ILogger,
                message As String
            )

            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Debug) Then Return
            Try
                logger.LogDebug("{Message}", message)
            Catch ex As Exception When ExceptionFilterGuard.IsLoggerWriteException(ex)
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub

        ''' <summary>
        '''     Schreibt eine Warn-Meldung fail-closed.
        ''' </summary>
        ''' <param name="logger">Ziel-Logger.</param>
        ''' <param name="message">Zu schreibende Meldung.</param>
        Friend Shared Sub Warn _
            (
                logger As ILogger,
                message As String
            )

            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Warning) Then Return
            Try
                logger.LogWarning("{Message}", message)
            Catch ex As Exception When ExceptionFilterGuard.IsLoggerWriteException(ex)
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub

        ''' <summary>
        '''     Schreibt eine Error-Meldung inklusive Exception fail-closed.
        ''' </summary>
        ''' <param name="logger">Ziel-Logger.</param>
        ''' <param name="message">Zu schreibende Meldung.</param>
        ''' <param name="ex">Optionale Kontext-Exception für den Logeintrag.</param>
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
            Catch logEx As Exception When ExceptionFilterGuard.IsLoggerWriteException(logEx)
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub
    End Class

End Namespace
