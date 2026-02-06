Option Strict On
Option Explicit On

Imports System.Collections.Immutable
Imports System.IO

Namespace FileTypeDetection
    ''' <summary>
    '''     Unveraenderliches In-Memory-Ergebnis einer sicheren ZIP-Extraktion.
    ''' </summary>
    Public NotInheritable Class ZipExtractedEntry
        Public ReadOnly Property RelativePath As String
        Public ReadOnly Property Content As ImmutableArray(Of Byte)
        Public ReadOnly Property Size As Integer

        Friend Sub New(entryPath As String, payload As Byte())
            RelativePath = If(entryPath, String.Empty)
            If payload Is Nothing OrElse payload.Length = 0 Then
                Content = ImmutableArray(Of Byte).Empty
                Size = 0
            Else
                Content = ImmutableArray.Create(payload)
                Size = payload.Length
            End If
        End Sub

        Public Function OpenReadOnlyStream() As MemoryStream
            Dim data = If(Content.IsDefaultOrEmpty, Array.Empty(Of Byte)(), Content.ToArray())
            Return New MemoryStream(data, writable:=False)
        End Function
    End Class
End Namespace
