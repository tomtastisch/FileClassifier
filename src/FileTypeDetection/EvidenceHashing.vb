' ============================================================================
' FILE: EvidenceHashing.vb
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
    '''     Öffentliche Fassade für deterministische Hash-Nachweise und RoundTrip-Konsistenzberichte.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Verantwortung: Die Klasse erzeugt reproduzierbare Digest-Evidence für Dateien, Rohbytes und Archiv-Entries.
    '''     </para>
    '''     <para>
    '''         Security/Compliance: Optionale HMAC-Digests verwenden den Schlüssel aus
    '''         <c>FILECLASSIFIER_HMAC_KEY_B64</c>; fehlt der Schlüssel, wird fail-closed ohne HMAC fortgeführt.
    '''     </para>
    '''     <para>
    '''         Nebenwirkungen: <c>VerifyRoundTrip</c> verwendet temporäre Dateisystempfade für die Materialisierung und
    '''         bereinigt diese anschließend best-effort.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class EvidenceHashing
        Private Const LogicalManifestVersion As String = "FTD-LOGICAL-HASH-V1"
        Private Const DefaultPayloadLabel As String = "payload.bin"
        Private Const HmacKeyEnvVarB64 As String = "FILECLASSIFIER_HMAC_KEY_B64"

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für eine Datei mit Standard-Hashoptionen.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit expliziten Hashoptionen.
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashFile _
            (
                path As String
            ) As HashEvidence
            Return HashFile(path, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für eine Datei.
        ''' </summary>
        ''' <remarks>
        '''     Archive werden über kanonisches Manifest gehasht; Nicht-Archive über direkte Payload-Digests.
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashFile _
            (
                path As String,
                options As HashOptions
            ) _
            As HashEvidence
            Dim detectorOptions As FileTypeProjectOptions = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions = ResolveHashOptions(detectorOptions, options)
            Dim fileBytes As Byte() = Array.Empty(Of Byte)()
            Dim readError As String = String.Empty
            Dim detectedType As FileType
            Dim entries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If String.IsNullOrWhiteSpace(path) OrElse Not IO.File.Exists(path) Then
                Return _
                    HashEvidence.CreateFailure(HashSourceType.FilePath, path, "Datei nicht gefunden.")
            End If

            If Not TryReadFileBounded(path, detectorOptions, fileBytes, readError) Then
                Return HashEvidence.CreateFailure(HashSourceType.FilePath, path, readError)
            End If

            detectedType = New FileTypeDetector().Detect(path)
            If ArchiveEntryCollector.TryCollectFromFile(path, detectorOptions, entries) Then
                Return BuildEvidenceFromEntries(
                    sourceType:=HashSourceType.FilePath,
                    label:=IO.Path.GetFileName(path),
                    detectedType:=detectedType,
                    compressedBytes:=fileBytes,
                    entries:=entries,
                    hashOptions:=normalizedOptions,
                    notes:="Archive content hashed via canonical manifest.")
            End If

            Return BuildEvidenceFromRawPayload(
                sourceType:=HashSourceType.FilePath,
                label:=IO.Path.GetFileName(path),
                detectedType:=detectedType,
                payload:=fileBytes,
                hashOptions:=normalizedOptions,
                notes:="Raw payload hashed directly.")
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für Rohbytes mit Standardlabel.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit Label und expliziten Hashoptionen.
        ''' </remarks>
        ''' <param name="data">Zu hashende Rohbytes.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashBytes _
            (
                data As Byte()
            ) As HashEvidence
            Return HashBytes(data, DefaultPayloadLabel, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für Rohbytes mit benutzerdefiniertem Label.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit expliziten Hashoptionen.
        ''' </remarks>
        ''' <param name="data">Zu hashende Rohbytes.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashBytes _
            (
                data As Byte(),
                label As String
            ) As HashEvidence
            Return HashBytes(data, label, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für Rohbytes.
        ''' </summary>
        ''' <remarks>
        '''     Die Eingabe wird gegen globale Größenlimits geprüft und anschließend als Archiv- oder Rohpayload verarbeitet.
        ''' </remarks>
        ''' <param name="data">Zu hashende Rohbytes.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashBytes _
            (
                data As Byte(),
                label As String,
                options As HashOptions
            ) _
            As HashEvidence
            Dim detectorOptions As FileTypeProjectOptions = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions = ResolveHashOptions(detectorOptions, options)
            Dim detectedType As FileType
            Dim entries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If data Is Nothing Then
                Return _
                    HashEvidence.CreateFailure(HashSourceType.RawBytes, label, "Payload ist null.")
            End If

            If CLng(data.Length) > detectorOptions.MaxBytes Then
                Return _
                    HashEvidence.CreateFailure(HashSourceType.RawBytes, label, "Payload größer als MaxBytes.")
            End If

            detectedType = New FileTypeDetector().Detect(data)
            If ArchiveEntryCollector.TryCollectFromBytes(data, detectorOptions, entries) Then
                Return BuildEvidenceFromEntries(
                    sourceType:=HashSourceType.RawBytes,
                    label:=NormalizeLabel(label),
                    detectedType:=detectedType,
                    compressedBytes:=data,
                    entries:=entries,
                    hashOptions:=normalizedOptions,
                    notes:="Archive bytes hashed via canonical manifest.")
            End If

            Return BuildEvidenceFromRawPayload(
                sourceType:=HashSourceType.RawBytes,
                label:=NormalizeLabel(label),
                detectedType:=detectedType,
                payload:=data,
                hashOptions:=normalizedOptions,
                notes:="Raw payload hashed directly.")
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis aus extrahierten Archiveinträgen mit Standardlabel.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit Label und expliziten Hashoptionen.
        ''' </remarks>
        ''' <param name="entries">Read-only Liste normalisierbarer Archiveinträge.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry)
            ) As HashEvidence
            Return HashEntries(entries, "archive-entries", options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis aus extrahierten Archiveinträgen mit benutzerdefiniertem Label.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit expliziten Hashoptionen.
        ''' </remarks>
        ''' <param name="entries">Read-only Liste normalisierbarer Archiveinträge.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry),
                label As String
            ) _
            As HashEvidence
            Return HashEntries(entries, label, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis aus extrahierten Archiveinträgen.
        ''' </summary>
        ''' <remarks>
        '''     Entry-Pfade und -Inhalte werden vor der Manifestbildung normalisiert, dedupliziert und deterministisch sortiert.
        ''' </remarks>
        ''' <param name="entries">Read-only Liste normalisierbarer Archiveinträge.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry),
                label As String,
                options As HashOptions
            ) As HashEvidence
            Dim projectOptions = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions = ResolveHashOptions(projectOptions, options)
            Return BuildEvidenceFromEntries(
                sourceType:=HashSourceType.ArchiveEntries,
                label:=NormalizeLabel(label),
                detectedType:=FileTypeRegistry.Resolve(FileKind.Zip),
                compressedBytes:=Array.Empty(Of Byte)(),
                entries:=entries,
                hashOptions:=normalizedOptions,
                notes:="Entries hashed via canonical manifest.")
        End Function

        ''' <summary>
        '''     Führt den deterministischen h1-h4-RoundTrip mit Standard-Hashoptionen aus.
        ''' </summary>
        ''' <remarks>
        '''     Delegiert auf die Überladung mit expliziten Hashoptionen.
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <returns>RoundTrip-Bericht mit Konsistenzkennzahlen und Notes.</returns>
        Public Shared Function VerifyRoundTrip _
            (
                path As String
            ) As HashRoundTripReport
            Return VerifyRoundTrip(path, options:=Nothing)
        End Function

        ''' <summary>
        '''     Führt den deterministischen h1-h4-RoundTrip aus und bewertet logische sowie physische Konsistenz.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:
        '''         1) h1: Hash des Eingabeobjekts,
        '''         2) h2: Hash der kanonischen Archivsicht bzw. der Originalbytes,
        '''         3) h3: Hash der logisch kanonisierten Bytes,
        '''         4) h4: Hash nach Materialisierung der kanonischen Bytes.
        '''     </para>
        '''     <para>
        '''         Fehler werden fail-closed als Bericht mit Fehler-Evidence zurückgegeben.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>RoundTrip-Bericht mit Konsistenzkennzahlen und Notes.</returns>
        Public Shared Function VerifyRoundTrip _
            (
                path As String,
                options As HashOptions
            ) _
            As HashRoundTripReport
            Dim detectorOptions As FileTypeProjectOptions = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions = ResolveHashOptions(detectorOptions, options)
            Dim failed As HashEvidence
            Dim h1 As HashEvidence
            Dim originalBytes As Byte() = Array.Empty(Of Byte)()
            Dim readError As String = String.Empty
            Dim archiveEntries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            Dim isArchiveInput As Boolean
            Dim h2 As HashEvidence
            Dim canonicalBytes As Byte()
            Dim normalizedEntries As List(Of NormalizedEntry)
            Dim normalizeError As String
            Dim h3 As HashEvidence
            Dim h4 As HashEvidence = HashEvidence.CreateFailure(
                    HashSourceType.MaterializedFile,
                    "roundtrip-h4-file",
                    "Materialization failed."
                )
            Dim roundTripTempRoot As String = IO.Path.Combine(
                    IO.Path.GetTempPath(),
                    "ftd-roundtrip-" &
                    Guid.NewGuid().ToString("N", Globalization.CultureInfo.InvariantCulture)
                )
            Dim targetFile As String
            Dim notes As String

            If String.IsNullOrWhiteSpace(path) OrElse Not IO.File.Exists(path) Then
                failed = HashEvidence.CreateFailure(HashSourceType.FilePath, path, "Datei nicht gefunden.")
                Return _
                    New HashRoundTripReport(path, isArchiveInput:=False, h1:=failed, h2:=failed, h3:=failed, h4:=failed,
                                            notes:="Input file missing.")
            End If

            h1 = HashFile(path, normalizedOptions)
            If Not h1.Digests.HasLogicalHash Then
                failed = HashEvidence.CreateFailure(HashSourceType.Unknown, path, "h1 konnte nicht berechnet werden.")
                Return _
                    New HashRoundTripReport(path, isArchiveInput:=False, h1:=h1, h2:=failed, h3:=failed, h4:=failed,
                                            notes:="h1 missing logical digest.")
            End If

            If Not TryReadFileBounded(path, detectorOptions, originalBytes, readError) Then
                failed = HashEvidence.CreateFailure(HashSourceType.Unknown, path, readError)
                Return _
                    New HashRoundTripReport(path, isArchiveInput:=False, h1:=h1, h2:=failed, h3:=failed, h4:=failed,
                                            notes:=readError)
            End If

            isArchiveInput = ArchiveEntryCollector.TryCollectFromFile(path, detectorOptions, archiveEntries)

            If isArchiveInput Then
                h2 = HashEntries(archiveEntries, "roundtrip-h2-entries", normalizedOptions)
                normalizedEntries = Nothing
                normalizeError = String.Empty
                If TryNormalizeEntries(archiveEntries, normalizedEntries, normalizeError) Then
                    canonicalBytes = BuildLogicalManifestBytes(normalizedEntries)
                Else
                    canonicalBytes = Array.Empty(Of Byte)()
                End If
            Else
                h2 = HashBytes(originalBytes, "roundtrip-h2-bytes", normalizedOptions)
                canonicalBytes = CopyBytes(originalBytes)
            End If

            h3 = BuildEvidenceFromRawPayload(
                sourceType:=HashSourceType.RawBytes,
                label:="roundtrip-h3-logical-bytes",
                detectedType:=FileTypeRegistry.Resolve(FileKind.Unknown),
                payload:=canonicalBytes,
                hashOptions:=normalizedOptions,
                notes:="Canonical logical bytes hashed directly.")

            Try
                IO.Directory.CreateDirectory(roundTripTempRoot)
                targetFile = IO.Path.Combine(
                        roundTripTempRoot,
                        NormalizeLabel(normalizedOptions.MaterializedFileName)
                    )

                If FileMaterializer.Persist(canonicalBytes, targetFile, overwrite:=False, secureExtract:=False) Then
                    h4 = HashFile(targetFile, normalizedOptions)
                End If

            Finally
                Try
                    If IO.Directory.Exists(roundTripTempRoot) Then
                        IO.Directory.Delete(roundTripTempRoot, recursive:=True)
                    End If

                Catch ex As Exception When _
                    TypeOf ex Is UnauthorizedAccessException OrElse
                    TypeOf ex Is System.Security.SecurityException OrElse
                    TypeOf ex Is IO.IOException OrElse
                    TypeOf ex Is IO.PathTooLongException OrElse
                    TypeOf ex Is NotSupportedException OrElse
                    TypeOf ex Is ArgumentException
                    LogGuard.Debug(detectorOptions.Logger, $"[HashRoundTrip] Cleanup-Fehler: {ex.Message}")
                End Try
            End Try

            notes = If(
                    isArchiveInput,
                    "Archive roundtrip (h1-h4) executed.",
                    "Raw file roundtrip (h1-h4) executed."
                )

            Return New HashRoundTripReport(path, isArchiveInput, h1, h2, h3, h4, notes)
        End Function

        Private Shared Function BuildEvidenceFromEntries(
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

            persistedCompressed =
                    If(hashOptions.IncludePayloadCopies, CopyBytes(compressedBytes), Array.Empty(Of Byte)())
            persistedLogical =
                    If(hashOptions.IncludePayloadCopies, CopyBytes(logicalBytes), Array.Empty(Of Byte)())

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

        Private Shared Function BuildEvidenceFromRawPayload(
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

            persistedPayload = If(hashOptions.IncludePayloadCopies, CopyBytes(safePayload), Array.Empty(Of Byte)())
            entry = New ZipExtractedEntry(DefaultPayloadLabel, safePayload)

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

        Private Shared Function TryNormalizeEntries(
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

        Private Shared Function TryNormalizeEntryPath(rawPath As String, ByRef normalizedPath As String) As Boolean
            Dim isDirectory = False
            Return _
                ArchiveEntryPathPolicy.TryNormalizeRelativePath(rawPath, allowDirectoryMarker:=False, normalizedPath,
                                                                isDirectory)
        End Function

        Private Shared Function BuildLogicalManifestBytes(entries As IReadOnlyList(Of NormalizedEntry)) As Byte()
            Dim versionBytes As Byte()
            Dim pathBytes As Byte()
            Dim contentHash As Byte()

            Using ms As New IO.MemoryStream()
                Using writer As New IO.BinaryWriter(ms, Text.Encoding.UTF8, leaveOpen:=True)
                    versionBytes = Text.Encoding.UTF8.GetBytes(LogicalManifestVersion)
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

        Private Shared Function ComputeSha256Hex(payload As Byte()) As String
            Dim data = If(payload, Array.Empty(Of Byte)())
            Return HashPrimitives.Current.Sha256.ComputeHashHex(data)
        End Function

        Private Shared Function TryResolveHmacKey(ByRef key As Byte(), ByRef note As String) As Boolean
            Dim b64 As String

            key = Array.Empty(Of Byte)()
            note = String.Empty

            b64 = Environment.GetEnvironmentVariable(HmacKeyEnvVarB64)
            If String.IsNullOrWhiteSpace(b64) Then
                note = $"Secure hashing requested but env var '{HmacKeyEnvVarB64}' is missing; HMAC digests omitted."
                Return False
            End If

            Try
                key = Convert.FromBase64String(b64.Trim())
                If key Is Nothing OrElse key.Length = 0 Then
                    key = Array.Empty(Of Byte)()
                    note = $"Secure hashing requested but env var '{HmacKeyEnvVarB64}' is empty; HMAC digests omitted."
                    Return False
                End If
                Return True
            Catch ex As Exception When _
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
                key = Array.Empty(Of Byte)()
                note = $"Secure hashing requested but env var '{HmacKeyEnvVarB64}' is invalid Base64; HMAC digests omitted."
                Return False
            End Try
        End Function

        Private Shared Function ComputeHmacSha256Hex(key As Byte(), payload As Byte()) As String
            Dim safeKey = If(key, Array.Empty(Of Byte)())
            Dim data = If(payload, Array.Empty(Of Byte)())
            Using hmac As New Security.Cryptography.HMACSHA256(safeKey)
                Return HashPrimitives.Current.HexCodec.EncodeLowerHex(hmac.ComputeHash(data))
            End Using
        End Function

        Private Shared Function ComputeFastHash(payload As Byte(), options As HashOptions) As String
            Dim data As Byte()

            If options Is Nothing OrElse Not options.IncludeFastHash Then Return String.Empty
            data = If(payload, Array.Empty(Of Byte)())
            Return HashPrimitives.Current.FastHash64.ComputeHashHex(data)
        End Function

        Private Shared Function AppendNoteIfAny(baseNotes As String, toAppend As String) As String
            Dim left = If(baseNotes, String.Empty).Trim()
            Dim right = If(toAppend, String.Empty).Trim()
            If right.Length = 0 Then Return left
            If left.Length = 0 Then Return right
            Return left & " " & right
        End Function

        Private Shared Function NormalizeLabel(label As String) As String
            Dim normalized = If(label, String.Empty).Trim()
            If normalized.Length = 0 Then Return DefaultPayloadLabel
            Return normalized
        End Function

        Private Shared Function CopyBytes(data As Byte()) As Byte()
            Dim copy As Byte()

            If data Is Nothing OrElse data.Length = 0 Then Return Array.Empty(Of Byte)()
            copy = New Byte(data.Length - 1) {}
            Buffer.BlockCopy(data, 0, copy, 0, data.Length)
            Return copy
        End Function

        Private Shared Function ResolveHashOptions(
                                                   projectOptions As FileTypeProjectOptions,
                                                   options As HashOptions
                                               ) As HashOptions

            If options IsNot Nothing Then Return HashOptions.Normalize(options)
            If projectOptions IsNot Nothing Then _
                Return HashOptions.Normalize(projectOptions.DeterministicHash)
            Return HashOptions.Normalize(Nothing)
        End Function

        Private Shared Function TryReadFileBounded(path As String, detectorOptions As FileTypeProjectOptions,
                                                   ByRef bytes As Byte(), ByRef errorMessage As String) As Boolean
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

                Using _
                    fs As _
                        New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, IO.FileOptions.SequentialScan)
                    Using ms As New IO.MemoryStream(CInt(Math.Min(Math.Max(fi.Length, 0), Integer.MaxValue)))
                        StreamBounds.CopyBounded(fs, ms, detectorOptions.MaxBytes)
                        bytes = ms.ToArray()
                    End Using
                End Using
                Return True
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IO.IOException OrElse
                TypeOf ex Is IO.InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return SetReadFileError(ex, errorMessage)
            End Try
        End Function

        Private Shared Function SetReadFileError(ex As Exception, ByRef errorMessage As String) As Boolean
            errorMessage = $"Datei konnte nicht gelesen werden: {ex.Message}"
            Return False
        End Function

        ''' <summary>
        '''     Interne Hilfsklasse <c>NormalizedEntry</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
        ''' </summary>
        Private NotInheritable Class NormalizedEntry
            Friend ReadOnly Property RelativePath As String
            Friend ReadOnly Property Content As Byte()

            Friend Sub New(relativePath As String, content As Byte())
                Me.RelativePath = If(relativePath, String.Empty)
                Me.Content = If(content, Array.Empty(Of Byte)())
            End Sub
        End Class
    End Class
End Namespace
