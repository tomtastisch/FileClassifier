' ============================================================================
' FILE: HashEvidence.vb
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
        Public ReadOnly Property CompressedBytes As Immutable.ImmutableArray(Of Byte)

        ''' <summary>
        '''     Optional mitgeführte unkomprimierte bzw. logische Bytes.
        ''' </summary>
        Public ReadOnly Property UncompressedBytes As Immutable.ImmutableArray(Of Byte)

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

        ''' <summary>
        '''     Interner Vollkonstruktor zur deterministischen Erzeugung eines Evidence-Snapshots.
        ''' </summary>
        ''' <param name="sourceType">Herkunftskanal der Hashquelle.</param>
        ''' <param name="label">Fachliches Quelllabel.</param>
        ''' <param name="detectedType">Ermittelter Dateitypkontext.</param>
        ''' <param name="entry">Optionaler Archiveintrag.</param>
        ''' <param name="compressedBytes">Optionale komprimierte Bytes als Quelle für defensive Kopie.</param>
        ''' <param name="uncompressedBytes">Optionale unkomprimierte/logische Bytes als Quelle für defensive Kopie.</param>
        ''' <param name="entryCount">Anzahl berücksichtigter Entries (wird auf >= 0 normalisiert).</param>
        ''' <param name="totalUncompressedBytes">Gesamtgröße der Nutzdaten in Bytes (wird auf >= 0 normalisiert).</param>
        ''' <param name="digests">Deterministischer Digest-Satz.</param>
        ''' <param name="notes">Ergänzende Hinweise.</param>
        Friend Sub New _
            (
                sourceType As HashSourceType,
                label As String,
                detectedType As FileType,
                entry As ZipExtractedEntry,
                compressedBytes As Byte(),
                uncompressedBytes As Byte(),
                entryCount As Integer,
                totalUncompressedBytes As Long,
                digests As HashDigestSet,
                notes As String
            )

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

        ''' <summary>
        '''     Erzeugt einen fail-closed Evidence-Snapshot für Fehlerpfade.
        ''' </summary>
        ''' <param name="sourceType">Herkunftskanal der Hashquelle.</param>
        ''' <param name="label">Fachliches Quelllabel.</param>
        ''' <param name="notes">Fehler-/Hinweistext.</param>
        ''' <returns>Evidence mit leerem Digest-Satz und UNKNOWN-Typkontext.</returns>
        Friend Shared Function CreateFailure _
            (
                sourceType As HashSourceType,
                label As String,
                notes As String
            ) As HashEvidence

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

        ''' <summary>
        '''     Erstellt aus einem Bytearray eine unveränderliche Kopie.
        ''' </summary>
        ''' <param name="data">Quellbytes oder <c>Nothing</c>.</param>
        ''' <returns>Leeres ImmutableArray bei fehlendem Inhalt, sonst defensive Kopie.</returns>
        Private Shared Function ToImmutable _
            (
                data As Byte()
            ) As Immutable.ImmutableArray(Of Byte)

            If Not ByteArrayGuard.HasContent(data) Then
                Return Immutable.ImmutableArray(Of Byte).Empty
            End If

            Return Immutable.ImmutableArray.Create(data)
        End Function
    End Class
End Namespace
