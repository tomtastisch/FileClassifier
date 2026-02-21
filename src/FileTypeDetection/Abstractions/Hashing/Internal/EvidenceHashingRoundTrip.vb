' ============================================================================
' FILE: EvidenceHashingRoundTrip.vb
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
    '''     Interne RoundTrip-Pipeline für den deterministischen h1-h4-Hashbericht.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Der Service erstellt temporäre Ziele für Materialisierung, nutzt die öffentliche Fassade für h1/h2/h4,
    '''         berechnet h3 über kanonische Logical-Bytes und bereinigt temporäre Verzeichnisse best-effort.
    '''     </para>
    '''     <para>
    '''         Catch-Filter und Fehltexte bleiben unverändert fail-closed.
    '''     </para>
    ''' </remarks>
    Friend NotInheritable Class EvidenceHashingRoundTrip
        Private Sub New()
        End Sub

        Friend Shared Function VerifyRoundTrip _
            (
                path As String,
                options As HashOptions
            ) As HashRoundTripReport

            Dim detectorOptions As FileTypeProjectOptions = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions = EvidenceHashing.ResolveHashOptionsCore(detectorOptions, options)
            Dim failed As HashEvidence
            Dim h1 As HashEvidence
            Dim originalBytes As Byte() = Array.Empty(Of Byte)()
            Dim readError As String = String.Empty
            Dim archiveEntries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            Dim isArchiveInput As Boolean
            Dim h2 As HashEvidence
            Dim canonicalBytes As Byte()
            Dim normalizedEntries As List(Of EvidenceHashingCore.NormalizedEntry)
            Dim normalizeError As String
            Dim h3 As HashEvidence
            Dim h4 As HashEvidence = HashEvidence.CreateFailure(
                HashSourceType.MaterializedFile,
                "roundtrip-h4-file",
                "Materialization failed.")

            Dim roundTripTempRoot As String = IO.Path.Combine(
                IO.Path.GetTempPath(),
                "ftd-roundtrip-" & Guid.NewGuid().ToString("N", Globalization.CultureInfo.InvariantCulture))

            Dim targetFile As String
            Dim notes As String

            If String.IsNullOrWhiteSpace(path) OrElse Not IO.File.Exists(path) Then
                failed = HashEvidence.CreateFailure(HashSourceType.FilePath, path, "Datei nicht gefunden.")
                Return New HashRoundTripReport(
                    path,
                    isArchiveInput:=False,
                    notes:="Input file missing.",
                    failed,
                    failed,
                    failed,
                    failed)
            End If

            h1 = EvidenceHashing.HashFile(path, normalizedOptions)
            If Not h1.Digests.HasLogicalHash Then
                failed = HashEvidence.CreateFailure(HashSourceType.Unknown, path, "h1 konnte nicht berechnet werden.")
                Return New HashRoundTripReport(
                    path,
                    isArchiveInput:=False,
                    notes:="h1 missing logical digest.",
                    h1,
                    failed,
                    failed,
                    failed)
            End If

            If Not EvidenceHashingIo.TryReadFileBounded(path, detectorOptions, originalBytes, readError) Then
                failed = HashEvidence.CreateFailure(HashSourceType.Unknown, path, readError)
                Return New HashRoundTripReport(
                    path,
                    isArchiveInput:=False,
                    notes:=readError,
                    h1,
                    failed,
                    failed,
                    failed)
            End If

            isArchiveInput = ArchiveEntryCollector.TryCollectFromFile(path, detectorOptions, archiveEntries)

            If isArchiveInput Then
                h2 = EvidenceHashing.HashEntries(archiveEntries, "roundtrip-h2-entries", normalizedOptions)
                normalizedEntries = Nothing
                normalizeError = String.Empty
                If EvidenceHashingCore.TryNormalizeEntries(archiveEntries, normalizedEntries, normalizeError) Then
                    canonicalBytes = EvidenceHashingCore.BuildLogicalManifestBytes(normalizedEntries)
                Else
                    canonicalBytes = Array.Empty(Of Byte)()
                End If
            Else
                h2 = EvidenceHashing.HashBytes(originalBytes, "roundtrip-h2-bytes", normalizedOptions)
                canonicalBytes = EvidenceHashingCore.CopyBytes(originalBytes)
            End If

            h3 = EvidenceHashingCore.BuildEvidenceFromRawPayload(
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
                    EvidenceHashingCore.NormalizeLabel(normalizedOptions.MaterializedFileName))

                If FileMaterializer.Persist(canonicalBytes, targetFile, overwrite:=False, secureExtract:=False) Then
                    h4 = EvidenceHashing.HashFile(targetFile, normalizedOptions)
                End If
            Finally
                Try
                    If IO.Directory.Exists(roundTripTempRoot) Then
                        IO.Directory.Delete(roundTripTempRoot, recursive:=True)
                    End If
                Catch ex As Exception When _
                    TypeOf ex Is UnauthorizedAccessException OrElse
                    TypeOf ex Is Security.SecurityException OrElse
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
                "Raw file roundtrip (h1-h4) executed.")

            Return New HashRoundTripReport(
                path,
                isArchiveInput,
                notes,
                h1,
                h2,
                h3,
                h4)
        End Function
    End Class
End Namespace
