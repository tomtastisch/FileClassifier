Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text.Json

Namespace FileTypeDetection

    ''' <summary>
    ''' Öffentliche API zur inhaltsbasierten Dateityp-Erkennung (fail-closed).
    ''' </summary>
    Public NotInheritable Class FileTypeDetector

        ' =====================================================================
        ' Config API
        ' =====================================================================

        Private Shared _defaultOptions As FileTypeDetectorOptions = FileTypeDetectorOptions.DefaultOptions()
        Private Shared ReadOnly _sniffer As IContentSniffer = New LibMagicSniffer()

        ''' <summary>Setzt globale Default-Optionen (Snapshot).</summary>
        Public Shared Sub SetDefaultOptions(opt As FileTypeDetectorOptions)
            _defaultOptions = Snapshot(opt)
        End Sub

        ''' <summary>Gibt einen Snapshot der aktuellen Default-Optionen zurück.</summary>
        Public Shared Function GetDefaultOptions() As FileTypeDetectorOptions
            Return Snapshot(_defaultOptions)
        End Function

        ''' <summary>
        ''' Lädt Optionen aus JSON (.json). Unbekannte Keys werden ignoriert, Fehler ergeben Defaults.
        ''' </summary>
        Public Shared Function LoadOptions(path As String) As FileTypeDetectorOptions
            Dim defaults = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(defaults.Logger, "[Config] Datei nicht gefunden, Defaults.")
                Return defaults
            End If

            If Not path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then
                LogGuard.Warn(defaults.Logger, "[Config] Nur JSON unterstützt, Defaults.")
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
                                LogGuard.Warn(defaults.Logger, $"[Config] Unbekannter Schlüssel '{p.Name}' ignoriert.")
                        End Select
                    Next
                End Using

                Return result

            Catch ex As Exception
                LogGuard.Warn(defaults.Logger, $"[Config] Parse/IO-Fehler: {ex.Message}, Defaults.")
                Return defaults
            End Try
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

        ' =====================================================================
        ' Public Detection API
        ' =====================================================================

        ''' <summary>Sicheres Einlesen einer Datei (bounded, fail-closed). (Für Detect(byte()) / externe Nutzung.)</summary>
        Public Shared Function ReadFileSafe(path As String) As Byte()
            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                Return Array.Empty(Of Byte)()
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 OrElse fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu groß ({fi.Length} > {opt.MaxBytes}).")
                    Return Array.Empty(Of Byte)()
                End If

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                    Using ms As New MemoryStream(CInt(Math.Min(fi.Length, Integer.MaxValue)))
                        CopyBounded(fs, ms, opt.MaxBytes)
                        Return ms.ToArray()
                    End Using
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] ReadFileSafe Fehler.", ex)
                Return Array.Empty(Of Byte)()
            End Try
        End Function

        ''' <summary>Erkennt den Dateityp anhand eines Pfads (partial read; ZIP stream-basiert).</summary>
        Public Function Detect(path As String) As FileType
            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

                ' 1) Partial Read (Header) -> deterministische Magic SSOT
                Dim header = ReadHeader(path, opt.SniffBytes, opt.MaxBytes, opt.Logger)
                If header.Length = 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

                Dim magicKind = MagicDetect(header)

                ' nicht-ZIP: direkt entscheiden
                If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                    Return FileTypeRegistry.Resolve(magicKind)
                End If

                ' 2) Sniffer (MimeGuesser) bleibt als Fallback (auf Header)
                Dim aliasKey = _sniffer.SniffAlias(header, opt.SniffBytes, opt.Logger)
                Dim baseType = FileTypeRegistry.ResolveByAlias(aliasKey)

                ' 3) ZIP stream-basiert: Gate + OOXML refine
                Dim zipLike As Boolean =
                    (magicKind = FileKind.Zip) OrElse
                    (baseType IsNot Nothing AndAlso baseType.Kind = FileKind.Zip)

                If zipLike Then
                    ' Gate über Stream (kein Voll-Read der Datei)
                    Dim okGate As Boolean
                    Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                        okGate = ZipSafetyGate.IsZipSafeStream(fs, opt, depth:=0)
                    End Using
                    If Not okGate Then
                        LogGuard.Warn(opt.Logger, "[Detect] ZIP-Gate verletzt.")
                        Return FileTypeRegistry.Resolve(FileKind.Unknown)
                    End If

                    ' OOXML refine: pro Probe ein frischer Stream (seekbar, Position=0)
                    Dim refined = OpenXmlRefiner.TryRefine(
                        Function()
                            Return CType(New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan), Stream)
                        End Function
                    )

                    If refined.Kind <> FileKind.Unknown Then
                        Return refined
                    End If

                    ' echtes ZIP
                    Return FileTypeRegistry.Resolve(FileKind.Zip)
                End If

                ' 4) Nicht-ZIP: sniffer-basiert, fail-closed
                If baseType Is Nothing OrElse Not baseType.Allowed OrElse baseType.Kind = FileKind.Unknown Then
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If

                Return baseType

            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        ''' <summary>Erkennt den Dateityp anhand von Bytes (bounded via Options.MaxBytes).</summary>
        Public Function Detect(data As Byte()) As FileType
            Dim opt = GetDefaultOptions()
            Return DetectInternalBytes(data, opt)
        End Function

        ''' <summary>Deterministische Typprüfung.</summary>
        Public Function IsOfType(data As Byte(), kind As FileKind) As Boolean
            Return Detect(data).Kind = kind
        End Function

        ' =====================================================================
        ' Internals (Bytes-Variante bleibt für In-Memory Nutzung)
        ' =====================================================================

        Private Function DetectInternalBytes(data As Byte(), opt As FileTypeDetectorOptions) As FileType
            If data Is Nothing OrElse data.Length = 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)
            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Detect] Daten zu groß ({data.Length} > {opt.MaxBytes}).")
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                ' SSOT Magic
                Dim magicKind = MagicDetect(data)
                If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                    Return FileTypeRegistry.Resolve(magicKind)
                End If

                Dim aliasKey = _sniffer.SniffAlias(data, opt.SniffBytes, opt.Logger)
                Dim baseType = FileTypeRegistry.ResolveByAlias(aliasKey)

                Dim zipLike As Boolean =
                    (magicKind = FileKind.Zip) OrElse
                    (baseType IsNot Nothing AndAlso baseType.Kind = FileKind.Zip)

                If zipLike Then
                    If Not ZipSafetyGate.IsZipSafeBytes(data, opt) Then
                        LogGuard.Warn(opt.Logger, "[Detect] ZIP-Gate verletzt.")
                        Return FileTypeRegistry.Resolve(FileKind.Unknown)
                    End If

                    Dim refined = OpenXmlRefiner.TryRefine(
                        Function()
                            Return CType(New MemoryStream(data, 0, data.Length, writable:=False, publiclyVisible:=False), Stream)
                        End Function
                    )
                    If refined.Kind <> FileKind.Unknown Then Return refined

                    Return FileTypeRegistry.Resolve(FileKind.Zip)
                End If

                If baseType Is Nothing OrElse Not baseType.Allowed OrElse baseType.Kind = FileKind.Unknown Then
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If

                Return baseType

            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        ' =====================================================================
        ' Helpers
        ' =====================================================================

        Private Shared Function ReadHeader(path As String, sniffBytes As Integer, maxBytes As Long, log As Object) As Byte()
            Try
                Dim fi As New FileInfo(path)
                If fi.Length <= 0 Then Return Array.Empty(Of Byte)()
                If fi.Length > maxBytes Then
                    ' Für Detect(path) lassen wir Oversize zu, weil wir nur Header lesen,
                    ' aber wir verhindern Path Reads, die über MaxBytes gehen würden.
                    ' Hier: Header ist ok, Voll-Read nicht.
                End If

                Dim want As Integer = sniffBytes
                If want <= 0 Then want = 4096
                Dim take As Integer = CInt(Math.Min(fi.Length, want))

                Dim buf(take - 1) As Byte
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                    Dim off As Integer = 0
                    While off < take
                        Dim n = fs.Read(buf, off, take - off)
                        If n <= 0 Then Exit While
                        off += n
                    End While
                End Using
                Return buf
            Catch
                Return Array.Empty(Of Byte)()
            End Try
        End Function

        Private Shared Sub CopyBounded(input As Stream, output As Stream, maxBytes As Long)
            Dim buf(8191) As Byte
            Dim total As Long = 0
            While True
                Dim n = input.Read(buf, 0, buf.Length)
                If n <= 0 Then Exit While
                total += n
                If total > maxBytes Then Throw New InvalidOperationException("bounded copy exceeded")
                output.Write(buf, 0, n)
            End While
        End Sub

        Private Shared Function MagicDetect(d As Byte()) As FileKind
            Dim n = d.Length

            ' PDF: %PDF-
            If n >= 5 AndAlso d(0) = &H25 AndAlso d(1) = &H50 AndAlso d(2) = &H44 AndAlso d(3) = &H46 AndAlso d(4) = &H2D Then
                Return FileKind.Pdf
            End If

            ' JPEG: FF D8 FF
            If n >= 3 AndAlso d(0) = &HFF AndAlso d(1) = &HD8 AndAlso d(2) = &HFF Then
                Return FileKind.Jpeg
            End If

            ' PNG: 89 50 4E 47 0D 0A 1A 0A
            If n >= 8 AndAlso d(0) = &H89 AndAlso d(1) = &H50 AndAlso d(2) = &H4E AndAlso d(3) = &H47 AndAlso d(4) = &HD AndAlso d(5) = &HA AndAlso d(6) = &H1A AndAlso d(7) = &HA Then
                Return FileKind.Png
            End If

            ' GIF: GIF87a / GIF89a
            If n >= 6 AndAlso d(0) = AscW("G"c) AndAlso d(1) = AscW("I"c) AndAlso d(2) = AscW("F"c) AndAlso
               ((d(3) = AscW("8"c) AndAlso d(4) = AscW("7"c) AndAlso d(5) = AscW("a"c)) OrElse
                (d(3) = AscW("8"c) AndAlso d(4) = AscW("9"c) AndAlso d(5) = AscW("a"c))) Then
                Return FileKind.Gif
            End If

            ' WEBP: "RIFF" .... "WEBP"
            If n >= 12 AndAlso d(0) = AscW("R"c) AndAlso d(1) = AscW("I"c) AndAlso d(2) = AscW("F"c) AndAlso d(3) = AscW("F"c) AndAlso d(8) = AscW("W"c) AndAlso d(9) = AscW("E"c) AndAlso d(10) = AscW("B"c) AndAlso d(11) = AscW("P"c) Then
                Return FileKind.Webp
            End If

            ' ZIP: PK 03 04 / 05 06 / 07 08
            If n >= 4 AndAlso d(0) = &H50 AndAlso d(1) = &H4B AndAlso ((d(2) = &H3 AndAlso d(3) = &H4) OrElse (d(2) = &H5 AndAlso d(3) = &H6) OrElse (d(2) = &H7 AndAlso d(3) = &H8)) Then
                Return FileKind.Zip
            End If

            Return FileKind.Unknown
        End Function

    End Class

End Namespace
