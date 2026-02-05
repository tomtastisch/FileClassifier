Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.IO.Hashing
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text

Namespace FileTypeDetection

    ''' <summary>
    ''' Oeffentliche Fassade fuer deterministische Hash- und RoundTrip-Nachweise.
    ''' </summary>
    Public NotInheritable Class DeterministicHashing
        Private Const LogicalManifestVersion As String = "FTD-LOGICAL-HASH-V1"
        Private Const DefaultPayloadLabel As String = "payload.bin"

        Private Sub New()
        End Sub

        Public Shared Function HashFile(path As String) As DeterministicHashEvidence
            Return HashFile(path, New DeterministicHashOptions())
        End Function

        Public Shared Function HashFile(path As String, options As DeterministicHashOptions) As DeterministicHashEvidence
            Dim normalizedOptions = DeterministicHashOptions.Normalize(options)
            Dim detectorOptions = FileTypeOptions.GetSnapshot()

            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                Return DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.FilePath, path, "Datei nicht gefunden.")
            End If

            Dim fileBytes As Byte() = Array.Empty(Of Byte)()
            Dim readError As String = String.Empty
            If Not TryReadFileBounded(path, detectorOptions, fileBytes, readError) Then
                Return DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.FilePath, path, readError)
            End If

            Dim detectedType = New FileTypeDetector().Detect(path)
            Dim entries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If ArchiveEntryCollector.TryCollectFromFile(path, detectorOptions, entries) Then
                Return BuildEvidenceFromEntries(
                    sourceType:=DeterministicHashSourceType.FilePath,
                    label:=Global.System.IO.Path.GetFileName(path),
                    detectedType:=detectedType,
                    compressedBytes:=fileBytes,
                    entries:=entries,
                    hashOptions:=normalizedOptions,
                    notes:="Archive content hashed via canonical manifest.")
            End If

            Return BuildEvidenceFromRawPayload(
                sourceType:=DeterministicHashSourceType.FilePath,
                label:=Global.System.IO.Path.GetFileName(path),
                detectedType:=detectedType,
                payload:=fileBytes,
                hashOptions:=normalizedOptions,
                notes:="Raw payload hashed directly.")
        End Function

        Public Shared Function HashBytes(data As Byte()) As DeterministicHashEvidence
            Return HashBytes(data, DefaultPayloadLabel, New DeterministicHashOptions())
        End Function

        Public Shared Function HashBytes(data As Byte(), label As String) As DeterministicHashEvidence
            Return HashBytes(data, label, New DeterministicHashOptions())
        End Function

        Public Shared Function HashBytes(data As Byte(), label As String, options As DeterministicHashOptions) As DeterministicHashEvidence
            Dim normalizedOptions = DeterministicHashOptions.Normalize(options)
            Dim detectorOptions = FileTypeOptions.GetSnapshot()

            If data Is Nothing Then
                Return DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.RawBytes, label, "Payload ist null.")
            End If

            If CLng(data.Length) > detectorOptions.MaxBytes Then
                Return DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.RawBytes, label, "Payload groesser als MaxBytes.")
            End If

            Dim detectedType = New FileTypeDetector().Detect(data)
            Dim entries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If ArchiveEntryCollector.TryCollectFromBytes(data, detectorOptions, entries) Then
                Return BuildEvidenceFromEntries(
                    sourceType:=DeterministicHashSourceType.RawBytes,
                    label:=NormalizeLabel(label),
                    detectedType:=detectedType,
                    compressedBytes:=data,
                    entries:=entries,
                    hashOptions:=normalizedOptions,
                    notes:="Archive bytes hashed via canonical manifest.")
            End If

            Return BuildEvidenceFromRawPayload(
                sourceType:=DeterministicHashSourceType.RawBytes,
                label:=NormalizeLabel(label),
                detectedType:=detectedType,
                payload:=data,
                hashOptions:=normalizedOptions,
                notes:="Raw payload hashed directly.")
        End Function

        Public Shared Function HashEntries(entries As IReadOnlyList(Of ZipExtractedEntry)) As DeterministicHashEvidence
            Return HashEntries(entries, "archive-entries", New DeterministicHashOptions())
        End Function

        Public Shared Function HashEntries(entries As IReadOnlyList(Of ZipExtractedEntry), label As String) As DeterministicHashEvidence
            Return HashEntries(entries, label, New DeterministicHashOptions())
        End Function

        Public Shared Function HashEntries(entries As IReadOnlyList(Of ZipExtractedEntry), label As String, options As DeterministicHashOptions) As DeterministicHashEvidence
            Dim normalizedOptions = DeterministicHashOptions.Normalize(options)
            Return BuildEvidenceFromEntries(
                sourceType:=DeterministicHashSourceType.ArchiveEntries,
                label:=NormalizeLabel(label),
                detectedType:=FileTypeRegistry.Resolve(FileKind.Zip),
                compressedBytes:=Array.Empty(Of Byte)(),
                entries:=entries,
                hashOptions:=normalizedOptions,
                notes:="Entries hashed via canonical manifest.")
        End Function

        Public Shared Function VerifyRoundTrip(path As String) As DeterministicHashRoundTripReport
            Return VerifyRoundTrip(path, New DeterministicHashOptions())
        End Function

        Public Shared Function VerifyRoundTrip(path As String, options As DeterministicHashOptions) As DeterministicHashRoundTripReport
            Dim normalizedOptions = DeterministicHashOptions.Normalize(options)
            Dim detectorOptions = FileTypeOptions.GetSnapshot()

            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                Dim failed = DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.FilePath, path, "Datei nicht gefunden.")
                Return New DeterministicHashRoundTripReport(path, isArchiveInput:=False, h1:=failed, h2:=failed, h3:=failed, h4:=failed, notes:="Input file missing.")
            End If

            Dim h1 = HashFile(path, normalizedOptions)
            If Not h1.Digests.HasLogicalHash Then
                Dim failed = DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.Unknown, path, "h1 konnte nicht berechnet werden.")
                Return New DeterministicHashRoundTripReport(path, isArchiveInput:=False, h1:=h1, h2:=failed, h3:=failed, h4:=failed, notes:="h1 missing logical digest.")
            End If

            Dim originalBytes As Byte() = Array.Empty(Of Byte)()
            Dim readError As String = String.Empty
            If Not TryReadFileBounded(path, detectorOptions, originalBytes, readError) Then
                Dim failed = DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.Unknown, path, readError)
                Return New DeterministicHashRoundTripReport(path, isArchiveInput:=False, h1:=h1, h2:=failed, h3:=failed, h4:=failed, notes:=readError)
            End If

            Dim archiveEntries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            Dim isArchiveInput = ArchiveEntryCollector.TryCollectFromFile(path, detectorOptions, archiveEntries)

            Dim h2 As DeterministicHashEvidence
            Dim canonicalBytes As Byte()

            If isArchiveInput Then
                h2 = HashEntries(archiveEntries, "roundtrip-h2-entries", normalizedOptions)
                Dim normalizedEntries As List(Of NormalizedEntry) = Nothing
                Dim normalizeError As String = String.Empty
                If TryNormalizeEntries(archiveEntries, normalizedEntries, normalizeError) Then
                    canonicalBytes = BuildLogicalManifestBytes(normalizedEntries)
                Else
                    canonicalBytes = Array.Empty(Of Byte)()
                End If
            Else
                h2 = HashBytes(originalBytes, "roundtrip-h2-bytes", normalizedOptions)
                canonicalBytes = CopyBytes(originalBytes)
            End If

            Dim h3 = BuildEvidenceFromRawPayload(
                sourceType:=DeterministicHashSourceType.RawBytes,
                label:="roundtrip-h3-logical-bytes",
                detectedType:=FileTypeRegistry.Resolve(FileKind.Unknown),
                payload:=canonicalBytes,
                hashOptions:=normalizedOptions,
                notes:="Canonical logical bytes hashed directly.")

            Dim h4 As DeterministicHashEvidence = DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.MaterializedFile, "roundtrip-h4-file", "Materialization failed.")
            Dim roundTripTempRoot = Global.System.IO.Path.Combine(Global.System.IO.Path.GetTempPath(), "ftd-roundtrip-" & Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
            Try
                Directory.CreateDirectory(roundTripTempRoot)
                Dim targetFile = Global.System.IO.Path.Combine(roundTripTempRoot, NormalizeLabel(normalizedOptions.MaterializedFileName))
                If FileMaterializer.Persist(canonicalBytes, targetFile, overwrite:=False, secureExtract:=False) Then
                    h4 = HashFile(targetFile, normalizedOptions)
                End If
            Finally
                Try
                    If Directory.Exists(roundTripTempRoot) Then
                        Directory.Delete(roundTripTempRoot, recursive:=True)
                    End If
                Catch
                End Try
            End Try

            Dim notes = If(isArchiveInput, "Archive roundtrip (h1-h4) executed.", "Raw file roundtrip (h1-h4) executed.")
            Return New DeterministicHashRoundTripReport(path, isArchiveInput, h1, h2, h3, h4, notes)
        End Function

        Private Shared Function BuildEvidenceFromEntries(
            sourceType As DeterministicHashSourceType,
            label As String,
            detectedType As FileType,
            compressedBytes As Byte(),
            entries As IReadOnlyList(Of ZipExtractedEntry),
            hashOptions As DeterministicHashOptions,
            notes As String
        ) As DeterministicHashEvidence
            Dim normalizedEntries As List(Of NormalizedEntry) = Nothing
            Dim normalizeError As String = String.Empty
            If Not TryNormalizeEntries(entries, normalizedEntries, normalizeError) Then
                Return DeterministicHashEvidence.CreateFailure(sourceType, label, normalizeError)
            End If

            Dim logicalBytes = BuildLogicalManifestBytes(normalizedEntries)
            Dim logicalSha = ComputeSha256Hex(logicalBytes)
            Dim fastLogical = ComputeFastHash(logicalBytes, hashOptions)
            Dim physicalSha = String.Empty
            Dim fastPhysical = String.Empty
            Dim hasPhysical = False

            If compressedBytes IsNot Nothing AndAlso compressedBytes.Length > 0 Then
                physicalSha = ComputeSha256Hex(compressedBytes)
                fastPhysical = ComputeFastHash(compressedBytes, hashOptions)
                hasPhysical = True
            End If

            Dim firstEntry As ZipExtractedEntry = Nothing
            If normalizedEntries.Count > 0 Then
                firstEntry = New ZipExtractedEntry(normalizedEntries(0).RelativePath, normalizedEntries(0).Content)
            End If

            Dim digestSet = New DeterministicHashDigestSet(
                physicalSha256:=physicalSha,
                logicalSha256:=logicalSha,
                fastPhysicalXxHash3:=fastPhysical,
                fastLogicalXxHash3:=fastLogical,
                hasPhysicalHash:=hasPhysical,
                hasLogicalHash:=True)

            Dim totalBytes As Long = 0
            For Each entry In normalizedEntries
                totalBytes += entry.Content.LongLength
            Next

            Dim persistedCompressed = If(hashOptions.IncludePayloadCopies, CopyBytes(compressedBytes), Array.Empty(Of Byte)())
            Dim persistedLogical = If(hashOptions.IncludePayloadCopies, CopyBytes(logicalBytes), Array.Empty(Of Byte)())

            Return New DeterministicHashEvidence(
                sourceType:=sourceType,
                label:=NormalizeLabel(label),
                detectedType:=If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown)),
                entry:=firstEntry,
                compressedBytes:=persistedCompressed,
                uncompressedBytes:=persistedLogical,
                entryCount:=normalizedEntries.Count,
                totalUncompressedBytes:=totalBytes,
                digests:=digestSet,
                notes:=notes)
        End Function

        Private Shared Function BuildEvidenceFromRawPayload(
            sourceType As DeterministicHashSourceType,
            label As String,
            detectedType As FileType,
            payload As Byte(),
            hashOptions As DeterministicHashOptions,
            notes As String
        ) As DeterministicHashEvidence
            Dim safePayload = If(payload, Array.Empty(Of Byte)())
            Dim physicalSha = ComputeSha256Hex(safePayload)
            Dim logicalSha = physicalSha
            Dim fastPhysical = ComputeFastHash(safePayload, hashOptions)
            Dim fastLogical = fastPhysical
            Dim persistedPayload = If(hashOptions.IncludePayloadCopies, CopyBytes(safePayload), Array.Empty(Of Byte)())
            Dim entry = New ZipExtractedEntry(DefaultPayloadLabel, safePayload)

            Dim digestSet = New DeterministicHashDigestSet(
                physicalSha256:=physicalSha,
                logicalSha256:=logicalSha,
                fastPhysicalXxHash3:=fastPhysical,
                fastLogicalXxHash3:=fastLogical,
                hasPhysicalHash:=True,
                hasLogicalHash:=True)

            Return New DeterministicHashEvidence(
                sourceType:=sourceType,
                label:=NormalizeLabel(label),
                detectedType:=If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown)),
                entry:=entry,
                compressedBytes:=persistedPayload,
                uncompressedBytes:=persistedPayload,
                entryCount:=1,
                totalUncompressedBytes:=safePayload.LongLength,
                digests:=digestSet,
                notes:=notes)
        End Function

        Private Shared Function TryNormalizeEntries(
            entries As IReadOnlyList(Of ZipExtractedEntry),
            ByRef normalizedEntries As List(Of NormalizedEntry),
            ByRef errorMessage As String
        ) As Boolean
            normalizedEntries = New List(Of NormalizedEntry)()
            errorMessage = String.Empty

            If entries Is Nothing Then
                errorMessage = "Entries sind null."
                Return False
            End If

            Dim seen As New HashSet(Of String)(StringComparer.Ordinal)
            For Each entry In entries
                If entry Is Nothing Then
                    errorMessage = "Entry ist null."
                    Return False
                End If

                Dim normalizedPath As String = Nothing
                If Not TryNormalizeEntryPath(entry.RelativePath, normalizedPath) Then
                    errorMessage = $"Ungueltiger Entry-Pfad: '{entry.RelativePath}'."
                    Return False
                End If

                If Not seen.Add(normalizedPath) Then
                    errorMessage = $"Doppelter Entry-Pfad nach Normalisierung: '{normalizedPath}'."
                    Return False
                End If

                Dim payload = If(entry.Content.IsDefaultOrEmpty, Array.Empty(Of Byte)(), entry.Content.ToArray())
                normalizedEntries.Add(New NormalizedEntry(normalizedPath, payload))
            Next

            normalizedEntries.Sort(Function(a, b) StringComparer.Ordinal.Compare(a.RelativePath, b.RelativePath))
            Return True
        End Function

        Private Shared Function TryNormalizeEntryPath(rawPath As String, ByRef normalizedPath As String) As Boolean
            Dim isDirectory As Boolean = False
            Return ArchiveEntryPathPolicy.TryNormalizeRelativePath(rawPath, allowDirectoryMarker:=False, normalizedPath, isDirectory)
        End Function

        Private Shared Function BuildLogicalManifestBytes(entries As IReadOnlyList(Of NormalizedEntry)) As Byte()
            Using ms As New MemoryStream()
                Using writer As New BinaryWriter(ms, Encoding.UTF8, leaveOpen:=True)
                    Dim versionBytes = Encoding.UTF8.GetBytes(LogicalManifestVersion)
                    writer.Write(versionBytes.Length)
                    writer.Write(versionBytes)
                    writer.Write(entries.Count)

                    For Each entry In entries
                        Dim pathBytes = Encoding.UTF8.GetBytes(entry.RelativePath)
                        Dim contentHash = SHA256.HashData(entry.Content)
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
            Return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()
        End Function

        Private Shared Function ComputeFastHash(payload As Byte(), options As DeterministicHashOptions) As String
            If options Is Nothing OrElse Not options.IncludeFastHash Then Return String.Empty
            Dim data = If(payload, Array.Empty(Of Byte)())
            Dim value = XxHash3.HashToUInt64(data)
            Return value.ToString("x16", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function NormalizeLabel(label As String) As String
            Dim normalized = If(label, String.Empty).Trim()
            If normalized.Length = 0 Then Return DefaultPayloadLabel
            Return normalized
        End Function

        Private Shared Function CopyBytes(data As Byte()) As Byte()
            If data Is Nothing OrElse data.Length = 0 Then Return Array.Empty(Of Byte)()
            Dim copy(data.Length - 1) As Byte
            Buffer.BlockCopy(data, 0, copy, 0, data.Length)
            Return copy
        End Function

        Private Shared Function TryReadFileBounded(path As String, detectorOptions As FileTypeDetectorOptions, ByRef bytes As Byte(), ByRef errorMessage As String) As Boolean
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
                Dim fi As New FileInfo(path)
                If Not fi.Exists Then
                    errorMessage = "Datei existiert nicht."
                    Return False
                End If

                If fi.Length > detectorOptions.MaxBytes Then
                    errorMessage = "Datei groesser als MaxBytes."
                    Return False
                End If

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Using ms As New MemoryStream(CInt(Math.Min(Math.Max(fi.Length, 0), Integer.MaxValue)))
                        StreamBounds.CopyBounded(fs, ms, detectorOptions.MaxBytes)
                        bytes = ms.ToArray()
                    End Using
                End Using
                Return True
            Catch ex As Exception
                errorMessage = $"Datei konnte nicht gelesen werden: {ex.Message}"
                Return False
            End Try
        End Function

        Private Structure NormalizedEntry
            Friend ReadOnly RelativePath As String
            Friend ReadOnly Content As Byte()

            Friend Sub New(relativePath As String, content As Byte())
                Me.RelativePath = If(relativePath, String.Empty)
                Me.Content = If(content, Array.Empty(Of Byte)())
            End Sub
        End Structure
    End Class

End Namespace
