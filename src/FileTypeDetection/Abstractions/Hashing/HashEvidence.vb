' ============================================================================
' FILE: HashEvidence.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.md
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Nachweisobjekt für einen deterministischen Hash-Schritt.
    ''' </summary>
    ''' <remarks>
    '''     Das Objekt kapselt Herkunft, Typkontext, optionale Payloadkopien, Digest-Satz sowie ergänzende Notes in
    '''     unveränderlicher Form für externe Auswertung.
    ''' </remarks>
    Public NotInheritable Class HashEvidence
        ''' <summary>
        '''     Herkunftskanal des Nachweises.
        ''' </summary>
        Public ReadOnly Property SourceType As HashSourceType

        ''' <summary>
        '''     Fachliches Label der Eingabequelle.
        ''' </summary>
        Public ReadOnly Property Label As String

        ''' <summary>
        '''     Ermittelter Dateitypkontext für den Nachweis.
        ''' </summary>
        Public ReadOnly Property DetectedType As FileType

        ''' <summary>
        '''     Optionaler Beispiel-Entry bei archivbasierten Nachweisen.
        ''' </summary>
        Public ReadOnly Property Entry As ZipExtractedEntry

        ''' <summary>
        '''     Optional mitgeführte komprimierte Bytes.
        ''' </summary>
        Public ReadOnly Property CompressedBytes As Global.System.Collections.Immutable.ImmutableArray(Of Byte)

        ''' <summary>
        '''     Optional mitgeführte unkomprimierte bzw. logische Bytes.
        ''' </summary>
        Public ReadOnly Property UncompressedBytes As Global.System.Collections.Immutable.ImmutableArray(Of Byte)

        ''' <summary>
        '''     Anzahl berücksichtigter Entries im Nachweis.
        ''' </summary>
        Public ReadOnly Property EntryCount As Integer

        ''' <summary>
        '''     Gesamtgröße unkomprimierter Nutzdaten in Bytes.
        ''' </summary>
        Public ReadOnly Property TotalUncompressedBytes As Long

        ''' <summary>
        '''     Deterministischer Digest-Satz des Nachweises.
        ''' </summary>
        Public ReadOnly Property Digests As HashDigestSet

        ''' <summary>
        '''     Ergänzende Hinweise, z. B. zu Fehlern oder Sicherheitsaspekten.
        ''' </summary>
        Public ReadOnly Property Notes As String

        Friend Sub New(
                       sourceType As HashSourceType,
                       label As String,
                       detectedType As FileType,
                       entry As ZipExtractedEntry,
                       compressedBytes As Byte(),
                       uncompressedBytes As Byte(),
                       entryCount As Integer,
                       totalUncompressedBytes As Long,
                       digests As HashDigestSet,
                       notes As String)
            Me.SourceType = sourceType
            Me.Label = If(label, String.Empty)
            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.Entry = entry
            Me.EntryCount = Math.Max(0, entryCount)
            Me.TotalUncompressedBytes = Math.Max(0, totalUncompressedBytes)
            Me.Digests = If(digests, HashDigestSet.Empty)
            Me.Notes = If(notes, String.Empty)
            Me.CompressedBytes = ToImmutable(compressedBytes)
            Me.UncompressedBytes = ToImmutable(uncompressedBytes)
        End Sub

        Friend Shared Function CreateFailure(sourceType As HashSourceType, label As String, notes As String) _
            As HashEvidence
            Return New HashEvidence(
                sourceType:=sourceType,
                label:=label,
                detectedType:=FileTypeRegistry.Resolve(FileKind.Unknown),
                entry:=Nothing,
                compressedBytes:=Array.Empty(Of Byte)(),
                uncompressedBytes:=Array.Empty(Of Byte)(),
                entryCount:=0,
                totalUncompressedBytes:=0,
                digests:=HashDigestSet.Empty,
                notes:=notes)
        End Function

        Private Shared Function ToImmutable(data As Byte()) As Global.System.Collections.Immutable.ImmutableArray(Of Byte)
            If data Is Nothing OrElse data.Length = 0 Then
                Return Global.System.Collections.Immutable.ImmutableArray(Of Byte).Empty
            End If
            Return Global.System.Collections.Immutable.ImmutableArray.Create(data)
        End Function
    End Class
End Namespace
