Option Strict On
Option Explicit On

Imports HeyRed.Mime

Namespace FileTypeDetection

    ''' <summary>
    ''' Wrapper um vorhandenes Mime-Paket (keine neuen Abhängigkeiten).
    ''' </summary>
    Friend NotInheritable Class MimeProvider
        Public Shared ReadOnly Instance As New MimeProvider()
        Private Sub New()
        End Sub

        Friend Function GetMime(extWithDot As String) As String
            If String.IsNullOrWhiteSpace(extWithDot) Then Return String.Empty
            Dim ext = extWithDot
            If Not ext.StartsWith(".", StringComparison.Ordinal) Then ext = "." & ext
            Try
                Return MimeTypesMap.GetMimeType(ext)
            Catch
                Return String.Empty
            End Try
        End Function
    End Class

End Namespace
