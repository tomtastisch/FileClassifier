Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text.Json

Namespace FileTypeDetection

    ''' <summary>
    ''' Oeffentliche API zur inhaltsbasierten Dateityp-Erkennung.
    '''
    ''' Sicherheits- und Architekturprinzipien:
    ''' - fail-closed: jeder Fehlerpfad liefert FileKind.Unknown.
    ''' - SSOT: Magic-Bytes werden nur in MagicDetect gepflegt.
    ''' - Dateiendung ist nur Metadatum; die Entscheidung basiert auf Content.
    ''' - ZIP-Dateien laufen immer durch ZIP-Gate und optionales OOXML-Refinement.
    ''' </summary>
    Public NotInheritable Class FileTypeDetector

        Private Shared ReadOnly _optionsLock As New Object()
        Private Shared _defaultOptions As FileTypeDetectorOptions = FileTypeDetectorOptions.DefaultOptions()
        Private Shared ReadOnly _sniffer As IContentSniffer = New LibMagicSniffer()

        ''' <summary>
        ''' Setzt globale Default-Optionen als Snapshot.
        ''' </summary>
        Public Shared Sub SetDefaultOptions(opt As FileTypeDetectorOptions)
            SyncLock _optionsLock
                _defaultOptions = Snapshot(opt)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Liefert einen Snapshot der aktuellen Default-Optionen.
        ''' </summary>
        Public Shared Function GetDefaultOptions() As FileTypeDetectorOptions
            SyncLock _optionsLock
                Return Snapshot(_defaultOptions)
            End SyncLock
        End Function

        ''' <summary>
        ''' Laedt Optionen aus einer JSON-Datei.
        ''' Unbekannte Schluessel werden ignoriert, Parse-/IO-Fehler fallen auf Defaults zurueck.
        ''' </summary>
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
        ''' 1) Header lesen + MagicDetect (SSOT)
        ''' 2) Sniffer-Fallback (Alias)
        ''' 3) ZIP-Gate und OOXML-Refinement
        ''' 4) fail-closed auf Unknown
        ''' </summary>
        Public Function Detect(path As String) As FileType
            Dim opt = GetDefaultOptions()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LogGuard.Warn(opt.Logger, "[Detect] Datei nicht gefunden.")
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                Dim fi As New FileInfo(path)
                If fi.Length < 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)
                If fi.Length > opt.MaxBytes Then
                    LogGuard.Warn(opt.Logger, $"[Detect] Datei zu gross ({fi.Length} > {opt.MaxBytes}).")
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If

                Dim header = ReadHeader(path, opt.SniffBytes, opt.MaxBytes)
                Return ResolveByHeaderAndFallback(
                    header,
                    opt,
                    Function()
                        Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan)
                            Return ZipSafetyGate.IsZipSafeStream(fs, opt, depth:=0)
                        End Using
                    End Function,
                    Function()
                        Return CType(New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan), Stream)
                    End Function)
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Detect] Ausnahme, fail-closed.", ex)
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        ''' <summary>
        ''' Erkennt den Dateityp anhand von In-Memory-Daten.
        ''' </summary>
        Public Function Detect(data As Byte()) As FileType
            Dim opt = GetDefaultOptions()
            Return DetectInternalBytes(data, opt)
        End Function

        ''' <summary>
        ''' Deterministische Typpruefung als Convenience-API.
        ''' </summary>
        Public Function IsOfType(data As Byte(), kind As FileKind) As Boolean
            Return Detect(data).Kind = kind
        End Function

        Private Function DetectInternalBytes(data As Byte(), opt As FileTypeDetectorOptions) As FileType
            If data Is Nothing OrElse data.Length = 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)
            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Detect] Daten zu gross ({data.Length} > {opt.MaxBytes}).")
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                Return ResolveByHeaderAndFallback(
                    data,
                    opt,
                    Function()
                        Return ZipSafetyGate.IsZipSafeBytes(data, opt)
                    End Function,
                    Function()
                        Return CType(New MemoryStream(data, 0, data.Length, writable:=False, publiclyVisible:=False), Stream)
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
            zipSafetyCheck As Func(Of Boolean),
            streamFactory As Func(Of Stream)
        ) As FileType
            If header Is Nothing OrElse header.Length = 0 Then
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Dim magicKind = MagicDetect(header)
            If magicKind <> FileKind.Unknown AndAlso magicKind <> FileKind.Zip Then
                Return FileTypeRegistry.Resolve(magicKind)
            End If

            Dim aliasKey = _sniffer.SniffAlias(header, opt.SniffBytes, opt.Logger)
            Dim baseType = FileTypeRegistry.ResolveByAlias(aliasKey)

            Dim zipLike As Boolean =
                (magicKind = FileKind.Zip) OrElse
                (baseType IsNot Nothing AndAlso baseType.Kind = FileKind.Zip)

            If zipLike Then
                If zipSafetyCheck Is Nothing OrElse Not zipSafetyCheck() Then
                    LogGuard.Warn(opt.Logger, "[Detect] ZIP-Gate verletzt.")
                    Return FileTypeRegistry.Resolve(FileKind.Unknown)
                End If

                Dim refined = OpenXmlRefiner.TryRefine(streamFactory)
                If refined.Kind <> FileKind.Unknown Then Return refined

                Return FileTypeRegistry.Resolve(FileKind.Zip)
            End If

            If baseType Is Nothing OrElse Not baseType.Allowed OrElse baseType.Kind = FileKind.Unknown Then
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Return baseType
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

        Private Shared Function ReadHeader(path As String, sniffBytes As Integer, maxBytes As Long) As Byte()
            Try
                Dim fi As New FileInfo(path)
                If fi.Length <= 0 OrElse fi.Length > maxBytes Then Return Array.Empty(Of Byte)()

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

                    If off <= 0 Then Return Array.Empty(Of Byte)()
                    If off < take Then
                        Dim exact(off - 1) As Byte
                        Array.Copy(buf, exact, off)
                        Return exact
                    End If
                End Using

                Return buf
            Catch
                Return Array.Empty(Of Byte)()
            End Try
        End Function

        ''' <summary>
        ''' SSOT fuer bekannte Magic-Bytes.
        ''' Erweiterungen duerfen nur hier gepflegt werden.
        ''' </summary>
        Private Shared Function MagicDetect(d As Byte()) As FileKind
            If HasPrefix(d, &H25, &H50, &H44, &H46, &H2D) Then
                Return FileKind.Pdf
            End If

            If HasPrefix(d, &HFF, &HD8, &HFF) Then
                Return FileKind.Jpeg
            End If

            If HasPrefix(d, &H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA) Then
                Return FileKind.Png
            End If

            If HasAscii(d, 0, "GIF87a") OrElse HasAscii(d, 0, "GIF89a") Then
                Return FileKind.Gif
            End If

            If HasAscii(d, 0, "RIFF") AndAlso HasAscii(d, 8, "WEBP") Then
                Return FileKind.Webp
            End If

            If HasPrefix(d, &H50, &H4B, &H3, &H4) OrElse
               HasPrefix(d, &H50, &H4B, &H5, &H6) OrElse
               HasPrefix(d, &H50, &H4B, &H7, &H8) Then
                Return FileKind.Zip
            End If

            Return FileKind.Unknown
        End Function

        Private Shared Function HasPrefix(data As Byte(), ParamArray prefix As Byte()) As Boolean
            If data Is Nothing OrElse prefix Is Nothing Then Return False
            If data.Length < prefix.Length Then Return False

            For i As Integer = 0 To prefix.Length - 1
                If data(i) <> prefix(i) Then Return False
            Next

            Return True
        End Function

        Private Shared Function HasAscii(data As Byte(), offset As Integer, token As String) As Boolean
            If data Is Nothing OrElse String.IsNullOrEmpty(token) Then Return False
            If offset < 0 Then Return False
            If data.Length < offset + token.Length Then Return False

            For i As Integer = 0 To token.Length - 1
                If data(offset + i) <> AscW(token(i)) Then Return False
            Next

            Return True
        End Function

    End Class

End Namespace
