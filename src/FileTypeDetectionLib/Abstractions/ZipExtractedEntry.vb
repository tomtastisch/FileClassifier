Option Strict On
Option Explicit On

Imports System.Collections.Immutable
Imports System.IO

Namespace FileTypeDetection

    ''' <summary>
    ''' Unveraenderliches In-Memory-Ergebnis einer sicheren ZIP-Extraktion.
    ''' </summary>
    Public NotInheritable Class ZipExtractedEntry

        Public ReadOnly Property RelativePath As String
        Public ReadOnly Property Content As ImmutableArray(Of Byte)
        Public ReadOnly Property Size As Integer

        Friend Sub New(relativePath As String, content As Byte())
            Me.RelativePath = If(relativePath, String.Empty)
            If content Is Nothing OrElse content.Length = 0 Then
                Me.Content = ImmutableArray(Of Byte).Empty
                Me.Size = 0
            Else
                Me.Content = ImmutableArray.Create(content)
                Me.Size = content.Length
            End If
        End Sub

        Public Function OpenReadOnlyStream() As MemoryStream
            Dim data = If(Content.IsDefaultOrEmpty, Array.Empty(Of Byte)(), Content.ToArray())
            Return New MemoryStream(data, writable:=False)
        End Function

    End Class

End Namespace
