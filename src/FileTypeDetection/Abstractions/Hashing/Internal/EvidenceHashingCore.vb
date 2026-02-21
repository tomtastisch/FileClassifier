' ============================================================================
' FILE: EvidenceHashingCore.vb
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
    '''     Interner, zustandsloser Kernservice für deterministische Evidence-Bildung.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Die Komponente kapselt Normalisierung, Manifestbildung, Digest-Berechnung und optionale HMAC-Verarbeitung
    '''         ohne Public-API-Verantwortung.
    '''     </para>
    '''     <para>
    '''         Fehler werden fail-closed über Rückgabewerte und unveränderte Fehltexte in die aufrufende Fassade propagiert.
    '''     </para>
    ''' </remarks>
    Friend NotInheritable Class EvidenceHashingCore
        Private Sub New()
        End Sub

        Friend Shared Function BuildEvidenceFromEntries _
            (
                sourceType As HashSourceType,
                label As String,
                detectedType As FileType,
                compressedBytes As Byte(),
                entries As IReadOnlyList(Of ZipExtractedEntry),
                hashOptions As HashOptions,
                notes As String
            ) As HashEvidence

            Dim normalizedEntries As List(Of NormalizedEntry) = Nothing
            Dim normalizeError As String = String.Empty
            Dim logicalBytes As Byte()
            Dim logicalSha As String
            Dim fastLogical As String
            Dim hmacLogical As String
            Dim physicalSha As String
            Dim fastPhysical As String
            Dim hmacPhysical As String
            Dim hasPhysical As Boolean
            Dim secureNote As String
            Dim hmacKey As Byte()
            Dim hasHmacKey As Boolean
            Dim firstEntry As ZipExtractedEntry = Nothing
            Dim digestSet As HashDigestSet
            Dim combinedNotes As String
            Dim totalBytes As Long
            Dim persistedCompressed As Byte()
            Dim persistedLogical As Byte()

            If Not TryNormalizeEntries(entries, normalizedEntries, normalizeError) Then
                Return HashEvidence.CreateFailure(sourceType, label, normalizeError)
            End If

            logicalBytes = BuildLogicalManifestBytes(normalizedEntries)
            logicalSha = ComputeSha256Hex(logicalBytes)
            fastLogical = ComputeFastHash(logicalBytes, hashOptions)
            hmacLogical = String.Empty
            physicalSha = String.Empty
            fastPhysical = String.Empty
            hmacPhysical = String.Empty
            hasPhysical = False
            secureNote = String.Empty
            hmacKey = Array.Empty(Of Byte)()
            hasHmacKey = False

            If hashOptions IsNot Nothing AndAlso hashOptions.IncludeSecureHash Then
                hasHmacKey = TryResolveHmacKey(hmacKey, secureNote)
                If hasHmacKey Then
                    hmacLogical = ComputeHmacSha256Hex(hmacKey, logicalBytes)
                End If
            End If

            If compressedBytes IsNot Nothing AndAlso compressedBytes.Length > 0 Then
                physicalSha = ComputeSha256Hex(compressedBytes)
                fastPhysical = ComputeFastHash(compressedBytes, hashOptions)
                hasPhysical = True
                If hasHmacKey Then
                    hmacPhysical = ComputeHmacSha256Hex(hmacKey, compressedBytes)
                End If
            End If

            If normalizedEntries.Count > 0 Then
                firstEntry = New ZipExtractedEntry(normalizedEntries(0).RelativePath, normalizedEntries(0).Content)
            End If

            digestSet = New HashDigestSet(
                physicalSha256:=physicalSha,
                logicalSha256:=logicalSha,
                fastPhysicalXxHash3:=fastPhysical,
                fastLogicalXxHash3:=fastLogical,
                hmacPhysicalSha256:=hmacPhysical,
                hmacLogicalSha256:=hmacLogical,
                hasPhysicalHash:=hasPhysical,
                hasLogicalHash:=True)

            combinedNotes = AppendNoteIfAny(notes, secureNote)

            totalBytes = 0
            For Each entry In normalizedEntries
                totalBytes += CLng(entry.Content.LongLength)
            Next

            persistedCompressed = If(
                hashOptions.IncludePayloadCopies,
                CopyBytes(compressedBytes),
                Array.Empty(Of Byte)())

            persistedLogical = If(
                hashOptions.IncludePayloadCopies,
                CopyBytes(logicalBytes),
                Array.Empty(Of Byte)())

            Return New HashEvidence(
                sourceType:=sourceType,
                label:=NormalizeLabel(label),
                detectedType:=If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown)),
                entry:=firstEntry,
                compressedBytes:=persistedCompressed,
                uncompressedBytes:=persistedLogical,
                entryCount:=normalizedEntries.Count,
                totalUncompressedBytes:=totalBytes,
                digests:=digestSet,
                notes:=combinedNotes)
        End Function

        Friend Shared Function BuildEvidenceFromRawPayload _
            (
                sourceType As HashSourceType,
                label As String,
                detectedType As FileType,
                payload As Byte(),
                hashOptions As HashOptions,
                notes As String
            ) As HashEvidence

            Dim safePayload As Byte() = If(payload, Array.Empty(Of Byte)())
            Dim physicalSha As String = ComputeSha256Hex(safePayload)
            Dim logicalSha As String = physicalSha
            Dim fastPhysical As String = ComputeFastHash(safePayload, hashOptions)
            Dim fastLogical As String = fastPhysical
            Dim hmacPhysical As String = String.Empty
            Dim hmacLogical As String = String.Empty
            Dim secureNote As String = String.Empty
            Dim hmacKey As Byte() = Array.Empty(Of Byte)()
            Dim persistedPayload As Byte()
            Dim entry As ZipExtractedEntry
            Dim digestSet As HashDigestSet
            Dim combinedNotes As String

            If hashOptions IsNot Nothing AndAlso hashOptions.IncludeSecureHash Then
                If TryResolveHmacKey(hmacKey, secureNote) Then
                    hmacPhysical = ComputeHmacSha256Hex(hmacKey, safePayload)
                    hmacLogical = hmacPhysical
                End If
            End If

            persistedPayload = If(
                hashOptions.IncludePayloadCopies,
                CopyBytes(safePayload),
                Array.Empty(Of Byte)())

            entry = New ZipExtractedEntry(EvidenceHashing.DefaultPayloadLabelCore(), safePayload)

            digestSet = New HashDigestSet(
                physicalSha256:=physicalSha,
                logicalSha256:=logicalSha,
                fastPhysicalXxHash3:=fastPhysical,
                fastLogicalXxHash3:=fastLogical,
                hmacPhysicalSha256:=hmacPhysical,
                hmacLogicalSha256:=hmacLogical,
                hasPhysicalHash:=True,
                hasLogicalHash:=True)

            combinedNotes = AppendNoteIfAny(notes, secureNote)

            Return New HashEvidence(
                sourceType:=sourceType,
                label:=NormalizeLabel(label),
                detectedType:=If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown)),
                entry:=entry,
                compressedBytes:=persistedPayload,
                uncompressedBytes:=persistedPayload,
                entryCount:=1,
                totalUncompressedBytes:=safePayload.LongLength,
                digests:=digestSet,
                notes:=combinedNotes)
        End Function

        Friend Shared Function TryNormalizeEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry),
                ByRef normalizedEntries As List(Of NormalizedEntry),
                ByRef errorMessage As String
            ) As Boolean

            Dim seen As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
            Dim normalizedPath As String
            Dim payload As Byte()

            normalizedEntries = New List(Of NormalizedEntry)()
            errorMessage = String.Empty

            If entries Is Nothing Then
                errorMessage = "Entries sind null."
                Return False
            End If

            For Each entry In entries
                If entry Is Nothing Then
                    errorMessage = "Entry ist null."
                    Return False
                End If

                normalizedPath = Nothing
                If Not TryNormalizeEntryPath(entry.RelativePath, normalizedPath) Then
                    errorMessage = $"Ungültiger Entry-Pfad: '{entry.RelativePath}'."
                    Return False
                End If

                If Not seen.Add(normalizedPath) Then
                    errorMessage = $"Doppelter Entry-Pfad nach Normalisierung: '{normalizedPath}'."
                    Return False
                End If

                payload = If(entry.Content.IsDefaultOrEmpty, Array.Empty(Of Byte)(), entry.Content.ToArray())
                normalizedEntries.Add(New NormalizedEntry(normalizedPath, payload))
            Next

            normalizedEntries.Sort(Function(a, b) StringComparer.Ordinal.Compare(a.RelativePath, b.RelativePath))
            Return True
        End Function

        Friend Shared Function TryNormalizeEntryPath _
            (
                rawPath As String,
                ByRef normalizedPath As String
            ) As Boolean

            Dim isDirectory As Boolean = False
            Return ArchiveEntryPathPolicy.TryNormalizeRelativePath(
                rawPath,
                allowDirectoryMarker:=False,
                normalizedPath,
                isDirectory)
        End Function

        Friend Shared Function BuildLogicalManifestBytes _
            (
                entries As IReadOnlyList(Of NormalizedEntry)
            ) As Byte()

            Dim versionBytes As Byte()
            Dim pathBytes As Byte()
            Dim contentHash As Byte()

            Using ms As New IO.MemoryStream()
                Using writer As New IO.BinaryWriter(ms, Text.Encoding.UTF8, leaveOpen:=True)
                    versionBytes = Text.Encoding.UTF8.GetBytes(EvidenceHashing.LogicalManifestVersionCore())
                    writer.Write(versionBytes.Length)
                    writer.Write(versionBytes)
                    writer.Write(entries.Count)

                    For Each entry In entries
                        pathBytes = Text.Encoding.UTF8.GetBytes(entry.RelativePath)
                        contentHash = HashPrimitives.Current.Sha256.ComputeHash(entry.Content)
                        writer.Write(pathBytes.Length)
                        writer.Write(pathBytes)
                        writer.Write(CLng(entry.Content.LongLength))
                        writer.Write(contentHash.Length)
                        writer.Write(contentHash)
                    Next
                End Using

                Return ms.ToArray()
            End Using
        End Function

        Friend Shared Function ComputeSha256Hex _
            (
                payload As Byte()
            ) As String

            Dim data As Byte() = If(payload, Array.Empty(Of Byte)())
            Return HashPrimitives.Current.Sha256.ComputeHashHex(data)
        End Function

        Friend Shared Function ComputeFastHash _
            (
                payload As Byte(),
                options As HashOptions
            ) As String

            Dim data As Byte()

            If options Is Nothing OrElse Not options.IncludeFastHash Then Return String.Empty
            data = If(payload, Array.Empty(Of Byte)())

            Return HashPrimitives.Current.FastHash64.ComputeHashHex(data)
        End Function

        Friend Shared Function ComputeHmacSha256Hex _
            (
                key As Byte(),
                payload As Byte()
            ) As String

            Dim safeKey As Byte() = If(key, Array.Empty(Of Byte)())
            Dim data As Byte() = If(payload, Array.Empty(Of Byte)())

            Using hmac As New Security.Cryptography.HMACSHA256(safeKey)
                Return HashPrimitives.Current.HexCodec.EncodeLowerHex(hmac.ComputeHash(data))
            End Using
        End Function

        Friend Shared Function TryResolveHmacKey _
            (
                ByRef key As Byte(),
                ByRef note As String
            ) As Boolean

            Dim b64 As String

            key = Array.Empty(Of Byte)()
            note = String.Empty

            b64 = Environment.GetEnvironmentVariable(EvidenceHashing.HmacKeyEnvVarB64Core())
            If String.IsNullOrWhiteSpace(b64) Then
                note = $"Secure hashing requested but env var '{EvidenceHashing.HmacKeyEnvVarB64Core()}' is missing; HMAC digests omitted."
                Return False
            End If

            Try
                key = Convert.FromBase64String(b64.Trim())
                If key Is Nothing OrElse key.Length = 0 Then
                    key = Array.Empty(Of Byte)()
                    note = $"Secure hashing requested but env var '{EvidenceHashing.HmacKeyEnvVarB64Core()}' is empty; HMAC digests omitted."
                    Return False
                End If

                Return True
            Catch ex As Exception When _
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
                key = Array.Empty(Of Byte)()
                note = $"Secure hashing requested but env var '{EvidenceHashing.HmacKeyEnvVarB64Core()}' is invalid Base64; HMAC digests omitted."
                Return False
            End Try
        End Function

        Friend Shared Function AppendNoteIfAny _
            (
                baseNotes As String,
                toAppend As String
            ) As String

            Dim left As String = If(baseNotes, String.Empty).Trim()
            Dim right As String = If(toAppend, String.Empty).Trim()

            If right.Length = 0 Then Return left
            If left.Length = 0 Then Return right
            Return left & " " & right
        End Function

        Friend Shared Function NormalizeLabel _
            (
                label As String
            ) As String

            Dim normalized As String = If(label, String.Empty).Trim()
            If normalized.Length = 0 Then Return EvidenceHashing.DefaultPayloadLabelCore()
            Return normalized
        End Function

        Friend Shared Function CopyBytes _
            (
                data As Byte()
            ) As Byte()

            Dim copy As Byte()

            If data Is Nothing OrElse data.Length = 0 Then Return Array.Empty(Of Byte)()
            copy = New Byte(data.Length - 1) {}
            Buffer.BlockCopy(data, 0, copy, 0, data.Length)

            Return copy
        End Function

        ''' <summary>
        '''     Normalisierte Entry-Repräsentation für kanonische Manifestbildung.
        ''' </summary>
        ''' <remarks>
        '''     Relative Pfade und Inhalte werden nach Guard-Prüfung unveränderlich für deterministische Sortierung gehalten.
        ''' </remarks>
        Friend NotInheritable Class NormalizedEntry
            Friend ReadOnly Property RelativePath As String
            Friend ReadOnly Property Content As Byte()

            Friend Sub New(relativePath As String, content As Byte())
                Me.RelativePath = If(relativePath, String.Empty)
                Me.Content = If(content, Array.Empty(Of Byte)())
            End Sub
        End Class
    End Class
End Namespace
