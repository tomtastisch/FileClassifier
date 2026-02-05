Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports Microsoft.IO

Namespace FileTypeDetection

    ''' <summary>
    ''' Zentrale SSOT-Engine fuer ZIP-Verarbeitung.
    ''' Eine Iterationslogik fuer Validierung und sichere Extraktion.
    ''' </summary>
    Friend NotInheritable Class ZipProcessingEngine
        Private Shared ReadOnly _recyclableStreams As New RecyclableMemoryStreamManager()

        Private Sub New()
        End Sub

        Friend Shared Function ValidateZipStream(stream As Stream, opt As FileTypeDetectorOptions, depth As Integer) As Boolean
            Return ProcessZipStream(stream, opt, depth, Nothing)
        End Function

        Friend Shared Function ProcessZipStream(
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

                        If IsNestedZipEntry(e) Then
                            If depth >= opt.MaxZipNestingDepth Then Return False
                            If u <= 0 OrElse u > opt.MaxZipNestedBytes Then Return False

                            Try
                                Using es = e.Open()
                                    Using nestedMs = _recyclableStreams.GetStream("ZipProcessingEngine.Nested")
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

        Private Shared Function IsNestedZipEntry(entry As ZipArchiveEntry) As Boolean
            If entry Is Nothing Then Return False

            Try
                Using entryStream = entry.Open()
                    Dim header(15) As Byte
                    Dim read = entryStream.Read(header, 0, header.Length)
                    If read < 4 Then Return False

                    If read = header.Length Then
                        Return FileTypeRegistry.DetectByMagic(header) = FileKind.Zip
                    End If

                    Dim exact(read - 1) As Byte
                    Buffer.BlockCopy(header, 0, exact, 0, read)
                    Return FileTypeRegistry.DetectByMagic(exact) = FileKind.Zip
                End Using
            Catch
                Return False
            End Try
        End Function
    End Class

    Friend NotInheritable Class ZipArchiveBackend
        Implements IArchiveBackend

        Public ReadOnly Property ContainerType As ArchiveContainerType Implements IArchiveBackend.ContainerType
            Get
                Return ArchiveContainerType.Zip
            End Get
        End Property

        Public Function Process(
            stream As Stream,
            opt As FileTypeDetectorOptions,
            depth As Integer,
            containerType As ArchiveContainerType,
            extractEntry As Func(Of IArchiveEntryModel, Boolean)
        ) As Boolean Implements IArchiveBackend.Process
            If containerType <> ArchiveContainerType.Zip Then Return False

            If extractEntry Is Nothing Then
                Return ZipProcessingEngine.ProcessZipStream(stream, opt, depth, Nothing)
            End If

            Return ZipProcessingEngine.ProcessZipStream(
                stream,
                opt,
                depth,
                Function(entry)
                    Return extractEntry(New ZipArchiveEntryModel(entry))
                End Function)
        End Function
    End Class

    Friend NotInheritable Class ZipArchiveEntryModel
        Implements IArchiveEntryModel

        Private ReadOnly _entry As ZipArchiveEntry

        Friend Sub New(entry As ZipArchiveEntry)
            _entry = entry
        End Sub

        Public ReadOnly Property RelativePath As String Implements IArchiveEntryModel.RelativePath
            Get
                If _entry Is Nothing Then Return String.Empty
                Return If(_entry.FullName, String.Empty)
            End Get
        End Property

        Public ReadOnly Property IsDirectory As Boolean Implements IArchiveEntryModel.IsDirectory
            Get
                If _entry Is Nothing Then Return False
                Dim name = If(_entry.FullName, String.Empty)
                Return name.EndsWith("/", StringComparison.Ordinal)
            End Get
        End Property

        Public ReadOnly Property UncompressedSize As Long? Implements IArchiveEntryModel.UncompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Return _entry.Length
            End Get
        End Property

        Public ReadOnly Property CompressedSize As Long? Implements IArchiveEntryModel.CompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Return _entry.CompressedLength
            End Get
        End Property

        Public ReadOnly Property LinkTarget As String Implements IArchiveEntryModel.LinkTarget
            Get
                Return String.Empty
            End Get
        End Property

        Public Function OpenStream() As Stream Implements IArchiveEntryModel.OpenStream
            If _entry Is Nothing Then Return Stream.Null
            Return _entry.Open()
        End Function
    End Class

    ''' <summary>
    ''' Sicherer ZIP-Extraktionsadapter auf Basis der zentralen ZIP-SSOT-Engine.
    ''' </summary>
    Friend NotInheritable Class ZipExtractor
        Private Shared ReadOnly _recyclableStreams As New RecyclableMemoryStreamManager()

        Private Sub New()
        End Sub

        Friend Shared Function TryExtractZipStreamToMemory(stream As Stream, opt As FileTypeDetectorOptions) As IReadOnlyList(Of ZipExtractedEntry)
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If stream Is Nothing OrElse Not stream.CanRead Then Return emptyResult

            Dim entries As New List(Of ZipExtractedEntry)()
            Try
                If stream.CanSeek Then stream.Position = 0

                Dim ok = ZipProcessingEngine.ProcessZipStream(
                    stream,
                    opt,
                    depth:=0,
                    extractEntry:=Function(entry)
                                      Return ExtractEntryToMemory(entry, entries, opt)
                                  End Function)
                If Not ok Then
                    entries.Clear()
                    Return emptyResult
                End If

                Return entries.AsReadOnly()
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipExtract] InMemory-Fehler: {ex.Message}")
                entries.Clear()
                Return emptyResult
            End Try
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

            If Not DestinationPathGuard.ValidateNewExtractionTarget(destinationFull, opt) Then
                Return False
            End If

            Dim parent = Path.GetDirectoryName(destinationFull)

            Dim stageDir = destinationFull & ".stage-" & Guid.NewGuid().ToString("N")
            Try
                Directory.CreateDirectory(parent)
                Directory.CreateDirectory(stageDir)

                If stream.CanSeek Then stream.Position = 0

                Dim stagePrefix = EnsureTrailingSeparator(Path.GetFullPath(stageDir))
                Dim ok = ZipProcessingEngine.ProcessZipStream(
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

        Private Shared Function ExtractEntryToDirectory(entry As ZipArchiveEntry, destinationPrefix As String, opt As FileTypeDetectorOptions) As Boolean
            If entry Is Nothing Then Return False

            Dim entryName As String = Nothing
            If Not TryGetSafeEntryName(entry, entryName) Then Return False

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
                    Using target As New FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                        StreamBounds.CopyBounded(source, target, opt.MaxZipEntryUncompressedBytes)
                    End Using
                End Using
                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipExtract] Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function ExtractEntryToMemory(entry As ZipArchiveEntry, entries As List(Of ZipExtractedEntry), opt As FileTypeDetectorOptions) As Boolean
            If entry Is Nothing Then Return False
            If entries Is Nothing Then Return False

            Dim entryName As String = Nothing
            If Not TryGetSafeEntryName(entry, entryName) Then Return False
            If entryName.EndsWith("/", StringComparison.Ordinal) Then Return True

            If entry.Length < 0 OrElse entry.Length > opt.MaxZipEntryUncompressedBytes Then Return False

            Try
                Using source = entry.Open()
                    Using ms = _recyclableStreams.GetStream("ZipExtractor.MemoryEntry")
                        StreamBounds.CopyBounded(source, ms, opt.MaxZipEntryUncompressedBytes)
                        Dim payload As Byte() = Array.Empty(Of Byte)()
                        If ms.Length > 0 Then
                            payload = New Byte(CInt(ms.Length) - 1) {}
                            Buffer.BlockCopy(ms.GetBuffer(), 0, payload, 0, payload.Length)
                        End If
                        entries.Add(New ZipExtractedEntry(entryName, payload))
                    End Using
                End Using
                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ZipExtract] InMemory-Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function TryGetSafeEntryName(entry As ZipArchiveEntry, ByRef safeEntryName As String) As Boolean
            safeEntryName = Nothing
            If entry Is Nothing Then Return False

            Dim entryName = NormalizeEntryName(If(entry.FullName, String.Empty))
            If String.IsNullOrWhiteSpace(entryName) Then Return False
            If Path.IsPathRooted(entryName) Then Return False

            Dim trimmed = entryName.TrimEnd("/"c)
            If trimmed.Length = 0 Then
                safeEntryName = entryName
                Return True
            End If

            Dim segments = trimmed.Split("/"c)
            For Each seg In segments
                If seg.Length = 0 Then Return False
                If seg = "." OrElse seg = ".." Then Return False
            Next

            safeEntryName = entryName
            Return True
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

End Namespace
