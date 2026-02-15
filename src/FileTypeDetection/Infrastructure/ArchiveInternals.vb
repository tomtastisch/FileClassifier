Option Strict On
Option Explicit On

Imports System.IO

Namespace Global.Tomtastisch.FileClassifier
    Friend Enum ArchiveContainerType
        Unknown = 0
        Zip
        Tar
        GZip
        SevenZip
        Rar
    End Enum

    Friend NotInheritable Class ArchiveDescriptor
        Public ReadOnly Property LogicalKind As FileKind
        Public ReadOnly Property ContainerType As ArchiveContainerType
        Public ReadOnly Property ContainerChain As IReadOnlyList(Of ArchiveContainerType)

        Private Sub New(logicalKind As FileKind, containerType As ArchiveContainerType,
                        containerChain As ArchiveContainerType())
            Me.LogicalKind = logicalKind
            Me.ContainerType = containerType
            Dim chain = If(containerChain, Array.Empty(Of ArchiveContainerType)())
            Me.ContainerChain = Array.AsReadOnly(CType(chain.Clone(), ArchiveContainerType()))
        End Sub

        Friend Shared Function UnknownDescriptor() As ArchiveDescriptor
            Return _
                New ArchiveDescriptor(FileKind.Unknown, ArchiveContainerType.Unknown,
                                      Array.Empty(Of ArchiveContainerType)())
        End Function

        Friend Shared Function ForContainerType(containerType As ArchiveContainerType) As ArchiveDescriptor
            If containerType = ArchiveContainerType.Unknown Then Return UnknownDescriptor()
            Return New ArchiveDescriptor(FileKind.Zip, containerType, {containerType})
        End Function

        Friend Function WithChain(chain As ArchiveContainerType()) As ArchiveDescriptor
            Return New ArchiveDescriptor(LogicalKind, ContainerType, chain)
        End Function
    End Class

    Friend Interface IArchiveEntryModel
        ReadOnly Property RelativePath As String
        ReadOnly Property IsDirectory As Boolean
        ReadOnly Property UncompressedSize As Long?
        ReadOnly Property CompressedSize As Long?
        ReadOnly Property LinkTarget As String
        Function OpenStream() As Stream
    End Interface

    Friend Interface IArchiveBackend
        ReadOnly Property ContainerType As ArchiveContainerType

        Function Process(
                         stream As Stream,
                         opt As FileTypeProjectOptions,
                         depth As Integer,
                         containerTypeValue As ArchiveContainerType,
                         extractEntry As Func(Of IArchiveEntryModel, Boolean)
                         ) As Boolean
    End Interface

    Friend NotInheritable Class ArchiveBackendRegistry
        Private Shared ReadOnly _managedArchiveBackend As New ArchiveManagedBackend()
        Private Shared ReadOnly _sharpCompressBackend As New SharpCompressArchiveBackend()

        Private Sub New()
        End Sub

        Friend Shared Function Resolve(containerType As ArchiveContainerType) As IArchiveBackend
            Select Case containerType
                Case ArchiveContainerType.Zip
                    Return _managedArchiveBackend
                Case ArchiveContainerType.Tar, ArchiveContainerType.GZip, ArchiveContainerType.SevenZip,
                    ArchiveContainerType.Rar
                    Return _sharpCompressBackend
                Case Else
                    Return Nothing
            End Select
        End Function
    End Class

    Friend NotInheritable Class ArchiveTypeResolver
        Private Sub New()
        End Sub

        Friend Shared Function TryDescribeBytes(data As Byte(), opt As FileTypeProjectOptions,
                                                ByRef descriptor As ArchiveDescriptor) As Boolean
            descriptor = ArchiveDescriptor.UnknownDescriptor()
            If data Is Nothing OrElse data.Length = 0 Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return TryDescribeStream(ms, opt, descriptor)
                End Using
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveDetect] Byte-Erkennung fehlgeschlagen: {ex.Message}")
                descriptor = ArchiveDescriptor.UnknownDescriptor()
                Return False
            End Try
        End Function

        Friend Shared Function TryDescribeStream(stream As Stream, opt As FileTypeProjectOptions,
                                                 ByRef descriptor As ArchiveDescriptor) As Boolean
            descriptor = ArchiveDescriptor.UnknownDescriptor()
            If Not StreamGuard.IsReadable(stream) Then Return False

            Try
                StreamGuard.RewindToStart(stream)
                Using archive = SharpCompress.Archives.ArchiveFactory.Open(stream)
                    If archive Is Nothing Then Return False

                    Dim mapped = MapArchiveType(archive.Type)
                    If mapped = ArchiveContainerType.Unknown Then Return False

                    descriptor = ArchiveDescriptor.ForContainerType(mapped)
                    Return True
                End Using
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveDetect] Stream-Erkennung fehlgeschlagen: {ex.Message}")
                descriptor = ArchiveDescriptor.UnknownDescriptor()
                Return False
            Finally
                Try
                    StreamGuard.RewindToStart(stream)
                Catch
                End Try
            End Try
        End Function

        Friend Shared Function MapArchiveType(type As SharpCompress.Common.ArchiveType) As ArchiveContainerType
            Select Case type
                Case SharpCompress.Common.ArchiveType.Zip
                    Return ArchiveContainerType.Zip
                Case SharpCompress.Common.ArchiveType.Tar
                    Return ArchiveContainerType.Tar
                Case SharpCompress.Common.ArchiveType.GZip
                    Return ArchiveContainerType.GZip
                Case SharpCompress.Common.ArchiveType.SevenZip
                    Return ArchiveContainerType.SevenZip
                Case SharpCompress.Common.ArchiveType.Rar
                    Return ArchiveContainerType.Rar
                Case Else
                    Return ArchiveContainerType.Unknown
            End Select
        End Function
    End Class

    Friend NotInheritable Class ArchiveProcessingEngine
        Private Sub New()
        End Sub

        Friend Shared Function ValidateArchiveStream(stream As Stream, opt As FileTypeProjectOptions, depth As Integer,
                                                     descriptor As ArchiveDescriptor) As Boolean
            Return ProcessArchiveStream(stream, opt, depth, descriptor, Nothing)
        End Function

        Friend Shared Function ProcessArchiveStream(
                                                    stream As Stream,
                                                    opt As FileTypeProjectOptions,
                                                    depth As Integer,
                                                    descriptor As ArchiveDescriptor,
                                                    extractEntry As Func(Of IArchiveEntryModel, Boolean)
                                                    ) As Boolean
            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False

            Dim backend = ArchiveBackendRegistry.Resolve(descriptor.ContainerType)
            If backend Is Nothing Then Return False
            Return backend.Process(stream, opt, depth, descriptor.ContainerType, extractEntry)
        End Function
    End Class

    Friend NotInheritable Class ArchiveExtractor
        Private Shared ReadOnly _recyclableStreams As New Microsoft.IO.RecyclableMemoryStreamManager()

        Private Sub New()
        End Sub

        Friend Shared Function TryExtractArchiveStreamToMemory(stream As Stream, opt As FileTypeProjectOptions) _
            As IReadOnlyList(Of ZipExtractedEntry)
            Dim descriptor As ArchiveDescriptor = Nothing
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If Not ArchiveTypeResolver.TryDescribeStream(stream, opt, descriptor) Then Return emptyResult
            Return TryExtractArchiveStreamToMemory(stream, opt, descriptor)
        End Function

        Friend Shared Function TryExtractArchiveStreamToMemory(stream As Stream, opt As FileTypeProjectOptions,
                                                               descriptor As ArchiveDescriptor) _
            As IReadOnlyList(Of ZipExtractedEntry)
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If Not StreamGuard.IsReadable(stream) Then Return emptyResult
            If opt Is Nothing Then Return emptyResult
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then _
                Return emptyResult

            Dim entries As New List(Of ZipExtractedEntry)()
            Try
                StreamGuard.RewindToStart(stream)
                Dim ok = ArchiveProcessingEngine.ProcessArchiveStream(
                    stream,
                    opt,
                    depth:=0,
                    descriptor:=descriptor,
                    extractEntry:=Function(entry)
                                      Return ExtractEntryToMemory(entry, entries, opt)
                                  End Function)
                If Not ok Then
                    entries.Clear()
                    Return emptyResult
                End If

                Return entries.AsReadOnly()
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] InMemory-Fehler: {ex.Message}")
                entries.Clear()
                Return emptyResult
            End Try
        End Function

        Friend Shared Function TryExtractArchiveStream(stream As Stream, destinationDirectory As String,
                                                       opt As FileTypeProjectOptions) As Boolean
            Dim descriptor As ArchiveDescriptor = Nothing
            If Not ArchiveTypeResolver.TryDescribeStream(stream, opt, descriptor) Then Return False
            Return TryExtractArchiveStream(stream, destinationDirectory, opt, descriptor)
        End Function

        Friend Shared Function TryExtractArchiveStream(stream As Stream, destinationDirectory As String,
                                                       opt As FileTypeProjectOptions, descriptor As ArchiveDescriptor) _
            As Boolean
            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False
            If String.IsNullOrWhiteSpace(destinationDirectory) Then Return False

            Dim destinationFull As String
            Try
                destinationFull = Path.GetFullPath(destinationDirectory)
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Ungueltiger Zielpfad: {ex.Message}")
                Return False
            End Try

            If Not DestinationPathGuard.ValidateNewExtractionTarget(destinationFull, opt) Then Return False

            Dim parent = Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then Return False

            Dim stageDir = destinationFull & ".stage-" & Guid.NewGuid().ToString("N")
            Try
                Directory.CreateDirectory(parent)
                Directory.CreateDirectory(stageDir)

                StreamGuard.RewindToStart(stream)

                Dim stagePrefix = EnsureTrailingSeparator(Path.GetFullPath(stageDir))
                Dim ok = ArchiveProcessingEngine.ProcessArchiveStream(
                    stream,
                    opt,
                    depth:=0,
                    descriptor:=descriptor,
                    extractEntry:=Function(entry)
                                      Return ExtractEntryToDirectory(entry, stagePrefix, opt)
                                  End Function)
                If Not ok Then Return False

                Directory.Move(stageDir, destinationFull)
                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Fehler: {ex.Message}")
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

        Private Shared Function ExtractEntryToDirectory(entry As IArchiveEntryModel, destinationPrefix As String,
                                                        opt As FileTypeProjectOptions) As Boolean
            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            Dim entryName As String = Nothing
            Dim isDirectory = False
            If Not TryGetSafeEntryName(entry, opt, entryName, isDirectory) Then Return False

            Dim targetPath As String
            Try
                targetPath = Path.GetFullPath(Path.Combine(destinationPrefix, entryName))
            Catch
                Return False
            End Try

            If Not targetPath.StartsWith(destinationPrefix, StringComparison.Ordinal) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Path traversal erkannt.")
                Return False
            End If

            If isDirectory Then
                Directory.CreateDirectory(targetPath)
                Return True
            End If

            If Not ValidateEntrySize(entry, opt) Then Return False

            Dim targetDir = Path.GetDirectoryName(targetPath)
            If String.IsNullOrWhiteSpace(targetDir) Then Return False
            Directory.CreateDirectory(targetDir)

            If File.Exists(targetPath) OrElse Directory.Exists(targetPath) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Kollision bei Zielpfad.")
                Return False
            End If

            Try
                Using source = entry.OpenStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    Using _
                        target As _
                            New FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                           InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                        StreamBounds.CopyBounded(source, target, opt.MaxZipEntryUncompressedBytes)
                    End Using
                End Using
                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function ExtractEntryToMemory(entry As IArchiveEntryModel, entries As List(Of ZipExtractedEntry),
                                                     opt As FileTypeProjectOptions) As Boolean
            If entry Is Nothing OrElse entries Is Nothing Then Return False
            If opt Is Nothing Then Return False

            Dim entryName As String = Nothing
            Dim isDirectory = False
            If Not TryGetSafeEntryName(entry, opt, entryName, isDirectory) Then Return False
            If isDirectory Then Return True

            If Not ValidateEntrySize(entry, opt) Then Return False

            Try
                Using source = entry.OpenStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    Using ms = _recyclableStreams.GetStream("ArchiveExtractor.MemoryEntry")
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
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] InMemory-Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function TryGetSafeEntryName(entry As IArchiveEntryModel, opt As FileTypeProjectOptions,
                                                    ByRef safeEntryName As String, ByRef isDirectory As Boolean) _
            As Boolean
            safeEntryName = Nothing
            isDirectory = False
            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(entry.LinkTarget) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Link-Entry ist nicht erlaubt.")
                Return False
            End If

            Dim entryName As String = Nothing
            Dim normalizedDirectoryFlag = False
            If _
                Not _
                ArchiveEntryPathPolicy.TryNormalizeRelativePath(entry.RelativePath, allowDirectoryMarker:=True,
                                                                entryName, normalizedDirectoryFlag) Then
                Return False
            End If

            safeEntryName = entryName
            isDirectory = entry.IsDirectory OrElse normalizedDirectoryFlag OrElse
                          entryName.EndsWith("/"c)
            Return True
        End Function

        Private Shared Function ValidateEntrySize(entry As IArchiveEntryModel, opt As FileTypeProjectOptions) As Boolean
            If entry Is Nothing OrElse opt Is Nothing Then Return False
            If entry.IsDirectory Then Return True

            Dim sizeValue = entry.UncompressedSize
            If sizeValue.HasValue Then
                If sizeValue.Value < 0 Then
                    Return opt.AllowUnknownArchiveEntrySize
                End If

                If sizeValue.Value > opt.MaxZipEntryUncompressedBytes Then Return False
                Return True
            End If

            Return opt.AllowUnknownArchiveEntrySize
        End Function

        Private Shared Function EnsureTrailingSeparator(dirPath As String) As String
            If String.IsNullOrEmpty(dirPath) Then Return Path.DirectorySeparatorChar.ToString()
            If dirPath.EndsWith(Path.DirectorySeparatorChar) OrElse dirPath.EndsWith(Path.AltDirectorySeparatorChar) _
                Then
                Return dirPath
            End If
            Return dirPath & Path.DirectorySeparatorChar
        End Function
    End Class

    Friend NotInheritable Class ArchiveEntryCollector
        Private Sub New()
        End Sub

        Friend Shared Function TryCollectFromFile(path As String, opt As FileTypeProjectOptions,
                                                  ByRef entries As IReadOnlyList(Of ZipExtractedEntry)) As Boolean
            entries = Array.Empty(Of ZipExtractedEntry)()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False
            If opt Is Nothing Then Return False

            Try
                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    Dim descriptor As ArchiveDescriptor = Nothing
                    If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return False
                    StreamGuard.RewindToStart(fs)
                    If Not ArchiveSafetyGate.IsArchiveSafeStream(fs, opt, descriptor, depth:=0) Then Return False
                    StreamGuard.RewindToStart(fs)
                    entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(fs, opt, descriptor)
                    Return entries IsNot Nothing AndAlso entries.Count > 0
                End Using
            Catch
                entries = Array.Empty(Of ZipExtractedEntry)()
                Return False
            End Try
        End Function

        Friend Shared Function TryCollectFromBytes(data As Byte(), opt As FileTypeProjectOptions,
                                                   ByRef entries As IReadOnlyList(Of ZipExtractedEntry)) As Boolean
            entries = Array.Empty(Of ZipExtractedEntry)()
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False

            Try
                Dim descriptor As ArchiveDescriptor = Nothing
                If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return False
                If Not ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor) Then Return False
                Using ms As New MemoryStream(data, writable:=False)
                    entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(ms, opt, descriptor)
                    Return entries IsNot Nothing AndAlso entries.Count > 0
                End Using
            Catch
                entries = Array.Empty(Of ZipExtractedEntry)()
                Return False
            End Try
        End Function
    End Class

    Friend NotInheritable Class SharpCompressArchiveBackend
        Implements IArchiveBackend

        Public ReadOnly Property ContainerType As ArchiveContainerType Implements IArchiveBackend.ContainerType
            Get
                Return ArchiveContainerType.Unknown
            End Get
        End Property

        Public Function Process(
                                stream As Stream,
                                opt As FileTypeProjectOptions,
                                depth As Integer,
                                containerTypeValue As ArchiveContainerType,
                                extractEntry As Func(Of IArchiveEntryModel, Boolean)
                                ) As Boolean Implements IArchiveBackend.Process
            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If depth > opt.MaxZipNestingDepth Then Return False
            If containerTypeValue = ArchiveContainerType.Unknown Then Return False

            Try
                StreamGuard.RewindToStart(stream)

                Using archive = SharpCompress.Archives.ArchiveFactory.Open(stream)
                    If archive Is Nothing Then Return False
                    Dim mapped = ArchiveTypeResolver.MapArchiveType(archive.Type)
                    If mapped <> containerTypeValue Then Return False

                    Dim entries = archive.Entries.
                            OrderBy(Function(e) If(e.Key, String.Empty), StringComparer.Ordinal).
                            ToList()

                    Dim nestedResult As Boolean
                    Dim nestedHandled = TryProcessNestedGArchive(entries, opt, depth, containerTypeValue, extractEntry,
                                                                 nestedResult)
                    If nestedHandled Then Return nestedResult

                    If entries.Count > opt.MaxZipEntries Then Return False

                    Dim totalUncompressed As Long = 0
                    For Each entry In entries
                        If entry Is Nothing Then Return False
                        If Not entry.IsComplete Then Return False

                        Dim model As IArchiveEntryModel = New SharpCompressEntryModel(entry)

                        If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(model.LinkTarget) Then
                            LogGuard.Warn(opt.Logger, "[ArchiveGate] Link-Entry ist nicht erlaubt.")
                            Return False
                        End If

                        If Not model.IsDirectory Then
                            Dim knownSize As Long = 0
                            Dim requireKnownForTotal = (extractEntry Is Nothing) OrElse depth > 0
                            If Not TryGetValidatedSize(model, opt, knownSize, requireKnownForTotal) Then Return False
                            totalUncompressed += knownSize
                            If totalUncompressed > opt.MaxZipTotalUncompressedBytes Then Return False
                        End If

                        If extractEntry IsNot Nothing Then
                            If Not extractEntry(model) Then Return False
                        End If
                    Next
                End Using

                Return True
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] SharpCompress-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function TryProcessNestedGArchive(
                                                         entries As List(Of SharpCompress.Archives.IArchiveEntry),
                                                         opt As FileTypeProjectOptions,
                                                         depth As Integer,
                                                         containerType As ArchiveContainerType,
                                                         extractEntry As Func(Of IArchiveEntryModel, Boolean),
                                                         ByRef nestedResult As Boolean
                                                         ) As Boolean
            nestedResult = False
            If containerType <> ArchiveContainerType.GZip Then Return False
            If entries Is Nothing OrElse entries.Count <> 1 Then Return False

            Dim onlyEntry = entries(0)
            If onlyEntry Is Nothing OrElse onlyEntry.IsDirectory Then Return False

            Dim model As IArchiveEntryModel = New SharpCompressEntryModel(onlyEntry)
            If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(model.LinkTarget) Then
                nestedResult = False
                Return True
            End If

            Dim payload As Byte() = Nothing
            If Not TryReadEntryPayloadBounded(onlyEntry, opt.MaxZipNestedBytes, payload) Then
                nestedResult = False
                Return True
            End If

            Dim nestedDescriptor As ArchiveDescriptor = Nothing
            If Not ArchiveTypeResolver.TryDescribeBytes(payload, opt, nestedDescriptor) Then
                Return False
            End If

            nestedDescriptor = nestedDescriptor.WithChain({ArchiveContainerType.GZip, nestedDescriptor.ContainerType})

            If depth >= opt.MaxZipNestingDepth Then
                nestedResult = False
                Return True
            End If

            Using nestedMs As New MemoryStream(payload, writable:=False)
                nestedResult = ArchiveProcessingEngine.ProcessArchiveStream(nestedMs, opt, depth + 1, nestedDescriptor,
                                                                            extractEntry)
            End Using
            Return True
        End Function

        Private Shared Function TryReadEntryPayloadBounded(entry As SharpCompress.Archives.IArchiveEntry, maxBytes As Long,
                                                            ByRef payload As Byte()) As Boolean
            payload = Array.Empty(Of Byte)()
            If entry Is Nothing Then Return False
            If maxBytes <= 0 Then Return False

            Try
                Using source = entry.OpenEntryStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    Using ms As New MemoryStream()
                        StreamBounds.CopyBounded(source, ms, maxBytes)
                        payload = ms.ToArray()
                        Return True
                    End Using
                End Using
            Catch
                payload = Array.Empty(Of Byte)()
                Return False
            End Try
        End Function

        Private Shared Function TryGetValidatedSize(entry As IArchiveEntryModel, opt As FileTypeProjectOptions,
                                                    ByRef knownSize As Long, requireKnownForTotal As Boolean) As Boolean
            knownSize = 0
            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            Dim value = entry.UncompressedSize
            If value.HasValue Then
                If value.Value < 0 Then
                    If Not requireKnownForTotal Then Return True
                    Return TryMeasureEntrySize(entry, opt, knownSize)
                End If

                If value.Value > opt.MaxZipEntryUncompressedBytes Then Return False
                knownSize = value.Value
                Return True
            End If

            If Not requireKnownForTotal Then Return True
            Return TryMeasureEntrySize(entry, opt, knownSize)
        End Function

        Private Shared Function TryMeasureEntrySize(entry As IArchiveEntryModel, opt As FileTypeProjectOptions,
                                                    ByRef measured As Long) As Boolean
            measured = 0
            If entry Is Nothing OrElse opt Is Nothing Then Return False

            If opt.AllowUnknownArchiveEntrySize Then Return True

            Try
                Using source = entry.OpenStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    Dim buf(InternalIoDefaults.CopyBufferSize - 1) As Byte
                    While True
                        Dim n = source.Read(buf, 0, buf.Length)
                        If n <= 0 Then Exit While
                        measured += n
                        If measured > opt.MaxZipEntryUncompressedBytes Then Return False
                    End While
                End Using
                Return True
            Catch
                Return False
            End Try
        End Function
    End Class

    Friend NotInheritable Class SharpCompressEntryModel
        Implements IArchiveEntryModel

        Private ReadOnly _entry As SharpCompress.Archives.IArchiveEntry

        Friend Sub New(entry As SharpCompress.Archives.IArchiveEntry)
            _entry = entry
        End Sub

        Public ReadOnly Property RelativePath As String Implements IArchiveEntryModel.RelativePath
            Get
                If _entry Is Nothing Then Return String.Empty
                Return If(_entry.Key, String.Empty)
            End Get
        End Property

        Public ReadOnly Property IsDirectory As Boolean Implements IArchiveEntryModel.IsDirectory
            Get
                Return _entry IsNot Nothing AndAlso _entry.IsDirectory
            End Get
        End Property

        Public ReadOnly Property UncompressedSize As Long? Implements IArchiveEntryModel.UncompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Dim size = _entry.Size
                If size < 0 Then Return Nothing
                Return size
            End Get
        End Property

        Public ReadOnly Property CompressedSize As Long? Implements IArchiveEntryModel.CompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Dim size = _entry.CompressedSize
                If size < 0 Then Return Nothing
                Return size
            End Get
        End Property

        Public ReadOnly Property LinkTarget As String Implements IArchiveEntryModel.LinkTarget
            Get
                If _entry Is Nothing Then Return String.Empty
                Return If(_entry.LinkTarget, String.Empty)
            End Get
        End Property

        Public Function OpenStream() As Stream Implements IArchiveEntryModel.OpenStream
            If _entry Is Nothing Then Return Stream.Null
            Return _entry.OpenEntryStream()
        End Function
    End Class
End Namespace
