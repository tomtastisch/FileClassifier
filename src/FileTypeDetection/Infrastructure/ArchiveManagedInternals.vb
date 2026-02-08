Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Zentrale SSOT-Engine fuer archivbasierte Verarbeitung.
    '''     Eine Iterationslogik fuer Validierung und sichere Extraktion.
    ''' </summary>
    Friend NotInheritable Class ArchiveStreamEngine
        Private Shared ReadOnly _recyclableStreams As New Microsoft.IO.RecyclableMemoryStreamManager()

        Private Sub New()
        End Sub

        Friend Shared Function ValidateArchiveStream(stream As Stream, opt As FileTypeProjectOptions, depth As Integer) _
            As Boolean
            Return ProcessArchiveStream(stream, opt, depth, Nothing)
        End Function

        Friend Shared Function ProcessArchiveStream(
                                                    stream As Stream,
                                                    opt As FileTypeProjectOptions,
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

                        If IsNestedArchiveEntry(e) Then
                            If depth >= opt.MaxZipNestingDepth Then Return False
                            If u <= 0 OrElse u > opt.MaxZipNestedBytes Then Return False

                            Try
                                Using es = e.Open()
                                    Using nestedMs = _recyclableStreams.GetStream("ArchiveStreamEngine.Nested")
                                        StreamBounds.CopyBounded(es, nestedMs, opt.MaxZipNestedBytes)
                                        nestedMs.Position = 0

                                        If Not ProcessArchiveStream(nestedMs, opt, depth + 1, Nothing) Then
                                            Return False
                                        End If
                                    End Using
                                End Using
                            Catch ex As Exception
                                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Nested-Fehler: {ex.Message}")
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
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Stream-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function IsNestedArchiveEntry(entry As ZipArchiveEntry) As Boolean
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

    Friend NotInheritable Class ArchiveManagedBackend
        Implements IArchiveBackend

        Public ReadOnly Property ContainerType As ArchiveContainerType Implements IArchiveBackend.ContainerType
            Get
                Return ArchiveContainerType.Zip
            End Get
        End Property

        Public Function Process(
                                stream As Stream,
                                opt As FileTypeProjectOptions,
                                depth As Integer,
                                containerTypeValue As ArchiveContainerType,
                                extractEntry As Func(Of IArchiveEntryModel, Boolean)
                                ) As Boolean Implements IArchiveBackend.Process
            If containerTypeValue <> ArchiveContainerType.Zip Then Return False

            If extractEntry Is Nothing Then
                Return ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth, Nothing)
            End If

            Return ArchiveStreamEngine.ProcessArchiveStream(
                stream,
                opt,
                depth,
                Function(entry)
                    Return extractEntry(New ArchiveManagedEntryModel(entry))
                End Function)
        End Function
    End Class

    Friend NotInheritable Class ArchiveManagedEntryModel
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
                Return name.EndsWith("/"c)
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
End Namespace
