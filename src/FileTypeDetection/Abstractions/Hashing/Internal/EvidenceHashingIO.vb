' ============================================================================
' FILE: EvidenceHashingIO.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Interne I/O-Hilfsfunktionen für bounded Dateieinlesung im Hashing-Kontext.
    ''' </summary>
    ''' <remarks>
    '''     Die Komponente erzwingt MaxBytes-Limits fail-closed und liefert deterministische Fehltexte.
    ''' </remarks>
    Friend NotInheritable Class EvidenceHashingIO
        Private Sub New()
        End Sub

        Friend Shared Function TryReadFileBounded _
            (
                path As String,
                detectorOptions As FileTypeProjectOptions,
                ByRef bytes As Byte(),
                ByRef errorMessage As String
            ) As Boolean

            Dim fi As IO.FileInfo

            bytes = Array.Empty(Of Byte)()
            errorMessage = String.Empty

            If String.IsNullOrWhiteSpace(path) Then
                errorMessage = "Pfad ist leer."
                Return False
            End If

            If detectorOptions Is Nothing Then
                errorMessage = "Optionen fehlen."
                Return False
            End If

            Try
                fi = New IO.FileInfo(path)
                If Not fi.Exists Then
                    errorMessage = "Datei existiert nicht."
                    Return False
                End If

                If fi.Length > detectorOptions.MaxBytes Then
                    errorMessage = "Datei größer als MaxBytes."
                    Return False
                End If

                Using fs As New IO.FileStream(
                    path,
                    IO.FileMode.Open,
                    IO.FileAccess.Read,
                    IO.FileShare.Read,
                    InternalIoDefaults.FileStreamBufferSize,
                    IO.FileOptions.SequentialScan)

                    Using ms As New IO.MemoryStream(CInt(Math.Min(Math.Max(fi.Length, 0), Integer.MaxValue)))
                        StreamBounds.CopyBounded(fs, ms, detectorOptions.MaxBytes)
                        bytes = ms.ToArray()
                    End Using
                End Using

                Return True
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IO.IOException OrElse
                TypeOf ex Is IO.InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return SetReadFileError(ex, errorMessage)
            End Try
        End Function

        Friend Shared Function SetReadFileError _
            (
                ex As Exception,
                ByRef errorMessage As String
            ) As Boolean

            errorMessage = $"Datei konnte nicht gelesen werden: {ex.Message}"
            Return False
        End Function
    End Class
End Namespace
