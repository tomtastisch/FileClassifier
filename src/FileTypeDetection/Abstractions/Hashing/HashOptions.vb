' ============================================================================
' FILE: HashOptions.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Diagnostics.CodeAnalysis

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Konfiguriert das Verhalten der öffentlichen deterministischen Hashing-APIs.
    ''' </summary>
    ''' <remarks>
    '''     Die Optionen steuern, welche Digest-Arten berechnet und welche Payloadkopien in Evidence-Objekten mitgeführt werden.
    '''     Ungültige Dateinamen für Materialisierung werden intern auf einen sicheren Standardwert normalisiert.
    ''' </remarks>
    Public NotInheritable Class HashOptions
        ''' <summary>
        '''     Wenn True, werden komprimierte und unkomprimierte Bytes in Evidence als Kopie mitgeführt.
        ''' </summary>
        Public Property IncludePayloadCopies As Boolean = False

        ''' <summary>
        '''     Wenn True, wird zusätzlich ein schneller XxHash3-Digest berechnet.
        ''' </summary>
        Public Property IncludeFastHash As Boolean = True

        ''' <summary>
        '''     Wenn True, wird zusätzlich ein optionaler HMAC-SHA256 Digest berechnet (keyed).
        '''     Der Key wird aus der Environment Variable 'FILECLASSIFIER_HMAC_KEY_B64' gelesen.
        '''     Wenn der Key fehlt oder ungültig ist, werden HMAC-Digests leer gelassen und Notes ergänzt.
        ''' </summary>
        Public Property IncludeSecureHash As Boolean = False

        ''' <summary>
        '''     Dateiname für den Materialisierungs-Schritt im RoundTrip-Report.
        ''' </summary>
        Public Property MaterializedFileName As String = "deterministic-roundtrip.bin"

        ''' <summary>
        '''     Erstellt eine interne, seiteneffektfreie Kopie der Optionen.
        ''' </summary>
        ''' <returns>Neue Instanz mit identischen aktuellen Optionswerten.</returns>
        Friend Function Clone() As HashOptions

            Return New HashOptions With {
                    .IncludePayloadCopies = IncludePayloadCopies,
                    .IncludeFastHash = IncludeFastHash,
                    .IncludeSecureHash = IncludeSecureHash,
                    .MaterializedFileName = If(MaterializedFileName, String.Empty)
                }
        End Function

        ''' <summary>
        '''     Normalisiert ein Optionsobjekt fail-closed auf sichere Standardwerte.
        ''' </summary>
        ''' <param name="options">Zu normalisierende Optionen; <c>Nothing</c> erzeugt Standardoptionen.</param>
        ''' <returns>Normalisierte Optionskopie mit sicherem Materialisierungsdateinamen.</returns>
        Friend Shared Function Normalize _
            (
                options As HashOptions
            ) As HashOptions

            Dim cloned As HashOptions
            If options Is Nothing Then options = New HashOptions()

            cloned = options.Clone()
            cloned.MaterializedFileName = NormalizeMaterializedFileName(cloned.MaterializedFileName)
            Return cloned
        End Function

        ''' <summary>
        '''     Validiert und normalisiert den Materialisierungsdateinamen auf einen sicheren, deterministischen Wert.
        ''' </summary>
        ''' <param name="candidate">Kandidat aus den Optionen.</param>
        ''' <returns>
        '''     Sicherheitsnormalisierter Dateiname. Bei ungültigem Eingabewert wird
        '''     <c>deterministic-roundtrip.bin</c> zurückgegeben.
        ''' </returns>
        <SuppressMessage("Usage", "CA2249:Use 'string.Contains' instead of 'string.IndexOf' to improve readability", Justification:="IndexOf bleibt hier für deterministische Zeichenprüfung ohne Semantikänderung bestehen.")>
        Private Shared Function NormalizeMaterializedFileName _
            (
                candidate As String
            ) As String

            Dim normalized = If(candidate, String.Empty).Trim()
            If String.IsNullOrWhiteSpace(normalized) Then Return "deterministic-roundtrip.bin"

            Try
                normalized = IO.Path.GetFileName(normalized)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IO.IOException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return "deterministic-roundtrip.bin"
            End Try

            If String.IsNullOrWhiteSpace(normalized) Then Return "deterministic-roundtrip.bin"

            ' ReSharper disable once LoopCanBeConvertedToQuery
            For Each invalidChar In IO.Path.GetInvalidFileNameChars()
                If normalized.IndexOf(invalidChar) >= 0 Then
                    Return "deterministic-roundtrip.bin"
                End If
            Next

            Return normalized
        End Function
    End Class
End Namespace
