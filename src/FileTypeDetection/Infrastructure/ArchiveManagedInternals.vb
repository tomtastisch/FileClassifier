' ============================================================================
' FILE: ArchiveManagedInternals.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Zentrale SSOT-Engine für archivbasierte Verarbeitung.
    '''     Eine Iterationslogik für Validierung und sichere Extraktion.
    ''' </summary>
    Friend NotInheritable Class ArchiveStreamEngine
        Private Shared ReadOnly RecyclableStreams As New Microsoft.IO.RecyclableMemoryStreamManager()

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
            Dim totalUncompressed As Long = 0
            Dim ordered As IEnumerable(Of ZipArchiveEntry) = Nothing
            Dim u As Long = 0
            Dim c As Long = 0
            Dim ratio As Double = 0

            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            If depth > opt.MaxZipNestingDepth Then Return False

            Try
                StreamGuard.RewindToStart(stream)

                Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)
                    If zip.Entries.Count > opt.MaxZipEntries Then Return False

                    totalUncompressed = 0
                    ordered = zip.Entries.OrderBy(Function(e) If(e.FullName, String.Empty), StringComparer.Ordinal)

                    For Each e In ordered
                        u = e.Length
                        c = e.CompressedLength

                        If u > opt.MaxZipEntryUncompressedBytes Then Return False

                        totalUncompressed += u
                        If totalUncompressed > opt.MaxZipTotalUncompressedBytes Then Return False

                        If c > 0 AndAlso opt.MaxZipCompressionRatio > 0 Then
                            ratio = CDbl(u) / CDbl(c)
                            If ratio > opt.MaxZipCompressionRatio Then Return False
                        End If

                        If IsNestedArchiveEntry(e, opt) Then
                            If depth >= opt.MaxZipNestingDepth Then Return False
                            If u <= 0 OrElse u > opt.MaxZipNestedBytes Then Return False

                            Try
                                Using es = e.Open()
                                    Using nestedMs = RecyclableStreams.GetStream("ArchiveStreamEngine.Nested")
                                        StreamBounds.CopyBounded(es, nestedMs, opt.MaxZipNestedBytes)
                                        nestedMs.Position = 0

                                        If Not ProcessArchiveStream(nestedMs, opt, depth + 1, Nothing) Then
                                            Return False
                                        End If
                                    End Using
                                End Using
                            Catch ex As Exception When _
                                TypeOf ex Is UnauthorizedAccessException OrElse
                                TypeOf ex Is System.Security.SecurityException OrElse
                                TypeOf ex Is IOException OrElse
                                TypeOf ex Is InvalidDataException OrElse
                                TypeOf ex Is NotSupportedException OrElse
                                TypeOf ex Is ArgumentException OrElse
                                TypeOf ex Is InvalidOperationException OrElse
                                TypeOf ex Is ObjectDisposedException
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
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Stream-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Private Shared Function IsNestedArchiveEntry(entry As ZipArchiveEntry, opt As FileTypeProjectOptions) As Boolean
            Dim header(15) As Byte
            Dim read As Integer = 0
            Dim exact As Byte() = Array.Empty(Of Byte)()

            If entry Is Nothing Then Return False
            If opt Is Nothing Then Return False

            Try
                Using entryStream = entry.Open()
                    read = entryStream.Read(header, 0, header.Length)
                    If read < 4 Then Return False

                    If read = header.Length Then
                        Return FileTypeRegistry.DetectByMagic(header) = FileKind.Zip
                    End If

                    exact = New Byte(read - 1) {}
                    Buffer.BlockCopy(header, 0, exact, 0, read)
                    Return FileTypeRegistry.DetectByMagic(exact) = FileKind.Zip
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Nested-Header-Fehler: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

    ''' <summary>
    '''     Managed-ZIP-Backend zur Verarbeitung von ZIP-Archiven über <see cref="System.IO.Compression"/>.
    '''     Kapselt Guard-, I/O- und Policy-Logik und delegiert an die <see cref="ArchiveStreamEngine"/>.
    ''' </summary>
    Friend NotInheritable Class ArchiveManagedBackend
        Implements IArchiveBackend

        ''' <summary>
        '''     Liefert den vom Managed-Backend unterstützten Containertyp.
        ''' </summary>
        Public ReadOnly Property ContainerType As ArchiveContainerType Implements IArchiveBackend.ContainerType
            Get
                Return ArchiveContainerType.Zip
            End Get
        End Property

        ''' <summary>
        '''     Verarbeitet ZIP-Archive fail-closed über die Managed-Archive-Engine.
        ''' </summary>
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

    ''' <summary>
    '''     Adapter-Modell für Managed-ZIP-Einträge (<see cref="ZipArchiveEntry"/>) zur Bereitstellung einer
    '''     einheitlichen <see cref="IArchiveEntryModel"/>-Schnittstelle im Managed-Archiv-Backend.
    ''' </summary>
    Friend NotInheritable Class ArchiveManagedEntryModel
        Implements IArchiveEntryModel

        Private ReadOnly _entry As ZipArchiveEntry

        Friend Sub New(entry As ZipArchiveEntry)
            _entry = entry
        End Sub

        ''' <summary>
        '''     Liefert den relativen Archivpfad des Managed-Eintrags.
        ''' </summary>
        Public ReadOnly Property RelativePath As String Implements IArchiveEntryModel.RelativePath
            Get
                If _entry Is Nothing Then Return String.Empty
                Return If(_entry.FullName, String.Empty)
            End Get
        End Property

        ''' <summary>
        '''     Kennzeichnet, ob der Managed-Eintrag ein Verzeichnis repräsentiert.
        ''' </summary>
        Public ReadOnly Property IsDirectory As Boolean Implements IArchiveEntryModel.IsDirectory
            Get
                If _entry Is Nothing Then Return False
                Dim name = If(_entry.FullName, String.Empty)
                Return name.EndsWith("/"c)
            End Get
        End Property

        ''' <summary>
        '''     Liefert die unkomprimierte Größe des Eintrags, sofern verfügbar.
        ''' </summary>
        Public ReadOnly Property UncompressedSize As Long? Implements IArchiveEntryModel.UncompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Return _entry.Length
            End Get
        End Property

        ''' <summary>
        '''     Liefert die komprimierte Größe des Eintrags, sofern verfügbar.
        ''' </summary>
        Public ReadOnly Property CompressedSize As Long? Implements IArchiveEntryModel.CompressedSize
            Get
                If _entry Is Nothing Then Return Nothing
                Return _entry.CompressedLength
            End Get
        End Property

        ''' <summary>
        '''     Liefert für Managed-ZIP immer eine leere Zeichenfolge (keine Link-Metadaten).
        ''' </summary>
        Public ReadOnly Property LinkTarget As String Implements IArchiveEntryModel.LinkTarget
            Get
                Return String.Empty
            End Get
        End Property

        ''' <summary>
        '''     Öffnet einen lesbaren Stream auf den Eintragsinhalt.
        ''' </summary>
        Public Function OpenStream() As Stream Implements IArchiveEntryModel.OpenStream
            If _entry Is Nothing Then Return Stream.Null
            Return _entry.Open()
        End Function
    End Class
End Namespace
