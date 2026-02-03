Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text.Json

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

        Private Shared ReadOnly _optionsLock As New Object()
        Private Shared _defaultOptions As FileTypeDetectorOptions = FileTypeDetectorOptions.DefaultOptions()

        ''' <summary>
        ''' Setzt globale Default-Optionen als Snapshot.
        ''' </summary>
        ''' <param name="opt">Quelloptionen fuer den globalen Snapshot.</param>
        Public Shared Sub SetDefaultOptions(opt As FileTypeDetectorOptions)
            SyncLock _optionsLock
                _defaultOptions = Snapshot(opt)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Liefert einen Snapshot der aktuellen Default-Optionen.
        ''' </summary>
        ''' <returns>Unabhaengige Kopie der globalen Optionen.</returns>
        Public Shared Function GetDefaultOptions() As FileTypeDetectorOptions
            SyncLock _optionsLock
                Return Snapshot(_defaultOptions)
            End SyncLock
        End Function

        ''' <summary>
        ''' Laedt Optionen aus einer JSON-Datei.
        ''' Unbekannte Schluessel werden ignoriert, Parse-/IO-Fehler fallen auf Defaults zurueck.
        ''' </summary>
        ''' <param name="path">Pfad zur JSON-Konfigurationsdatei.</param>
        ''' <returns>Geparste Optionen oder Defaults.</returns>
        Public Shared Function LoadOptions(path As String) As FileTypeDetectorOptions
            Dim defaults = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(defaults.Logger, "[Config] Datei nicht gefunden, Defaults.")
                Return defaults
            End If

            If Not path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then
                LogGuard.Warn(defaults.Logger, "[Config] Nur JSON unterstuetzt, Defaults.")
                Return defaults
            End If

            Try
                Dim json = File.ReadAllText(path)
                Dim result = FileTypeDetectorOptions.DefaultOptions()

                Using doc = JsonDocument.Parse(json, New JsonDocumentOptions With {.AllowTrailingCommas = True})
                    For Each p In doc.RootElement.EnumerateObject()
                        
                        Select Case p.Name.ToLowerInvariant()
                            Case "maxbytes" : result.MaxBytes = SafeLong(p.Value, result.MaxBytes)
                            Case "sniffbytes" : result.SniffBytes = SafeInt(p.Value, result.SniffBytes)
                            Case "maxzipentries" : result.MaxZipEntries = SafeInt(p.Value, result.MaxZipEntries)
                            Case "maxziptotaluncompressedbytes" : result.MaxZipTotalUncompressedBytes = SafeLong(p.Value, result.MaxZipTotalUncompressedBytes)
                            Case "maxzipentryuncompressedbytes" : result.MaxZipEntryUncompressedBytes = SafeLong(p.Value, result.MaxZipEntryUncompressedBytes)
                            Case "maxzipcompressionratio" : result.MaxZipCompressionRatio = SafeInt(p.Value, result.MaxZipCompressionRatio)
                            Case "maxzipnestingdepth" : result.MaxZipNestingDepth = SafeInt(p.Value, result.MaxZipNestingDepth)
                            Case "maxzipnestedbytes" : result.MaxZipNestedBytes = SafeLong(p.Value, result.MaxZipNestedBytes)
                            Case Else
                                LogGuard.Warn(defaults.Logger, $"[Config] Unbekannter Schluessel '{p.Name}' ignoriert.")
                        End Select
                        
                    Next
                End Using

                Return result
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

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
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
        ''' Liefert ein detailiertes, auditierbares Detektionsergebnis.
        ''' </summary>
        Public Function DetectDetailed(path As String) As DetectionDetail
            Return DetectDetailed(path, verifyExtension:=False)
        End Function

        ''' <summary>
        ''' Liefert ein detailiertes, auditierbares Detektionsergebnis inkl. Endungs-Policy.
        ''' </summary>
        Public Function DetectDetailed(path As String, verifyExtension As Boolean) As DetectionDetail
            Dim opt = GetDefaultOptions()
            Dim trace As DetectionTrace = DetectionTrace.Empty

            Dim detected As FileType = DetectPathCoreWithTrace(path, opt, trace)
            Dim extensionOk As Boolean = True
            If verifyExtension Then
                extensionOk = ExtensionMatchesKind(path, detected.Kind)
                If Not extensionOk Then
                    detected = FileTypeRegistry.Resolve(FileKind.Unknown)
                    trace.ReasonCode = "ExtensionMismatch"
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
        ''' Prueft, ob eine Datei ein sicherer ZIP-Container ist.
        ''' </summary>
        Public Function TryValidateZip(path As String) As Boolean
            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False

            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                    Dim header = ReadHeader(fs, opt.SniffBytes, opt.MaxBytes)
                    If FileTypeRegistry.DetectByMagic(header) <> FileKind.Zip Then Return False
                    If fs.CanSeek Then fs.Position = 0
                    Return ZipSafetyGate.IsZipSafeStream(fs, opt, depth:=0)
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
                trace.ReasonCode = "FileNotFound"
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 Then
                    trace.ReasonCode = "InvalidLength"
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If
                If fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu gross ({fi.Length} > {opt.MaxBytes}).")
                    trace.ReasonCode = "FileTooLarge"
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                    Dim header = ReadHeader(fs, opt.SniffBytes, opt.MaxBytes)
                    Return ResolveByHeaderAndFallback(
                        header,
                        opt,
                        trace,
                        Function()
                            If fs.CanSeek Then fs.Position = 0
                            Return ZipSafetyGate.IsZipSafeStream(fs, opt, depth:=0)
                        End Function,
                        Function()
                            Return OpenXmlRefiner.TryRefineStream(fs)
                        End Function)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                trace.ReasonCode = "Exception"
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
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
        ''' Entpackt ein ZIP deterministisch und fail-closed in ein neues Zielverzeichnis.
        ''' Sicherheitsregeln (Traversal/Limits/Nesting) sind immer aktiv.
        ''' </summary>
        ''' <param name="path">Pfad zur ZIP-Datei.</param>
        ''' <param name="destinationDirectory">Leeres, noch nicht existierendes Zielverzeichnis.</param>
        ''' <param name="verifyBeforeExtract">Optionale Vorpruefung ueber Detect(path).</param>
        ''' <returns>True bei erfolgreichem, atomarem Entpacken.</returns>
        Public Function ExtractZipSafe(path As String, destinationDirectory As String, verifyBeforeExtract As Boolean) As Boolean
            Dim opt = GetDefaultOptions()
            If Not CanExtractZipPath(path, verifyBeforeExtract, opt) Then Return False

            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                    Return ZipExtractor.TryExtractZipStream(fs, destinationDirectory, opt)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[ZipExtract] Ausnahme, fail-closed.", ex)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Extrahiert ZIP-Inhalte sicher in Memory und gibt sie als wiederverwendbare Objekte zurueck.
        ''' Es erfolgt keine persistente Speicherung; Fehler liefern fail-closed eine leere Liste.
        ''' </summary>
        ''' <param name="path">Pfad zur ZIP-Datei.</param>
        ''' <param name="verifyBeforeExtract">Optionale Vorpruefung ueber Detect(path).</param>
        ''' <returns>Read-only Liste extrahierter Eintraege oder leer bei Fehler.</returns>
        Public Function ExtractZipSafeToMemory(path As String, verifyBeforeExtract As Boolean) As IReadOnlyList(Of ZipExtractedEntry)
            Dim opt = GetDefaultOptions()
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If Not CanExtractZipPath(path, verifyBeforeExtract, opt) Then Return emptyResult

            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                    Return ZipExtractor.TryExtractZipStreamToMemory(fs, opt)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[ZipExtract] Ausnahme, fail-closed.", ex)
                Return emptyResult
            End Try
        End Function

        Private Function DetectInternalBytes(data As Byte(), opt As FileTypeDetectorOptions) As FileType
            If data Is Nothing OrElse data.Length = 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)
            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Detect] Daten zu gross ({data.Length} > {opt.MaxBytes}).")
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                Dim trace As DetectionTrace = DetectionTrace.Empty
                Return ResolveByHeaderAndFallback(
                    data,
                    opt,
                    trace,
                    Function()
                        Return ZipSafetyGate.IsZipSafeBytes(data, opt)
                    End Function,
                    Function()
                        Return OpenXmlRefiner.TryRefine(
                            Function()
                                Return CType(New MemoryStream(data, 0, data.Length, writable:=False, publiclyVisible:=False), Stream)
                            End Function)
                    End Function)
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        ''' <summary>
        ''' Gemeinsame Entscheidungslogik fuer Path- und Byte-Variante.
        ''' </summary>
        Private Function ResolveByHeaderAndFallback(
            header As Byte(),
            opt As FileTypeDetectorOptions,
            ByRef trace As DetectionTrace,
            zipSafetyCheck As Func(Of Boolean),
            tryRefine As Func(Of FileType)
        ) As FileType
            If header Is Nothing OrElse header.Length = 0 Then
                trace.ReasonCode = "HeaderUnknown"
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Dim magicKind = FileTypeRegistry.DetectByMagic(header)
            If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                trace.ReasonCode = "HeaderMatch"
                Return FileTypeRegistry.Resolve(magicKind)
            End If

            Dim zipLike As Boolean = (magicKind = FileKind.Zip)

            If zipLike Then
                trace.UsedZipContentCheck = True
                If zipSafetyCheck Is Nothing OrElse Not zipSafetyCheck() Then
                    LogGuard.Warn(opt.Logger, "[Detect] ZIP-Gate verletzt.")
                    trace.ReasonCode = "ZipGateFailed"
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If

                Dim refined = FileTypeRegistry.Resolve(FileKind.Unknown)
                If tryRefine IsNot Nothing Then
                    refined = tryRefine()
                End If

                If refined.Kind <> FileKind.Unknown Then
                    WarnIfNoDirectContentDetection(refined.Kind, opt)
                    trace.UsedStructuredRefinement = (refined.Kind = FileKind.Docx OrElse refined.Kind = FileKind.Xlsx OrElse refined.Kind = FileKind.Pptx)
                    trace.ReasonCode = If(trace.UsedStructuredRefinement, "ZipStructuredRefined", "ZipRefined")
                    Return refined
                End If

                trace.ReasonCode = "ZipGeneric"
                Return FileTypeRegistry.Resolve(FileKind.Zip)
            End If

            trace.ReasonCode = "HeaderUnknown"
            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

        Private Function CanExtractZipPath(path As String, verifyBeforeExtract As Boolean, opt As FileTypeDetectorOptions) As Boolean
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[ZipExtract] Quelldatei fehlt.")
                Return False
            End If

            If verifyBeforeExtract Then
                Dim detected = Detect(path)
                If Not IsZipContainerKind(detected.Kind) Then
                    LogGuard.Warn(opt.Logger, $"[ZipExtract] Vorpruefung fehlgeschlagen ({detected.Kind}).")
                    Return False
                End If
            End If

            Return True
        End Function

        Private Shared Function ApplyExtensionPolicy(path As String, detected As FileType, verifyExtension As Boolean) As FileType
            If Not verifyExtension Then Return detected
            If ExtensionMatchesKind(path, detected.Kind) Then Return detected
            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

        Private Shared Function IsZipContainerKind(kind As FileKind) As Boolean
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
            Dim ext = System.IO.Path.GetExtension(If(path, String.Empty))
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

        Private Shared Function SafeInt(el As JsonElement, fallback As Integer) As Integer
            Dim v As Integer
            If el.ValueKind = JsonValueKind.Number AndAlso el.TryGetInt32(v) Then Return v
            Return fallback
        End Function

        Private Shared Function SafeLong(el As JsonElement, fallback As Long) As Long
            Dim v As Long
            If el.ValueKind = JsonValueKind.Number AndAlso el.TryGetInt64(v) Then Return v
            Return fallback
        End Function

        Private Shared Function Snapshot(opt As FileTypeDetectorOptions) As FileTypeDetectorOptions
            If opt Is Nothing Then Return FileTypeDetectorOptions.DefaultOptions()
            Return opt.Clone()
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
                If want <= 0 Then want = 4096
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

        Private Structure DetectionTrace
            Friend ReasonCode As String
            Friend UsedZipContentCheck As Boolean
            Friend UsedStructuredRefinement As Boolean

            Friend Shared ReadOnly Property Empty As DetectionTrace
                Get
                    Return New DetectionTrace With {.ReasonCode = "Unknown"}
                End Get
            End Property
        End Structure

    End Class

End Namespace
