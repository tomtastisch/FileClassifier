' ============================================================================
' FILE: ArchiveInternals.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.IO
Imports System.Reflection
Imports System.Security

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Interne Aufzählung <c>ArchiveContainerType</c> für deterministische Zustands- und Typkennzeichnung.
    ''' </summary>
    Friend Enum ArchiveContainerType
        Unknown = 0
        Zip
        Tar
        GZip
        SevenZip
        Rar
    End Enum

    ''' <summary>
    '''     Unveränderlicher Deskriptor zur Beschreibung erkannter Archivtypen und Containerketten.
    '''     Enthält Metadaten zu <see cref="LogicalKind"/>, <see cref="ContainerType"/> und <see cref="ContainerChain"/>.
    ''' </summary>
    Friend NotInheritable Class ArchiveDescriptor
        ''' <summary>
        '''     Logischer Dateityp des erkannten Archivcontainers.
        ''' </summary>
        Public ReadOnly Property LogicalKind As FileKind

        ''' <summary>
        '''     Primärer physischer Containertyp der Erkennung.
        ''' </summary>
        Public ReadOnly Property ContainerType As ArchiveContainerType

        ''' <summary>
        '''     Deterministische Containerkette für verschachtelte Formate.
        ''' </summary>
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

    ''' <summary>
    '''     Interner Vertrag <c>IArchiveEntryModel</c> für austauschbare Infrastrukturkomponenten.
    ''' </summary>
    Friend Interface IArchiveEntryModel
        ReadOnly Property RelativePath As String
        ReadOnly Property IsDirectory As Boolean
        ReadOnly Property UncompressedSize As Long?
        ReadOnly Property CompressedSize As Long?
        ReadOnly Property LinkTarget As String
        Function OpenStream() As Stream
    End Interface

    ''' <summary>
    '''     Interner Vertrag <c>IArchiveBackend</c> für austauschbare Infrastrukturkomponenten.
    ''' </summary>
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

    ''' <summary>
    '''     Interne Hilfsklasse <c>ArchiveBackendRegistry</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class ArchiveBackendRegistry
        Private Shared ReadOnly ManagedArchiveBackend As New ArchiveManagedBackend()
        Private Shared ReadOnly SharpCompressBackend As New SharpCompressArchiveBackend()

        Private Sub New()
        End Sub

        Friend Shared Function Resolve(containerType As ArchiveContainerType) As IArchiveBackend
            Select Case containerType
                Case ArchiveContainerType.Zip
                    Return ManagedArchiveBackend
                Case ArchiveContainerType.Tar, ArchiveContainerType.GZip, ArchiveContainerType.SevenZip,
                    ArchiveContainerType.Rar
                    Return SharpCompressBackend
                Case Else
                    Return Nothing
            End Select
        End Function
    End Class

    Friend NotInheritable Class ArchiveSharpCompressCompat
        Private Sub New()
        End Sub

        Friend Shared Function OpenArchive(stream As Stream) As SharpCompress.Archives.IArchive
            Try
                Dim options = New SharpCompress.Readers.ReaderOptions() With {.LeaveStreamOpen = True}
                Return OpenArchiveFactoryCompat(stream, options)
            Catch ex As Exception When _
                TypeOf ex Is MissingMethodException OrElse
                TypeOf ex Is System.Reflection.TargetInvocationException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return Nothing
            End Try
        End Function

        Friend Shared Function OpenArchiveForContainer(stream As Stream,
                                                       containerTypeValue As ArchiveContainerType) _
            As SharpCompress.Archives.IArchive
            If containerTypeValue = ArchiveContainerType.GZip Then
                Dim gzipArchive = OpenGZipArchive(stream)
                If gzipArchive IsNot Nothing Then Return gzipArchive
            End If
            Return OpenArchive(stream)
        End Function

        Friend Shared Function HasGZipMagic(stream As Stream) As Boolean
            If stream Is Nothing OrElse Not stream.CanRead Then Return False
            If Not stream.CanSeek Then Return False
            If stream.Length < 2 Then Return False

            Dim first = stream.ReadByte()
            Dim second = stream.ReadByte()
            Return first = &H1F AndAlso second = &H8B
        End Function

        Private Shared Function OpenGZipArchive(stream As Stream) As SharpCompress.Archives.IArchive
            Try
                Dim options = New SharpCompress.Readers.ReaderOptions() With {.LeaveStreamOpen = True}
                Return OpenGZipArchiveCompat(stream, options)
            Catch ex As Exception When _
                TypeOf ex Is MissingMethodException OrElse
                TypeOf ex Is System.Reflection.TargetInvocationException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return Nothing
            End Try
        End Function

        Private Shared Function OpenArchiveFactoryCompat(
                                                       stream As Stream,
                                                       options As SharpCompress.Readers.ReaderOptions
                                                       ) As SharpCompress.Archives.IArchive
            Dim method = GetOpenCompatMethod(GetType(SharpCompress.Archives.ArchiveFactory))
            Dim opened = method.Invoke(Nothing, New Object() {stream, options})
            Return CType(opened, SharpCompress.Archives.IArchive)
        End Function

        Private Shared Function OpenGZipArchiveCompat(
                                                    stream As Stream,
                                                    options As SharpCompress.Readers.ReaderOptions
                                                    ) As SharpCompress.Archives.IArchive
            Dim method = GetOpenCompatMethod(GetType(SharpCompress.Archives.GZip.GZipArchive))
            Dim opened = method.Invoke(Nothing, New Object() {stream, options})
            Return CType(opened, SharpCompress.Archives.IArchive)
        End Function

        Private Shared Function GetOpenCompatMethod(type As Type) As System.Reflection.MethodInfo
            Dim signature = New Type() {GetType(Stream), GetType(SharpCompress.Readers.ReaderOptions)}
            Dim method = type.GetMethod("OpenArchive", BindingFlags.Public Or
                                                       BindingFlags.Static,
                                        binder:=Nothing,
                                        types:=signature,
                                        modifiers:=Nothing)
            If method IsNot Nothing Then Return method

            method = type.GetMethod("Open", BindingFlags.Public Or
                                            BindingFlags.Static,
                                    binder:=Nothing,
                                    types:=signature,
                                    modifiers:=Nothing)
            If method IsNot Nothing Then Return method

            Throw New MissingMethodException(type.FullName, "OpenArchive/Open(Stream, ReaderOptions)")
        End Function
    End Class

    ''' <summary>
    '''     Interne Hilfsklasse <c>ArchiveTypeResolver</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class ArchiveTypeResolver
        Private Sub New()
        End Sub

        Friend Shared Function TryDescribeBytes(data As Byte(), opt As FileTypeProjectOptions,
                                                ByRef descriptor As ArchiveDescriptor) As Boolean
            descriptor = ArchiveDescriptor.UnknownDescriptor()
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return TryDescribeStream(ms, opt, descriptor)
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveDetect] Byte-Erkennung fehlgeschlagen: {ex.Message}")
                descriptor = ArchiveDescriptor.UnknownDescriptor()
                Return False
            End Try
        End Function

        Friend Shared Function TryDescribeStream(stream As Stream, opt As FileTypeProjectOptions,
                                                 ByRef descriptor As ArchiveDescriptor) As Boolean
            Dim mapped As ArchiveContainerType
            Dim gzipWrapped As Boolean

            descriptor = ArchiveDescriptor.UnknownDescriptor()
            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False

            Try
                StreamGuard.RewindToStart(stream)
                gzipWrapped = ArchiveSharpCompressCompat.HasGZipMagic(stream)
                StreamGuard.RewindToStart(stream)
                Using archive = ArchiveSharpCompressCompat.OpenArchive(stream)
                    If archive Is Nothing Then Return False

                    mapped = MapArchiveType(archive.Type)
                    If gzipWrapped AndAlso mapped = ArchiveContainerType.Tar Then
                        mapped = ArchiveContainerType.GZip
                    End If
                    If mapped = ArchiveContainerType.Unknown Then Return False

                    descriptor = ArchiveDescriptor.ForContainerType(mapped)
                    Return True
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveDetect] Stream-Erkennung fehlgeschlagen: {ex.Message}")
                descriptor = ArchiveDescriptor.UnknownDescriptor()
                Return False
            Finally
                Try
                    StreamGuard.RewindToStart(stream)
                Catch ex As Exception When _
                    TypeOf ex Is UnauthorizedAccessException OrElse
                    TypeOf ex Is SecurityException OrElse
                    TypeOf ex Is IOException OrElse
                    TypeOf ex Is NotSupportedException OrElse
                    TypeOf ex Is ArgumentException OrElse
                    TypeOf ex Is InvalidOperationException OrElse
                    TypeOf ex Is ObjectDisposedException
                    LogGuard.Debug(opt.Logger, $"[ArchiveDetect] Rewind fehlgeschlagen: {ex.Message}")
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

    ''' <summary>
    '''     Interne Hilfsklasse <c>ArchiveProcessingEngine</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
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
            Dim backend As IArchiveBackend

            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False

            backend = ArchiveBackendRegistry.Resolve(descriptor.ContainerType)
            If backend Is Nothing Then Return False
            Return backend.Process(stream, opt, depth, descriptor.ContainerType, extractEntry)
        End Function

    End Class

    ''' <summary>
    '''     Interne Hilfsklasse <c>ArchiveExtractor</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class ArchiveExtractor
        Private Shared ReadOnly RecyclableStreams As New Microsoft.IO.RecyclableMemoryStreamManager()

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
            Dim entries As List(Of ZipExtractedEntry) = New List(Of ZipExtractedEntry)()
            Dim ok As Boolean

            If Not StreamGuard.IsReadable(stream) Then Return emptyResult
            If opt Is Nothing Then Return emptyResult
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then _
                Return emptyResult

            Try
                StreamGuard.RewindToStart(stream)
                ok = ArchiveProcessingEngine.ProcessArchiveStream(
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
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                    TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
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
            Dim destinationFull As String
            Dim parent As String
            Dim stageDir As String
            Dim stagePrefix As String
            Dim ok As Boolean

            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False
            If String.IsNullOrWhiteSpace(destinationDirectory) Then Return False

            Try
                destinationFull = Path.GetFullPath(destinationDirectory)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                    TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is PathTooLongException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Ungültiger Zielpfad: {ex.Message}")
                Return False
            End Try

            If Not DestinationPathGuard.ValidateNewExtractionTarget(destinationFull, opt) Then Return False

            parent = Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then Return False

            stageDir = destinationFull & ".stage-" & Guid.NewGuid().ToString("N")
            Try
                Directory.CreateDirectory(parent)
                Directory.CreateDirectory(stageDir)

                StreamGuard.RewindToStart(stream)

                stagePrefix = EnsureTrailingSeparator(Path.GetFullPath(stageDir))
                ok = ArchiveProcessingEngine.ProcessArchiveStream(
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
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Fehler: {ex.Message}")
                Return False
            Finally
                If Directory.Exists(stageDir) Then
                    Try
                        Directory.Delete(stageDir, recursive:=True)
                    Catch ex As Exception When _
                        TypeOf ex Is UnauthorizedAccessException OrElse
                        TypeOf ex Is SecurityException OrElse
                        TypeOf ex Is IOException OrElse
                        TypeOf ex Is NotSupportedException OrElse
                        TypeOf ex Is ArgumentException
                        LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Cleanup-Fehler: {ex.Message}")
                    End Try
                End If
            End Try
        End Function

        Private Shared Function ExtractEntryToDirectory(entry As IArchiveEntryModel, destinationPrefix As String,
                                                        opt As FileTypeProjectOptions) As Boolean
            Dim entryName As String = Nothing
            Dim isDirectory As Boolean = False
            Dim targetPath As String
            Dim targetDir As String

            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            If Not TryGetSafeEntryName(entry, opt, entryName, isDirectory) Then Return False

            Try
                targetPath = Path.GetFullPath(Path.Combine(destinationPrefix, entryName))
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is PathTooLongException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Zielpfad-Fehler: {ex.Message}")
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

            targetDir = Path.GetDirectoryName(targetPath)
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
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function ExtractEntryToMemory(entry As IArchiveEntryModel, entries As List(Of ZipExtractedEntry),
                                                     opt As FileTypeProjectOptions) As Boolean
            Dim entryName As String = Nothing
            Dim isDirectory As Boolean = False
            Dim payload As Byte()

            If entry Is Nothing OrElse entries Is Nothing Then Return False
            If opt Is Nothing Then Return False

            If Not TryGetSafeEntryName(entry, opt, entryName, isDirectory) Then Return False
            If isDirectory Then Return True

            If Not ValidateEntrySize(entry, opt) Then Return False

            Try
                Using source = entry.OpenStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    Using ms = RecyclableStreams.GetStream("ArchiveExtractor.MemoryEntry")
                        StreamBounds.CopyBounded(source, ms, opt.MaxZipEntryUncompressedBytes)
                        payload = Array.Empty(Of Byte)()
                        If ms.Length > 0 Then
                            payload = New Byte(CInt(ms.Length) - 1) {}
                            Buffer.BlockCopy(ms.GetBuffer(), 0, payload, 0, payload.Length)
                        End If
                        entries.Add(New ZipExtractedEntry(entryName, payload))
                    End Using
                End Using
                Return True
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveExtract] InMemory-Entry-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function TryGetSafeEntryName(entry As IArchiveEntryModel, opt As FileTypeProjectOptions,
                                                    ByRef safeEntryName As String, ByRef isDirectory As Boolean) _
            As Boolean
            Dim entryName As String = Nothing
            Dim normalizedDirectoryFlag As Boolean = False

            safeEntryName = Nothing
            isDirectory = False
            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(entry.LinkTarget) Then
                LogGuard.Warn(opt.Logger, "[ArchiveExtract] Link-Entry ist nicht erlaubt.")
                Return False
            End If

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
            Dim sizeValue As Long?

            If entry Is Nothing OrElse opt Is Nothing Then Return False
            If entry.IsDirectory Then Return True

            sizeValue = entry.UncompressedSize
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

    ''' <summary>
    '''     Interne Hilfsklasse <c>ArchiveEntryCollector</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class ArchiveEntryCollector
        Private Sub New()
        End Sub

        Friend Shared Function TryCollectFromFile(path As String, opt As FileTypeProjectOptions,
                                                  ByRef entries As IReadOnlyList(Of ZipExtractedEntry)) As Boolean
            Dim descriptor As ArchiveDescriptor = Nothing

            entries = Array.Empty(Of ZipExtractedEntry)()
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False
            If opt Is Nothing Then Return False

            Try
                Using _
                    fs As _
                        New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    If Not ArchiveTypeResolver.TryDescribeStream(fs, opt, descriptor) Then Return False
                    StreamGuard.RewindToStart(fs)
                    If Not ArchiveSafetyGate.IsArchiveSafeStream(fs, opt, descriptor, depth:=0) Then Return False
                    StreamGuard.RewindToStart(fs)
                    entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(fs, opt, descriptor)
                    Return entries IsNot Nothing AndAlso entries.Count > 0
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveCollect] Datei-Fehler: {ex.Message}")
                entries = Array.Empty(Of ZipExtractedEntry)()
                Return False
            End Try
        End Function

        Friend Shared Function TryCollectFromBytes(data As Byte(), opt As FileTypeProjectOptions,
                                                   ByRef entries As IReadOnlyList(Of ZipExtractedEntry)) As Boolean
            Dim descriptor As ArchiveDescriptor = Nothing

            entries = Array.Empty(Of ZipExtractedEntry)()
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False

            Try
                If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return False
                If Not ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor) Then Return False
                Using ms As New MemoryStream(data, writable:=False)
                    entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(ms, opt, descriptor)
                    Return entries IsNot Nothing AndAlso entries.Count > 0
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveCollect] Byte-Fehler: {ex.Message}")
                entries = Array.Empty(Of ZipExtractedEntry)()
                Return False
            End Try
        End Function
    End Class

    ''' <summary>
    '''     SharpCompress-Backend zur Verarbeitung verschiedener Archivformate (z. B. TAR, RAR, 7z).
    '''     Kapselt Guard-, I/O- und Policy-Logik für nicht-managed Container.
    ''' </summary>
    Friend NotInheritable Class SharpCompressArchiveBackend
        Implements IArchiveBackend

        ''' <summary>
        '''     Liefert den vom Backend gemeldeten Containertyp.
        ''' </summary>
        Public ReadOnly Property ContainerType As ArchiveContainerType Implements IArchiveBackend.ContainerType
            Get
                Return ArchiveContainerType.Unknown
            End Get
        End Property

        ''' <summary>
        '''     Verarbeitet ein Archiv über SharpCompress fail-closed und optionalen Entry-Callback.
        ''' </summary>
        Public Function Process(
                                stream As Stream,
                                opt As FileTypeProjectOptions,
                                depth As Integer,
                                containerTypeValue As ArchiveContainerType,
                                extractEntry As Func(Of IArchiveEntryModel, Boolean)
                                ) As Boolean Implements IArchiveBackend.Process
            Dim mapped As ArchiveContainerType
            Dim entries As List(Of SharpCompress.Archives.IArchiveEntry)
            Dim nestedResult As Boolean = False
            Dim nestedHandled As Boolean
            Dim totalUncompressed As Long
            Dim model As IArchiveEntryModel
            Dim knownSize As Long
            Dim requireKnownForTotal As Boolean
            Dim gzipWrapped As Boolean
            Dim gzipWrappedTar As Boolean

            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If depth > opt.MaxZipNestingDepth Then Return False
            If containerTypeValue = ArchiveContainerType.Unknown Then Return False

            Try
                StreamGuard.RewindToStart(stream)
                gzipWrapped = ArchiveSharpCompressCompat.HasGZipMagic(stream)
                StreamGuard.RewindToStart(stream)
                If containerTypeValue = ArchiveContainerType.GZip AndAlso Not gzipWrapped Then Return False

                Using archive = OpenArchiveForContainerCompat(stream, containerTypeValue)
                    If archive Is Nothing Then Return False
                    mapped = ArchiveTypeResolver.MapArchiveType(archive.Type)
                    gzipWrappedTar = gzipWrapped AndAlso containerTypeValue = ArchiveContainerType.GZip AndAlso _
                                     mapped = ArchiveContainerType.Tar
                    If mapped <> containerTypeValue AndAlso Not gzipWrappedTar Then Return False

                    entries = archive.Entries.
                            OrderBy(Function(e) If(e.Key, String.Empty), StringComparer.Ordinal).
                            ToList()

                    If Not gzipWrappedTar Then
                        nestedHandled = TryProcessNestedGArchive(entries, opt, depth, containerTypeValue, extractEntry,
                                                                 nestedResult)
                        If nestedHandled Then Return nestedResult
                    End If

                    If entries.Count > opt.MaxZipEntries Then Return False

                    totalUncompressed = 0
                    For Each entry In entries
                        If entry Is Nothing Then Return False
                        If Not entry.IsComplete Then Return False

                        model = New SharpCompressEntryModel(entry)

                        If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(model.LinkTarget) Then
                            LogGuard.Warn(opt.Logger, "[ArchiveGate] Link-Entry ist nicht erlaubt.")
                            Return False
                        End If

                        If Not model.IsDirectory Then
                            knownSize = 0
                            requireKnownForTotal = (extractEntry Is Nothing) OrElse depth > 0
                            If gzipWrappedTar Then
                                requireKnownForTotal = False
                            End If
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
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] SharpCompress-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function OpenArchiveForContainerCompat(stream As Stream,
                                                              containerTypeValue As ArchiveContainerType) _
            As SharpCompress.Archives.IArchive
            Return ArchiveSharpCompressCompat.OpenArchiveForContainer(stream, containerTypeValue)
        End Function

        Private Shared Function TryProcessNestedGArchive(
                                                         entries As List(Of SharpCompress.Archives.IArchiveEntry),
                                                         opt As FileTypeProjectOptions,
                                                         depth As Integer,
                                                         containerType As ArchiveContainerType,
                                                         extractEntry As Func(Of IArchiveEntryModel, Boolean),
                                                         ByRef nestedResult As Boolean
                                                         ) As Boolean
            Dim onlyEntry As SharpCompress.Archives.IArchiveEntry
            Dim model As IArchiveEntryModel
            Dim payload As Byte() = Nothing
            Dim nestedDescriptor As ArchiveDescriptor = Nothing

            nestedResult = False
            If containerType <> ArchiveContainerType.GZip Then Return False
            If entries Is Nothing OrElse entries.Count <> 1 Then Return False

            onlyEntry = entries(0)
            If onlyEntry Is Nothing OrElse onlyEntry.IsDirectory Then Return False

            model = New SharpCompressEntryModel(onlyEntry)
            If opt.RejectArchiveLinks AndAlso Not String.IsNullOrWhiteSpace(model.LinkTarget) Then
                nestedResult = False
                Return True
            End If

            If Not TryReadEntryPayloadBoundedWithOptions(onlyEntry, opt.MaxZipNestedBytes, opt, payload) Then
                nestedResult = False
                Return True
            End If

            If Not ArchiveTypeResolver.TryDescribeBytes(payload, opt, nestedDescriptor) Then
                nestedResult = False
                Return True
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

        Private Shared Function TryReadEntryPayloadBoundedWithOptions(
                                                                       entry As SharpCompress.Archives.IArchiveEntry,
                                                                       maxBytes As Long,
                                                                       opt As FileTypeProjectOptions,
                                                                       ByRef payload As Byte()
                                                                       ) As Boolean
            payload = Array.Empty(Of Byte)()
            If entry Is Nothing Then Return False
            If maxBytes <= 0 Then Return False
            If opt Is Nothing Then Return False

            Try
                Using source = entry.OpenEntryStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    Using ms As New MemoryStream()
                        StreamBounds.CopyBounded(source, ms, maxBytes)
                        payload = ms.ToArray()
                        Return True
                    End Using
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Entry-Payload-Fehler: {ex.Message}")
                payload = Array.Empty(Of Byte)()
                Return False
            End Try
        End Function

        Private Shared Function TryGetValidatedSize(entry As IArchiveEntryModel, opt As FileTypeProjectOptions,
                                                    ByRef knownSize As Long, requireKnownForTotal As Boolean) As Boolean
            Dim value As Long?

            knownSize = 0
            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            value = entry.UncompressedSize
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
            Dim buf(InternalIoDefaults.CopyBufferSize - 1) As Byte
            Dim n As Integer

            measured = 0
            If entry Is Nothing OrElse opt Is Nothing Then Return False

            If opt.AllowUnknownArchiveEntrySize Then Return True

            Try
                Using source = entry.OpenStream()
                    If source Is Nothing OrElse Not source.CanRead Then Return False
                    While True
                        n = source.Read(buf, 0, buf.Length)
                        If n <= 0 Then Exit While
                        measured += n
                        If measured > opt.MaxZipEntryUncompressedBytes Then Return False
                    End While
                End Using
                Return True
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Größenmessung fehlgeschlagen: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

    ''' <summary>
    '''     Interne Hilfsklasse <c>SharpCompressEntryModel</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class SharpCompressEntryModel
        Implements IArchiveEntryModel

        Private ReadOnly _entry As SharpCompress.Archives.IArchiveEntry

        Friend Sub New(entry As SharpCompress.Archives.IArchiveEntry)
            _entry = entry
        End Sub

        ''' <summary>
        '''     Liefert den relativen Archivpfad des Eintrags.
        ''' </summary>
        Public ReadOnly Property RelativePath As String Implements IArchiveEntryModel.RelativePath
            Get
                If _entry Is Nothing Then Return String.Empty
                Return If(_entry.Key, String.Empty)
            End Get
        End Property

        ''' <summary>
        '''     Kennzeichnet, ob der Eintrag ein Verzeichnis repräsentiert.
        ''' </summary>
        Public ReadOnly Property IsDirectory As Boolean Implements IArchiveEntryModel.IsDirectory
            Get
                Return _entry IsNot Nothing AndAlso _entry.IsDirectory
            End Get
        End Property

        ''' <summary>
        '''     Liefert die unkomprimierte Größe, sofern verfügbar.
        ''' </summary>
        Public ReadOnly Property UncompressedSize As Long? Implements IArchiveEntryModel.UncompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Dim size = _entry.Size
                If size < 0 Then Return Nothing
                Return size
            End Get
        End Property

        ''' <summary>
        '''     Liefert die komprimierte Größe, sofern verfügbar.
        ''' </summary>
        Public ReadOnly Property CompressedSize As Long? Implements IArchiveEntryModel.CompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Dim size = _entry.CompressedSize
                If size < 0 Then Return Nothing
                Return size
            End Get
        End Property

        ''' <summary>
        '''     Liefert ein Linkziel bei Link-Einträgen, sonst eine leere Zeichenfolge.
        ''' </summary>
        Public ReadOnly Property LinkTarget As String Implements IArchiveEntryModel.LinkTarget
            Get
                If _entry Is Nothing Then Return String.Empty
                Return If(_entry.LinkTarget, String.Empty)
            End Get
        End Property

        ''' <summary>
        '''     Öffnet einen lesbaren Stream für den Eintragsinhalt.
        ''' </summary>
        Public Function OpenStream() As Stream Implements IArchiveEntryModel.OpenStream
            If _entry Is Nothing Then Return Stream.Null
            Return _entry.OpenEntryStream()
        End Function
    End Class
End Namespace
