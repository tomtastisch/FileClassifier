' ============================================================================
' FILE: IOGuards.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.IO

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     Interne Hilfsklasse <c>InternalIoDefaults</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class InternalIoDefaults
        Friend Const CopyBufferSize As Integer = 8192
        Friend Const FileStreamBufferSize As Integer = 81920
        Friend Const DefaultSniffBytes As Integer = 4096

        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über Konstanten.
        ''' </summary>
        Private Sub New()
        End Sub
    End Class

    ''' <summary>
    '''     Zentrale IO-Helfer für harte Grenzen.
    '''     SSOT-Regel: bounded copy wird nur hier gepflegt.
    ''' </summary>
    Friend NotInheritable Class StreamBounds
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Kopiert einen Stream mit harter Byte-Grenze fail-closed.
        ''' </summary>
        ''' <param name="input">Lesbarer Eingabestream.</param>
        ''' <param name="output">Schreibbarer Ausgabestream.</param>
        ''' <param name="maxBytes">Maximal erlaubte kopierte Byteanzahl.</param>
        Friend Shared Sub CopyBounded _
            (
                input As Stream,
                output As Stream,
                maxBytes As Long
            )

            Dim buf(InternalIoDefaults.CopyBufferSize - 1) As Byte
            Dim total                                      As Long    = 0
            Dim n                                          As Integer

            While True
                n = input.Read(buf, 0, buf.Length)
                If n <= 0 Then Exit While

                total += n
                If total > maxBytes Then Throw New InvalidOperationException("bounded copy exceeded")
                output.Write(buf, 0, n)
            End While
        End Sub
    End Class

    ''' <summary>
    '''     Kleine, zentrale Stream-Guards, um duplizierte Pattern-Checks in Archivroutinen zu reduzieren.
    '''     Keine Semantik: reine Abfrage/Positionierung.
    ''' </summary>
    Friend NotInheritable Class StreamGuard
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft, ob ein Stream gesetzt und lesbar ist.
        ''' </summary>
        ''' <param name="stream">Zu prüfender Stream.</param>
        Friend Shared Function IsReadable _
            (
                stream As Stream
            ) As Boolean

            Return stream IsNot Nothing AndAlso stream.CanRead
        End Function

        ''' <summary>
        '''     Setzt einen seekfähigen Stream deterministisch auf Position 0 zurück.
        ''' </summary>
        ''' <param name="stream">Zurückzusetzender Stream.</param>
        Friend Shared Sub RewindToStart _
            (
                stream As Stream
            )

            If stream Is Nothing Then Return
            If stream.CanSeek Then stream.Position = 0
        End Sub
    End Class

End Namespace
