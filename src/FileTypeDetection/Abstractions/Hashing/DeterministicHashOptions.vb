Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Steuerung fuer deterministic hashing APIs.
    ''' </summary>
    Public NotInheritable Class DeterministicHashOptions
        ''' <summary>
        '''     Wenn True, werden komprimierte und unkomprimierte Bytes in Evidence als Kopie mitgefuehrt.
        ''' </summary>
        Public Property IncludePayloadCopies As Boolean = False

        ''' <summary>
        '''     Wenn True, wird zusaetzlich ein schneller XxHash3-Digest berechnet.
        ''' </summary>
        Public Property IncludeFastHash As Boolean = True

        ''' <summary>
        '''     Dateiname fuer den Materialisierungs-Schritt im RoundTrip-Report.
        ''' </summary>
        Public Property MaterializedFileName As String = "deterministic-roundtrip.bin"

        Friend Function Clone() As DeterministicHashOptions
            Return New DeterministicHashOptions With {
                .IncludePayloadCopies = IncludePayloadCopies,
                .IncludeFastHash = IncludeFastHash,
                .MaterializedFileName = If(MaterializedFileName, String.Empty)
                }
        End Function

        Friend Shared Function Normalize(options As DeterministicHashOptions) As DeterministicHashOptions
            If options Is Nothing Then options = New DeterministicHashOptions()

            Dim cloned = options.Clone()
            cloned.MaterializedFileName = NormalizeMaterializedFileName(cloned.MaterializedFileName)
            Return cloned
        End Function

        Private Shared Function NormalizeMaterializedFileName(candidate As String) As String
            Dim normalized = If(candidate, String.Empty).Trim()
            If String.IsNullOrWhiteSpace(normalized) Then Return "deterministic-roundtrip.bin"

            Try
                normalized = Path.GetFileName(normalized)
            Catch
                Return "deterministic-roundtrip.bin"
            End Try

            If String.IsNullOrWhiteSpace(normalized) Then Return "deterministic-roundtrip.bin"

            If Path.GetInvalidFileNameChars().Any(Function(invalidChar) normalized.Contains(invalidChar)) Then
                Return "deterministic-roundtrip.bin"
            End If

            Return normalized
        End Function
    End Class
End Namespace
