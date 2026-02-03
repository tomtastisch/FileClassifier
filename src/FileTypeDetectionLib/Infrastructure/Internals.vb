Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports HeyRed.Mime
Imports Microsoft.Extensions.Logging
Imports Microsoft.IO

Namespace FileTypeDetection

    ''' <summary>
    ''' Zentrale IO-Helfer fuer harte Grenzen.
    ''' SSOT-Regel: bounded copy wird nur hier gepflegt.
    ''' </summary>
    Friend NotInheritable Class StreamBounds
        Private Sub New()
        End Sub

        Friend Shared Sub CopyBounded(input As Stream, output As Stream, maxBytes As Long)
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
    End Class

    ''' <summary>
    ''' Adapter fuer externe Content-Sniffer.
    ''' Die finale Entscheidung bleibt in FileTypeDetector (SSOT + fail-closed).
    ''' </summary>
    Friend Interface IContentSniffer
        Function SniffAlias(data As Byte(), sniffBytes As Integer, log As ILogger) As String
    End Interface

    ''' <summary>
    ''' Sniffer-Implementierung auf Basis von libmagic (MimeGuesser).
    '''
    ''' Regelwerk:
    ''' - Keine eigenen Magic-Signaturen anlegen.
    ''' - Ausschliesslich Aliaswert liefern; bei Fehlern Nothing.
    ''' </summary>
    Friend NotInheritable Class LibMagicSniffer
        Implements IContentSniffer

        Public Function SniffAlias(data As Byte(), sniffBytes As Integer, log As ILogger) As String Implements IContentSniffer.SniffAlias
            If data Is Nothing OrElse data.Length = 0 Then Return Nothing

            Dim count As Integer = Math.Min(data.Length, sniffBytes)
            Try
                Using ms As New MemoryStream(data, 0, count, writable:=False, publiclyVisible:=False)
                    Dim ext As String = MimeGuesser.GuessExtension(ms)
                    If String.IsNullOrWhiteSpace(ext) Then Return Nothing
                    Return FileTypeRegistry.NormalizeAlias(ext)
                End Using
            Catch ex As Exception
                LogGuard.Debug(log, $"[Sniffer] Fehler: {ex.Message}")
                Return Nothing
            End Try
        End Function

    End Class

    ''' <summary>
    ''' Sicherheits-Gate fuer ZIP-Container.
    '''
    ''' Sicherheitsgrenzen:
    ''' - zu viele Entries
    ''' - zu grosse unkomprimierte Datenmengen
    ''' - hohe Kompressionsraten
    ''' - tiefe/nicht begrenzte ZIP-Verschachtelung
    ''' </summary>
    Friend NotInheritable Class ZipSafetyGate
        Private Shared ReadOnly _recyclableStreams As New RecyclableMemoryStreamManager()

        Private Sub New()
        End Sub

        Friend Shared Function IsZipSafeBytes(data As Byte(), opt As FileTypeDetectorOptions) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return IsZipSafeStream(ms, opt, depth:=0)
                End Using
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipGate] Bytes-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Friend Shared Function IsZipSafeStream(stream As Stream, opt As FileTypeDetectorOptions, depth As Integer) As Boolean
            If stream Is Nothing OrElse Not stream.CanRead Then Return False
            If depth > opt.MaxZipNestingDepth Then Return False
            Return ProcessZipStream(stream, opt, depth, Nothing)
        End Function

        Friend Shared Function TryExtractZipStream(stream As Stream, destinationDirectory As String, opt As FileTypeDetectorOptions) As Boolean
            If stream Is Nothing OrElse Not stream.CanRead Then Return False
            If String.IsNullOrWhiteSpace(destinationDirectory) Then Return False

            Dim destinationFull As String
            Try
                destinationFull = Path.GetFullPath(destinationDirectory)
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipExtract] Ungueltiger Zielpfad: {ex.Message}")
                Return False
            End Try

            If Directory.Exists(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[ZipExtract] Zielordner existiert bereits.")
                Return False
            End If

            Dim parent = Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then
                LogGuard.Warn(opt.Logger, "[ZipExtract] Zielordner ohne gueltigen Parent.")
                Return False
            End If

            Dim stageDir = destinationFull & ".stage-" & Guid.NewGuid().ToString("N")
            Try
                Directory.CreateDirectory(parent)
                Directory.CreateDirectory(stageDir)

                If stream.CanSeek Then stream.Position = 0

                Dim stagePrefix = EnsureTrailingSeparator(Path.GetFullPath(stageDir))
                Dim ok = ProcessZipStream(
                    stream,
                    opt,
                    depth:=0,
                    extractEntry:=Function(entry)
                                      Return ExtractEntryToDirectory(entry, stagePrefix, opt)
                                  End Function)
                If Not ok Then Return False

                Directory.Move(stageDir, destinationFull)
                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipExtract] Fehler: {ex.Message}")
                Return False
            Finally
                If Directory.Exists(stageDir) Then
                    Try
                        Directory.Delete(stageDir, recursive:=True)
                    Catch
                    End Try
                End If
            End Try
        End Function

        Private Shared Function ProcessZipStream(
            stream As Stream,
            opt As FileTypeDetectorOptions,
            depth As Integer,
            extractEntry As Func(Of ZipArchiveEntry, Boolean)
        ) As Boolean
            If stream Is Nothing OrElse Not stream.CanRead Then Return False
            If depth > opt.MaxZipNestingDepth Then Return False

            Try
                If stream.CanSeek Then stream.Position = 0

                Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                    If zip.Entries.Count > opt.MaxZipEntries Then Return False

                    Dim totalUncompressed As Long = 0
                    Dim ordered = zip.Entries.OrderBy(Function(e) If(e.FullName, String.Empty), StringComparer.Ordinal)

                    For Each e In ordered
                        Dim u As Long = e.Length
                        Dim c As Long = e.CompressedLength

                        If u > opt.MaxZipEntryUncompressedBytes Then Return False

                        totalUncompressed += u
                        If totalUncompressed > opt.MaxZipTotalUncompressedBytes Then Return False

                        If c > 0 AndAlso opt.MaxZipCompressionRatio > 0 Then
                            Dim ratio As Double = CDbl(u) / CDbl(c)
                            If ratio > opt.MaxZipCompressionRatio Then Return False
                        End If

                        Dim name = If(e.FullName, String.Empty)
                        If name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) Then
                            If depth >= opt.MaxZipNestingDepth Then Return False
                            If u <= 0 OrElse u > opt.MaxZipNestedBytes Then Return False

                            Try
                                Using es = e.Open()
                                    Using nestedMs = _recyclableStreams.GetStream("ZipSafetyGate.Nested")
                                        StreamBounds.CopyBounded(es, nestedMs, opt.MaxZipNestedBytes)
                                        nestedMs.Position = 0

                                        If Not ProcessZipStream(nestedMs, opt, depth + 1, Nothing) Then
                                            Return False
                                        End If
                                    End Using
                                End Using
                            Catch ex As Exception
                                LogGuard.Debug(opt.Logger, $"[ZipGate] Nested-Fehler: {ex.Message}")
                                Return False
                            End Try
                        End If

                        If depth = 0 AndAlso extractEntry IsNot Nothing Then
                            If Not extractEntry(e) Then Return False
                        End If
                    Next
                End Using

                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipGate] Stream-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function ExtractEntryToDirectory(entry As ZipArchiveEntry, destinationPrefix As String, opt As FileTypeDetectorOptions) As Boolean
            If entry Is Nothing Then Return False

            Dim entryName = NormalizeEntryName(If(entry.FullName, String.Empty))
            If String.IsNullOrWhiteSpace(entryName) Then Return False

            Dim targetPath As String
            Try
                targetPath = Path.GetFullPath(Path.Combine(destinationPrefix, entryName))
            Catch
                Return False
            End Try

            If Not targetPath.StartsWith(destinationPrefix, StringComparison.Ordinal) Then
                LogGuard.Warn(opt.Logger, "[ZipExtract] Path traversal erkannt.")
                Return False
            End If

            If entryName.EndsWith("/", StringComparison.Ordinal) Then
                Directory.CreateDirectory(targetPath)
                Return True
            End If

            Dim targetDir = Path.GetDirectoryName(targetPath)
            If String.IsNullOrWhiteSpace(targetDir) Then Return False
            Directory.CreateDirectory(targetDir)

            If File.Exists(targetPath) OrElse Directory.Exists(targetPath) Then
                LogGuard.Warn(opt.Logger, "[ZipExtract] Kollision bei Zielpfad.")
                Return False
            End If

            Try
                Using source = entry.Open()
                    Using target As New FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.SequentialScan)
                        StreamBounds.CopyBounded(source, target, opt.MaxZipEntryUncompressedBytes)
                    End Using
                End Using
                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipExtract] Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function NormalizeEntryName(entryName As String) As String
            Dim normalized = If(entryName, String.Empty).Replace("\"c, "/"c)
            normalized = normalized.TrimStart("/"c)
            Return normalized
        End Function

        Private Shared Function EnsureTrailingSeparator(dirPath As String) As String
            If String.IsNullOrEmpty(dirPath) Then Return System.IO.Path.DirectorySeparatorChar
            If dirPath.EndsWith(System.IO.Path.DirectorySeparatorChar) OrElse dirPath.EndsWith(System.IO.Path.AltDirectorySeparatorChar) Then
                Return dirPath
            End If
            Return dirPath & System.IO.Path.DirectorySeparatorChar
        End Function

    End Class

    ''' <summary>
    ''' Verfeinert ZIP-Dateien zu OOXML-Typen anhand kanonischer Paket-Pfade.
    '''
    ''' Implementationsprinzip:
    ''' - reduziert False-Positives bei generischen ZIP-Dateien
    ''' - bleibt fail-closed (Fehler => Unknown)
    ''' </summary>
    Friend NotInheritable Class OpenXmlRefiner
        Private Sub New()
        End Sub

        Friend Shared Function TryRefine(streamFactory As Func(Of Stream)) As FileType
            If streamFactory Is Nothing Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                Using s = streamFactory()
                    Return TryRefineStream(s)
                End Using
            Catch
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Friend Shared Function TryRefineStream(stream As Stream) As FileType
            If stream Is Nothing OrElse Not stream.CanRead Then
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End If

            Try
                If stream.CanSeek Then stream.Position = 0
                Return DetectKindFromZip(stream)
            Catch
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Private Shared Function DetectKindFromZip(stream As Stream) As FileType
            Try
                Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                    Dim hasContentTypes As Boolean = False
                    Dim hasDocxMarker As Boolean = False
                    Dim hasXlsxMarker As Boolean = False
                    Dim hasPptxMarker As Boolean = False

                    For Each entry In zip.Entries
                        Dim name = If(entry.FullName, String.Empty)

                        If String.Equals(name, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase) Then
                            hasContentTypes = True
                        End If

                        If String.Equals(name, "word/document.xml", StringComparison.OrdinalIgnoreCase) Then
                            hasDocxMarker = True
                        ElseIf String.Equals(name, "xl/workbook.xml", StringComparison.OrdinalIgnoreCase) Then
                            hasXlsxMarker = True
                        ElseIf String.Equals(name, "ppt/presentation.xml", StringComparison.OrdinalIgnoreCase) Then
                            hasPptxMarker = True
                        End If

                    Next

                    If hasContentTypes Then
                        If hasDocxMarker Then Return FileTypeRegistry.Resolve(FileKind.Docx)
                        If hasXlsxMarker Then Return FileTypeRegistry.Resolve(FileKind.Xlsx)
                        If hasPptxMarker Then Return FileTypeRegistry.Resolve(FileKind.Pptx)
                    End If
                End Using
            Catch
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try

            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

    End Class

    ''' <summary>
    ''' Defensiver Logger-Schutz.
    ''' Logging darf niemals zu Erkennungsfehlern oder Exceptions fuehren.
    ''' </summary>
    Friend NotInheritable Class LogGuard
        Private Sub New()
        End Sub

        Friend Shared Sub Debug(logger As ILogger, message As String)
            If logger Is Nothing Then Return
            Try
                logger.LogDebug(message)
            Catch
            End Try
        End Sub

        Friend Shared Sub Warn(logger As ILogger, message As String)
            If logger Is Nothing Then Return
            Try
                logger.LogWarning(message)
            Catch
            End Try
        End Sub

        Friend Shared Sub [Error](logger As ILogger, message As String, ex As Exception)
            If logger Is Nothing Then Return
            Try
                logger.LogError(ex, message)
            Catch
            End Try
        End Sub
    End Class

End Namespace
