Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace FileTypeDetection

    ''' <summary>
    ''' Oeffentliche API zur inhaltsbasierten Dateityp-Erkennung.
    '''
    ''' <remarks>
    ''' Sicherheits- und Architekturprinzipien:
    ''' - fail-closed: jeder Fehlerpfad liefert FileKind.Unknown.
    ''' - SSOT: Signatur- und Typdaten liegen zentral in FileTypeRegistry.
    ''' - Dateiendung ist nur Metadatum; die Entscheidung basiert auf Content.
    ''' - ZIP-Dateien laufen immer durch ZIP-Gate und optionales OOXML-Refinement.
    ''' </remarks>
    ''' </summary>
    Public NotInheritable Class FileTypeDetector
        Private Const ReasonUnknown As String = "Unknown"
        Private Const ReasonFileNotFound As String = "FileNotFound"
        Private Const ReasonInvalidLength As String = "InvalidLength"
        Private Const ReasonFileTooLarge As String = "FileTooLarge"
        Private Const ReasonException As String = "Exception"
        Private Const ReasonExtensionMismatch As String = "ExtensionMismatch"
        Private Const ReasonHeaderUnknown As String = "HeaderUnknown"
        Private Const ReasonHeaderMatch As String = "HeaderMatch"
        Private Const ReasonArchiveGateFailed As String = "ArchiveGateFailed"
        Private Const ReasonArchiveStructuredRefined As String = "ArchiveStructuredRefined"
        Private Const ReasonArchiveRefined As String = "ArchiveRefined"
        Private Const ReasonArchiveGeneric As String = "ArchiveGeneric"

        ''' <summary>
        ''' Setzt globale Default-Optionen als Snapshot.
        ''' </summary>
        ''' <param name="opt">Quelloptionen fuer den globalen Snapshot.</param>
        Friend Shared Sub SetDefaultOptions(opt As FileTypeDetectorOptions)
            FileTypeOptions.SetSnapshot(opt)
        End Sub

        ''' <summary>
        ''' Liefert einen Snapshot der aktuellen Default-Optionen.
        ''' </summary>
        ''' <returns>Unabhaengige Kopie der globalen Optionen.</returns>
        Friend Shared Function GetDefaultOptions() As FileTypeDetectorOptions
            Return FileTypeOptions.GetSnapshot()
        End Function

        ''' <summary>
        ''' Laedt Optionen aus einer JSON-Datei.
        ''' Unbekannte Schluessel werden ignoriert, Parse-/IO-Fehler fallen auf Defaults zurueck.
        ''' </summary>
        ''' <param name="path">Pfad zur JSON-Konfigurationsdatei.</param>
        ''' <returns>Geparste Optionen oder Defaults.</returns>
        Friend Shared Function LoadOptions(path As String) As FileTypeDetectorOptions
            Dim defaults = FileTypeDetectorOptions.DefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(defaults.Logger, "[Config] Datei nicht gefunden, Defaults.")
                Return defaults
            End If

            If Not path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then
                LogGuard.Warn(defaults.Logger, "[Config] Nur JSON unterstuetzt, Defaults.")
                Return defaults
            End If

            Try
                If Not FileTypeOptions.LoadOptionsFromPath(path) Then Return defaults
                Return FileTypeOptions.GetSnapshot()
            Catch ex As Exception
                LogGuard.Warn(defaults.Logger, $"[Config] Parse/IO-Fehler: {ex.Message}, Defaults.")
                Return defaults
            End Try
        End Function

        ''' <summary>
        ''' Liest eine Datei begrenzt in Memory ein. Wird z. B. fuer Detect(byte())-Workflows verwendet.
        ''' </summary>
        ''' <param name="path">Dateipfad.</param>
        ''' <returns>Gelesene Bytes oder leeres Array bei Fehler/Verletzung der Grenzen.</returns>
        Public Shared Function ReadFileSafe(path As String) As Byte()
            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                Return Array.Empty(Of Byte)()
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 OrElse fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu gross ({fi.Length} > {opt.MaxBytes}).")
                    Return Array.Empty(Of Byte)()
                End If

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Using ms As New MemoryStream(CInt(Math.Min(fi.Length, Integer.MaxValue)))
                        StreamBounds.CopyBounded(fs, ms, opt.MaxBytes)
                        Return ms.ToArray()
                    End Using
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] ReadFileSafe Fehler.", ex)
                Return Array.Empty(Of Byte)()
            End Try
        End Function

        ''' <summary>
        ''' Erkennt den Dateityp anhand eines Dateipfads.
        ''' Entscheidungspfad:
        ''' 1) Header lesen + Registry-Magic-Detektion (SSOT)
        ''' 2) ZIP-Gate und OOXML-Refinement
        ''' 3) fail-closed auf Unknown
        ''' </summary>
        ''' <param name="path">Dateipfad.</param>
        ''' <returns>Erkannter Typ oder Unknown.</returns>
        Public Function Detect(path As String) As FileType
            Return Detect(path, verifyExtension:=False)
        End Function

        ''' <summary>
        ''' Erkennt den Dateityp anhand eines Dateipfads und optionaler Endungspruefung.
        ''' </summary>
        ''' <param name="path">Dateipfad.</param>
        ''' <param name="verifyExtension">Aktiviert die fail-closed Endungspruefung nach Inhaltsdetektion.</param>
        ''' <returns>Erkannter Typ oder Unknown bei Mismatch/Fehler.</returns>
        Public Function Detect(path As String, verifyExtension As Boolean) As FileType
            Dim detected = DetectPathCore(path)
            Return ApplyExtensionPolicy(path, detected, verifyExtension)
        End Function

        ''' <summary>
        ''' Liefert ein detailliertes, auditierbares Detektionsergebnis.
        ''' </summary>
        Public Function DetectDetailed(path As String) As DetectionDetail
            Return DetectDetailed(path, verifyExtension:=False)
        End Function

        ''' <summary>
        ''' Liefert ein detailliertes, auditierbares Detektionsergebnis inkl. Endungs-Policy.
        ''' </summary>
        Public Function DetectDetailed(path As String, verifyExtension As Boolean) As DetectionDetail
            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty

            Dim detected As FileType = DetectPathCoreWithTrace(path, opt, trace)
            Dim extensionOk As Boolean = True
            If verifyExtension Then
                extensionOk = ExtensionMatchesKind(path, detected.Kind)
                If Not extensionOk Then
                    detected = UnknownType()
                    trace.ReasonCode = ReasonExtensionMismatch
                End If
            End If

            Return New DetectionDetail(
                detected,
                trace.ReasonCode,
                trace.UsedZipContentCheck,
                trace.UsedStructuredRefinement,
                verifyExtension AndAlso extensionOk)
        End Function

        ''' <summary>
        ''' Prueft, ob die Dateiendung zum inhaltsbasiert erkannten Typ passt.
        ''' </summary>
        ''' <param name="path">Dateipfad.</param>
        ''' <returns>True bei fehlender Endung oder passender Endung, sonst False.</returns>
        Public Function DetectAndVerifyExtension(path As String) As Boolean
            Dim detected = Detect(path)
            Return ExtensionMatchesKind(path, detected.Kind)
        End Function

        ''' <summary>
        ''' Prueft, ob eine Datei ein sicherer Archiv-Container ist (inkl. ZIP).
        ''' </summary>
        Public Function TryValidateArchive(path As String) As Boolean
            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False

            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Dim descriptor As ArchiveDescriptor = Nothing
                    If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return False
                    If fs.CanSeek Then fs.Position = 0
                    Return ArchiveSafetyGate.IsArchiveSafeStream(fs, opt, descriptor, depth:=0)
                End Using
            Catch
                Return False
            End Try
        End Function

        Private Function DetectPathCore(path As String) As FileType
            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty
            Return DetectPathCoreWithTrace(path, opt, trace)
        End Function

        Private Function DetectPathCoreWithTrace(path As String, opt As FileTypeDetectorOptions, ByRef trace As DetectionTrace) As FileType
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                trace.ReasonCode = ReasonFileNotFound
                Return UnknownType()
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 Then
                    trace.ReasonCode = ReasonInvalidLength
                    Return UnknownType()
                End If
                If fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu gross ({fi.Length} > {opt.MaxBytes}).")
                    trace.ReasonCode = ReasonFileTooLarge
                    Return UnknownType()
                End If

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Dim header = ReadHeader(fs, opt.SniffBytes, opt.MaxBytes)
                    Return ResolveByHeaderForPath(header, opt, trace, fs)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                trace.ReasonCode = ReasonException
                Return UnknownType()
            End Try
        End Function

        ''' <summary>
        ''' Erkennt den Dateityp anhand von In-Memory-Daten.
        ''' </summary>
        ''' <param name="data">Zu pruefende Nutzdaten.</param>
        ''' <returns>Erkannter Typ oder Unknown.</returns>
        Public Function Detect(data As Byte()) As FileType
            Dim opt = GetDefaultOptions()
            Return DetectInternalBytes(data, opt)
        End Function

        ''' <summary>
        ''' Deterministische Typpruefung als Convenience-API.
        ''' </summary>
        ''' <param name="data">Zu pruefende Nutzdaten.</param>
        ''' <param name="kind">Erwarteter Typ.</param>
        ''' <returns>True bei Typgleichheit, sonst False.</returns>
        Public Function IsOfType(data As Byte(), kind As FileKind) As Boolean
            Return Detect(data).Kind = kind
        End Function

        ''' <summary>
        ''' Entpackt ein Archiv deterministisch und fail-closed in ein neues Zielverzeichnis.
        ''' Sicherheitsregeln (Traversal/Limits/Nesting) sind immer aktiv.
        ''' </summary>
        ''' <param name="path">Pfad zur Archivdatei.</param>
        ''' <param name="destinationDirectory">Leeres, noch nicht existierendes Zielverzeichnis.</param>
        ''' <param name="verifyBeforeExtract">Optionale Vorpruefung ueber Detect(path).</param>
        ''' <returns>True bei erfolgreichem, atomarem Entpacken.</returns>
        Public Function ExtractArchiveSafe(path As String, destinationDirectory As String, verifyBeforeExtract As Boolean) As Boolean
            Dim opt = GetDefaultOptions()
            If Not CanExtractArchivePath(path, verifyBeforeExtract, opt) Then Return False

            Try
                Dim payload = ReadFileSafe(path)
                If payload.Length = 0 Then Return False
                Return FileMaterializer.Persist(payload, destinationDirectory, overwrite:=False, secureExtract:=True)
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[ArchiveExtract] Ausnahme, fail-closed.", ex)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Extrahiert Archiv-Inhalte sicher in Memory und gibt sie als wiederverwendbare Objekte zurueck.
        ''' Es erfolgt keine persistente Speicherung; Fehler liefern fail-closed eine leere Liste.
        ''' </summary>
        ''' <param name="path">Pfad zur Archivdatei.</param>
        ''' <param name="verifyBeforeExtract">Optionale Vorpruefung ueber Detect(path).</param>
        ''' <returns>Read-only Liste extrahierter Eintraege oder leer bei Fehler.</returns>
        Public Function ExtractArchiveSafeToMemory(path As String, verifyBeforeExtract As Boolean) As IReadOnlyList(Of ZipExtractedEntry)
            Dim opt = GetDefaultOptions()
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If Not CanExtractArchivePath(path, verifyBeforeExtract, opt) Then Return emptyResult

            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Return ArchiveExtractor.TryExtractArchiveStreamToMemory(fs, opt)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[ArchiveExtract] Ausnahme, fail-closed.", ex)
                Return emptyResult
            End Try
        End Function

        Private Function DetectInternalBytes(data As Byte(), opt As FileTypeDetectorOptions) As FileType
            If data Is Nothing OrElse data.Length = 0 Then Return UnknownType()
            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Detect] Daten zu gross ({data.Length} > {opt.MaxBytes}).")
                Return UnknownType()
            End If

            Try
                Dim trace As DetectionTrace = DetectionTrace.Empty
                Return ResolveByHeaderForBytes(data, opt, trace, data)
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                Return UnknownType()
            End Try
        End Function

        ''' <summary>
        ''' Entscheidungslogik fuer die Pfad-Variante.
        ''' </summary>
        Private Function ResolveByHeaderForPath(
            header As Byte(),
            opt As FileTypeDetectorOptions,
            ByRef trace As DetectionTrace,
            fs As FileStream
        ) As FileType
            Return ResolveByHeaderCommon(
                header,
                opt,
                trace,
                tryDescribe:=Function()
                                 Dim descriptor As ArchiveDescriptor = Nothing
                                 If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return Nothing
                                 Return descriptor
                             End Function,
                tryValidate:=Function(descriptor)
                                 Return ValidateArchiveStreamRaw(fs, opt, descriptor)
                             End Function,
                tryRefine:=Function()
                               Return OpenXmlRefiner.TryRefineStream(fs)
                           End Function)
        End Function

        ''' <summary>
        ''' Entscheidungslogik fuer die Byte-Variante.
        ''' </summary>
        Private Function ResolveByHeaderForBytes(
            header As Byte(),
            opt As FileTypeDetectorOptions,
            ByRef trace As DetectionTrace,
            data As Byte()
        ) As FileType
            Return ResolveByHeaderCommon(
                header,
                opt,
                trace,
                tryDescribe:=Function()
                                 Dim descriptor As ArchiveDescriptor = Nothing
                                 If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return Nothing
                                 Return descriptor
                             End Function,
                tryValidate:=Function(descriptor)
                                 Return ValidateArchiveBytesRaw(data, opt, descriptor)
                             End Function,
                tryRefine:=Function()
                               Using ms = CreateReadOnlyMemoryStream(data)
                                   Return OpenXmlRefiner.TryRefineStream(ms)
                               End Using
                           End Function)
        End Function

        Private Function ResolveByHeaderCommon(
            header As Byte(),
            opt As FileTypeDetectorOptions,
            ByRef trace As DetectionTrace,
            tryDescribe As Func(Of ArchiveDescriptor),
            tryValidate As Func(Of ArchiveDescriptor, Boolean),
            tryRefine As Func(Of FileType)
        ) As FileType
            If header Is Nothing OrElse header.Length = 0 Then
                trace.ReasonCode = ReasonHeaderUnknown
                Return UnknownType()
            End If

            Dim magicKind = FileTypeRegistry.DetectByMagic(header)
            If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                trace.ReasonCode = ReasonHeaderMatch
                Return FileTypeRegistry.Resolve(magicKind)
            End If

            Dim descriptor As ArchiveDescriptor = Nothing
            If magicKind = FileKind.Zip Then
                descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip)
            Else
                descriptor = tryDescribe()
                If descriptor Is Nothing Then
                    trace.ReasonCode = ReasonHeaderUnknown
                    Return UnknownType()
                End If
            End If

            trace.UsedZipContentCheck = True
            If Not tryValidate(descriptor) Then
                LogGuard.Warn(opt.Logger, "[Detect] Archive-Gate verletzt.")
                trace.ReasonCode = ReasonArchiveGateFailed
                Return UnknownType()
            End If

            Return ResolveAfterArchiveGate(magicKind, opt, trace, tryRefine)
        End Function

        Private Function ValidateArchiveStreamRaw(
            fs As FileStream,
            opt As FileTypeDetectorOptions,
            descriptor As ArchiveDescriptor
        ) As Boolean
            If fs Is Nothing OrElse Not fs.CanRead Then Return False
            If fs.CanSeek Then fs.Position = 0
            Return ArchiveSafetyGate.IsArchiveSafeStream(fs, opt, descriptor, depth:=0)
        End Function

        Private Function ValidateArchiveBytesRaw(
            data As Byte(),
            opt As FileTypeDetectorOptions,
            descriptor As ArchiveDescriptor
        ) As Boolean
            Return ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
        End Function

        Private Function ResolveAfterArchiveGate(
            magicKind As FileKind,
            opt As FileTypeDetectorOptions,
            ByRef trace As DetectionTrace,
            tryRefine As Func(Of FileType)
        ) As FileType
            If magicKind <> FileKind.Zip Then
                trace.ReasonCode = ReasonArchiveGeneric
                Return FileTypeRegistry.Resolve(FileKind.Zip)
            End If

            Dim refined = tryRefine()
            Return FinalizeArchiveDetection(refined, opt, trace)
        End Function

        Private Function FinalizeArchiveDetection(refined As FileType, opt As FileTypeDetectorOptions, ByRef trace As DetectionTrace) As FileType
            If refined.Kind <> FileKind.Unknown Then
                WarnIfNoDirectContentDetection(refined.Kind, opt)
                trace.UsedStructuredRefinement = (refined.Kind = FileKind.Docx OrElse refined.Kind = FileKind.Xlsx OrElse refined.Kind = FileKind.Pptx)
                trace.ReasonCode = If(trace.UsedStructuredRefinement, ReasonArchiveStructuredRefined, ReasonArchiveRefined)
                Return refined
            End If

            trace.ReasonCode = ReasonArchiveGeneric
            Return FileTypeRegistry.Resolve(FileKind.Zip)
        End Function

        Private Function CanExtractArchivePath(path As String, verifyBeforeExtract As Boolean, opt As FileTypeDetectorOptions) As Boolean
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Quelldatei fehlt.")
                Return False
            End If

            If verifyBeforeExtract Then
                Dim detected = Detect(path)
                If Not IsArchiveContainerKind(detected.Kind) Then
                    LogGuard.Warn(opt.Logger, $"[ArchiveExtract] Vorpruefung fehlgeschlagen ({detected.Kind}).")
                    Return False
                End If
            End If

            Return True
        End Function

        Private Shared Function ApplyExtensionPolicy(path As String, detected As FileType, verifyExtension As Boolean) As FileType
            If Not verifyExtension Then Return detected
            If ExtensionMatchesKind(path, detected.Kind) Then Return detected
            Return UnknownType()
        End Function

        Private Shared Function IsArchiveContainerKind(kind As FileKind) As Boolean
            Return kind = FileKind.Zip OrElse
                kind = FileKind.Docx OrElse
                kind = FileKind.Xlsx OrElse
                kind = FileKind.Pptx
        End Function

        Private Shared Sub WarnIfNoDirectContentDetection(kind As FileKind, opt As FileTypeDetectorOptions)
            If kind = FileKind.Unknown Then Return
            If FileTypeRegistry.HasDirectContentDetection(kind) Then Return
            LogGuard.Warn(opt.Logger, $"[Detect] Keine direkte Content-Erkennung fuer Typ '{kind}'. Ergebnis stammt aus Fallback/Refinement.")
        End Sub

        Private Shared Function ExtensionMatchesKind(path As String, detectedKind As FileKind) As Boolean
            Dim ext = Global.System.IO.Path.GetExtension(If(path, String.Empty))
            If String.IsNullOrWhiteSpace(ext) Then Return True

            If detectedKind = FileKind.Unknown Then Return False

            Dim normalizedExt = FileTypeRegistry.NormalizeAlias(ext)
            Dim detectedType = FileTypeRegistry.Resolve(detectedKind)

            If normalizedExt = FileTypeRegistry.NormalizeAlias(detectedType.CanonicalExtension) Then
                Return True
            End If

            If Not detectedType.Aliases.IsDefault Then
                For Each a In detectedType.Aliases
                    If String.Equals(a, normalizedExt, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function

        Private Shared Function ReadHeader(input As FileStream, sniffBytes As Integer, maxBytes As Long) As Byte()
            Try
                If input Is Nothing OrElse Not input.CanRead Then Return Array.Empty(Of Byte)()
                If maxBytes <= 0 Then Return Array.Empty(Of Byte)()
                If input.CanSeek Then
                    If input.Length <= 0 OrElse input.Length > maxBytes Then Return Array.Empty(Of Byte)()
                    input.Position = 0
                End If

                Dim want As Integer = sniffBytes
                If want <= 0 Then want = InternalIoDefaults.DefaultSniffBytes
                Dim take As Integer = want
                If input.CanSeek Then
                    take = CInt(Math.Min(input.Length, want))
                End If
                If take <= 0 Then Return Array.Empty(Of Byte)()

                Dim buf(take - 1) As Byte
                Dim off As Integer = 0
                While off < take
                    Dim n = input.Read(buf, off, take - off)
                    If n <= 0 Then Exit While
                    off += n
                End While

                If off <= 0 Then Return Array.Empty(Of Byte)()
                If off < take Then
                    Dim exact(off - 1) As Byte
                    Array.Copy(buf, exact, off)
                    Return exact
                End If

                Return buf
            Catch
                Return Array.Empty(Of Byte)()
            End Try
        End Function

        Private Shared Function UnknownType() As FileType
            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

        Private Shared Function CreateReadOnlyMemoryStream(data As Byte()) As MemoryStream
            Return New MemoryStream(data, 0, data.Length, writable:=False, publiclyVisible:=False)
        End Function

        Private Structure DetectionTrace
            Friend ReasonCode As String
            Friend UsedZipContentCheck As Boolean
            Friend UsedStructuredRefinement As Boolean

            Friend Shared ReadOnly Property Empty As DetectionTrace
                Get
                    Return New DetectionTrace With {.ReasonCode = ReasonUnknown}
                End Get
            End Property
        End Structure

    End Class

End Namespace
