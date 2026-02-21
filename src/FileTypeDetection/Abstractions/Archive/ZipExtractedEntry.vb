' ============================================================================
' FILE: ZipExtractedEntry.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Collections.Immutable
Imports System.IO

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Unveränderliches In-Memory-Modell eines sicher extrahierten Archiveintrags.
    ''' </summary>
    ''' <remarks>
    '''     Das Modell kapselt den normalisierten relativen Pfad und den unveränderlichen Byteinhalt eines Eintrags.
    '''     Es ist für externe Konsumenten als read-only Datenträger vorgesehen.
    ''' </remarks>
    Public NotInheritable Class ZipExtractedEntry
        ''' <summary>
        '''     Normalisierter relativer Eintragspfad innerhalb des Archivs.
        ''' </summary>
        Public ReadOnly Property RelativePath As String

        ''' <summary>
        '''     Unveränderlicher Byteinhalt des Eintrags.
        ''' </summary>
        Public ReadOnly Property Content As ImmutableArray(Of Byte)

        ''' <summary>
        '''     Größe des Eintragsinhalts in Bytes.
        ''' </summary>
        Public ReadOnly Property Size As Integer

        Friend Sub New _
            (
                entryPath As String,
                payload As Byte()
            )

            RelativePath = If(entryPath, String.Empty)
            If payload Is Nothing OrElse payload.Length = 0 Then
                Content = ImmutableArray(Of Byte).Empty
                Size = 0
            Else
                Content = ImmutableArray.Create(payload)
                Size = payload.Length
            End If
        End Sub

        ''' <summary>
        '''     Öffnet einen schreibgeschützten Speicherstream auf den Entry-Inhalt.
        ''' </summary>
        ''' <remarks>
        '''     Der zurückgegebene Stream basiert auf einer isolierten Bytekopie und kann vom Aufrufer sicher gelesen werden.
        ''' </remarks>
        ''' <returns>Schreibgeschützter <see cref="MemoryStream"/> mit dem Entry-Inhalt.</returns>
        Public Function OpenReadOnlyStream() As MemoryStream

            Dim data = If(Content.IsDefaultOrEmpty, Array.Empty(Of Byte)(), Content.ToArray())
            Return New MemoryStream(data, writable:=False)
        End Function
    End Class
End Namespace
