' ============================================================================
' FILE: CoreInternals.vb
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
Imports System.Text
Imports Microsoft.Extensions.Logging

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Interne Hilfsklasse <c>InternalIoDefaults</c> zur kapselnden Umsetzung von Guard-, I/O- und Policy-Logik.
    ''' </summary>
    Friend NotInheritable Class InternalIoDefaults
        Friend Const CopyBufferSize As Integer = 8192
        Friend Const FileStreamBufferSize As Integer = 81920
        Friend Const DefaultSniffBytes As Integer = 4096

        Private Sub New()
        End Sub
    End Class

    ''' <summary>
    '''     Zentrale IO-Helfer für harte Grenzen.
    '''     SSOT-Regel: bounded copy wird nur hier gepflegt.
    ''' </summary>
    Friend NotInheritable Class StreamBounds
        Private Sub New()
        End Sub

        Friend Shared Sub CopyBounded(input As Stream, output As Stream, maxBytes As Long)
            Dim buf(InternalIoDefaults.CopyBufferSize - 1) As Byte
            Dim total As Long = 0
            Dim n As Integer

            While True
                n = input.Read(buf, 0, buf.Length)
                If n <= 0 Then Exit While

                total += n
                If total > maxBytes Then Throw New InvalidOperationException("bounded copy exceeded")
                output.Write(buf, 0, n)
            End While
        End Sub
    End Class

    ''' <summary>
    '''     Kleine, zentrale Stream-Guards, um duplizierte Pattern-Checks in Archivroutinen zu reduzieren.
    '''     Keine Semantik: reine Abfrage/Positionierung.
    ''' </summary>
    Friend NotInheritable Class StreamGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsReadable(stream As Stream) As Boolean
            Return stream IsNot Nothing AndAlso stream.CanRead
        End Function

        Friend Shared Sub RewindToStart(stream As Stream)
            If stream Is Nothing Then Return
            If stream.CanSeek Then stream.Position = 0
        End Sub
    End Class

    ''' <summary>
    '''     Sicherheits-Gate für Archive-Container.
    ''' </summary>
    Friend NotInheritable Class ArchiveSafetyGate
        Private Sub New()
        End Sub

        Friend Shared Function IsArchiveSafeBytes(data As Byte(), opt As FileTypeProjectOptions,
                                                  descriptor As ArchiveDescriptor) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return IsArchiveSafeStream(ms, opt, descriptor, depth:=0)
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
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Bytes-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Friend Shared Function IsArchiveSafeStream(stream As Stream, opt As FileTypeProjectOptions,
                                                   descriptor As ArchiveDescriptor, depth As Integer) As Boolean
            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            Return ArchiveProcessingEngine.ValidateArchiveStream(stream, opt, depth, descriptor)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Guards für signaturbasierte Archiv-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ArchiveSignaturePayloadGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsArchiveSignatureCandidate(data As Byte()) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            Return FileTypeRegistry.DetectByMagic(data) = FileKind.Zip
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Guards für beliebige Archive-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ArchivePayloadGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsSafeArchivePayload(data As Byte(), opt As FileTypeProjectOptions) As Boolean
            Dim descriptor As ArchiveDescriptor = Nothing

            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False
            If CLng(data.Length) > opt.MaxBytes Then Return False

            If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return False
            Return ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Zielpfad-Policy für Materialisierung und Archiv-Extraktion.
    ''' </summary>
    Friend NotInheritable Class DestinationPathGuard
        Private Sub New()
        End Sub

        Friend Shared Function PrepareMaterializationTarget(destinationFull As String, overwrite As Boolean,
                                                            opt As FileTypeProjectOptions) As Boolean
            If IsRootPath(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel darf kein Root-Verzeichnis sein.")
                Return False
            End If

            If File.Exists(destinationFull) Then
                If Not overwrite Then Return False
                File.Delete(destinationFull)
            ElseIf Directory.Exists(destinationFull) Then
                If Not overwrite Then Return False
                Directory.Delete(destinationFull, recursive:=True)
            End If

            Return True
        End Function

        Friend Shared Function ValidateNewExtractionTarget(destinationFull As String, opt As FileTypeProjectOptions) _
            As Boolean
            Dim parent As String

            If IsRootPath(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel darf kein Root-Verzeichnis sein.")
                Return False
            End If

            If File.Exists(destinationFull) OrElse Directory.Exists(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel existiert bereits.")
                Return False
            End If

            parent = Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel ohne gültigen Parent.")
                Return False
            End If

            Return True
        End Function

        Friend Shared Function IsRootPath(destinationFull As String) As Boolean
            Dim rootPath As String

            If String.IsNullOrWhiteSpace(destinationFull) Then Return False

            Try
                rootPath = Path.GetPathRoot(destinationFull)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException
                Return False
            End Try

            If String.IsNullOrWhiteSpace(rootPath) Then Return False

            Return String.Equals(
                destinationFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Normalisierung für relative Archiv-Entry-Pfade.
    ''' </summary>
    Friend NotInheritable Class ArchiveEntryPathPolicy
        Private Sub New()
        End Sub

        Friend Shared Function TryNormalizeRelativePath(
                                                        rawPath As String,
                                                        allowDirectoryMarker As Boolean,
                                                        ByRef normalizedPath As String,
                                                        ByRef isDirectory As Boolean
                                                        ) As Boolean
            Dim safe As String
            Dim trimmed As String
            Dim segments As String()

            normalizedPath = String.Empty
            isDirectory = False

            safe = If(rawPath, String.Empty).Trim()
            If safe.Length = 0 Then Return False
            If safe.Contains(ChrW(0)) Then Return False

            safe = safe.Replace("\"c, "/"c)
            If Path.IsPathRooted(safe) Then Return False
            safe = safe.TrimStart("/"c)
            If safe.Length = 0 Then Return False

            trimmed = safe.TrimEnd("/"c)
            If trimmed.Length = 0 Then
                If Not allowDirectoryMarker Then Return False
                normalizedPath = safe
                isDirectory = True
                Return True
            End If

            segments = trimmed.Split("/"c)
            For Each seg In segments
                If seg.Length = 0 Then Return False
                If seg = "." OrElse seg = ".." Then Return False
            Next

            If safe.Length <> trimmed.Length AndAlso Not allowDirectoryMarker Then
                Return False
            End If

            normalizedPath = If(allowDirectoryMarker, safe, trimmed)
            isDirectory = allowDirectoryMarker AndAlso safe.Length <> trimmed.Length
            Return True
        End Function
    End Class

    ''' <summary>
    '''     Verfeinert ZIP-basierte Office-Container zu Dokumenttypen anhand kanonischer Paketmarker.
    '''     Implementationsprinzip:
    '''     - reduziert False-Positives bei generischen ZIP-Dateien
    '''     - bleibt fail-closed (Fehler => Unknown)
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
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Friend Shared Function TryRefineStream(stream As Stream) As FileType
            If Not StreamGuard.IsReadable(stream) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                StreamGuard.RewindToStart(stream)
                Return DetectKindFromArchivePackage(stream)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Private Shared Function DetectKindFromArchivePackage(stream As Stream) As FileType
            Dim hasContentTypes As Boolean = False
            Dim hasDocxMarker As Boolean = False
            Dim hasXlsxMarker As Boolean = False
            Dim hasPptxMarker As Boolean = False
            Dim openDocumentKind As FileKind = FileKind.Unknown
            Dim hasOpenDocumentConflict As Boolean = False
            Dim structuredMarkerCount As Integer
            Dim name As String
            Dim candidateOpenDocumentKind As FileKind

            Try
                Using zip As New ZipArchive(stream, ZipArchiveMode.Read, leaveOpen:=True)

                    For Each entry In zip.Entries
                        name = If(entry.FullName, String.Empty)

                        If String.Equals(name, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase) Then
                            hasContentTypes = True
                        End If

                        If String.Equals(name, "word/document.xml", StringComparison.OrdinalIgnoreCase) Then
                            hasDocxMarker = True
                        ElseIf String.Equals(name, "xl/workbook.xml", StringComparison.OrdinalIgnoreCase) OrElse
                               String.Equals(name, "xl/workbook.bin", StringComparison.OrdinalIgnoreCase) Then
                            hasXlsxMarker = True
                        ElseIf String.Equals(name, "ppt/presentation.xml", StringComparison.OrdinalIgnoreCase) Then
                            hasPptxMarker = True
                        End If

                        candidateOpenDocumentKind = TryDetectOpenDocumentKind(entry)
                        If candidateOpenDocumentKind <> FileKind.Unknown Then
                            If openDocumentKind = FileKind.Unknown Then
                                openDocumentKind = candidateOpenDocumentKind
                            ElseIf openDocumentKind <> candidateOpenDocumentKind Then
                                hasOpenDocumentConflict = True
                            End If
                        End If

                    Next

                    If hasContentTypes Then
                        structuredMarkerCount = 0
                        If hasDocxMarker Then structuredMarkerCount += 1
                        If hasXlsxMarker Then structuredMarkerCount += 1
                        If hasPptxMarker Then structuredMarkerCount += 1

                        If structuredMarkerCount > 1 Then
                            Return FileTypeRegistry.Resolve(FileKind.Unknown)
                        End If

                        If openDocumentKind <> FileKind.Unknown Then
                            Return FileTypeRegistry.Resolve(FileKind.Unknown)
                        End If

                        If hasDocxMarker Then Return FileTypeRegistry.Resolve(FileKind.Docx)
                        If hasXlsxMarker Then Return FileTypeRegistry.Resolve(FileKind.Xlsx)
                        If hasPptxMarker Then Return FileTypeRegistry.Resolve(FileKind.Pptx)
                    End If

                    If hasOpenDocumentConflict Then
                        Return FileTypeRegistry.Resolve(FileKind.Unknown)
                    End If

                    If openDocumentKind <> FileKind.Unknown Then
                        Return FileTypeRegistry.Resolve(openDocumentKind)
                    End If
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
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try

            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

        ''' <summary>
        '''     Liest den OpenDocument-MIME-Eintrag aus einem ZIP-Entry und mappt ihn auf die interne Office-Gruppierung.
        ''' </summary>
        ''' <remarks>
        '''     Fail-closed: Unbekannte, leere oder widersprüchliche MIME-Werte werden als <see cref="FileKind.Unknown"/> behandelt.
        ''' </remarks>
        ''' <param name="entry">ZIP-Entry, der den ODF-MIME-Inhalt enthalten kann.</param>
        ''' <returns>Gemappter Office-Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function TryDetectOpenDocumentKind(entry As ZipArchiveEntry) As FileKind
            Dim mimeValue As String
            Dim normalizedMime As String

            If entry Is Nothing Then Return FileKind.Unknown
            If Not String.Equals(entry.FullName, "mimetype", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Unknown

            mimeValue = ReadZipEntryText(entry, maxBytes:=256)
            If String.IsNullOrWhiteSpace(mimeValue) Then Return FileKind.Unknown
            normalizedMime = mimeValue.Trim().ToLowerInvariant()

            If normalizedMime = "application/vnd.oasis.opendocument.text" Then Return FileKind.Docx
            If normalizedMime = "application/vnd.oasis.opendocument.text-template" Then Return FileKind.Docx
            If normalizedMime = "application/vnd.oasis.opendocument.spreadsheet" Then Return FileKind.Xlsx
            If normalizedMime = "application/vnd.oasis.opendocument.spreadsheet-template" Then Return FileKind.Xlsx
            If normalizedMime = "application/vnd.oasis.opendocument.presentation" Then Return FileKind.Pptx
            If normalizedMime = "application/vnd.oasis.opendocument.presentation-template" Then Return FileKind.Pptx

            Return FileKind.Unknown
        End Function

        ''' <summary>
        '''     Liest einen kleinen Text-Entry defensiv und deterministisch aus einem ZIP-Container.
        ''' </summary>
        ''' <remarks>
        '''     Diese Hilfsfunktion ist absichtlich restriktiv:
        '''     - nur Einträge bis <paramref name="maxBytes"/>
        '''     - kein tolerantes „Best-Effort“-Decoding bei Teilreads
        '''     - Fehlerpfad immer leerer String (fail-closed)
        ''' </remarks>
        ''' <param name="entry">ZIP-Entry, der gelesen werden soll.</param>
        ''' <param name="maxBytes">Maximal erlaubte Größe in Byte.</param>
        ''' <returns>ASCII-Textinhalt oder leerer String bei Guard-/Fehlerpfad.</returns>
        Private Shared Function ReadZipEntryText(entry As ZipArchiveEntry, maxBytes As Integer) As String
            Dim buffer As Byte()
            Dim readTotal As Integer
            Dim readCount As Integer

            If entry Is Nothing Then Return String.Empty
            If maxBytes <= 0 Then Return String.Empty
            If entry.Length < 0 OrElse entry.Length > maxBytes Then Return String.Empty

            Try
                Using entryStream As Stream = entry.Open()
                    buffer = New Byte(CInt(entry.Length) - 1) {}
                    If buffer.Length = 0 Then Return String.Empty

                    While readTotal < buffer.Length
                        readCount = entryStream.Read(buffer, readTotal, buffer.Length - readTotal)
                        If readCount <= 0 Then Exit While
                        readTotal += readCount
                    End While

                    If readTotal <> buffer.Length Then Return String.Empty
                    Return Encoding.ASCII.GetString(buffer)
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
                Return String.Empty
            End Try
        End Function
    End Class

    ''' <summary>
    '''     Verfeinert klassische OLE2-Office-Dokumente (z. B. DOC/XLS/PPT) auf gruppierte Dokumenttypen.
    '''     Ziel ist die robuste Trennung von Office-Dokumenten gegenüber generischen Archiven.
    ''' </summary>
    ''' <remarks>
    '''     Das Refinement ist bewusst heuristisch und fail-closed:
    '''     - Voraussetzung ist ein gültiger OLE-Header.
    '''     - Es muss genau ein Office-Marker eindeutig erkannt werden.
    '''     - Mehrdeutigkeit oder Fehler führen deterministisch zu <see cref="FileKind.Unknown"/>.
    ''' </remarks>
    Friend NotInheritable Class LegacyOfficeBinaryRefiner
        Private Const DefaultMaxProbeBytes As Integer = 1048576

        Private Shared ReadOnly OleSignature As Byte() = {&HD0, &HCF, &H11, &HE0, &HA1, &HB1, &H1A, &HE1}
        Private Shared ReadOnly WordMarker As Byte() = Encoding.ASCII.GetBytes("WordDocument")
        Private Shared ReadOnly ExcelWorkbookMarker As Byte() = Encoding.ASCII.GetBytes("Workbook")
        Private Shared ReadOnly ExcelBookMarker As Byte() = Encoding.ASCII.GetBytes("Book")
        Private Shared ReadOnly PowerPointMarker As Byte() = Encoding.ASCII.GetBytes("PowerPoint Document")

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft, ob die Bytefolge mit der OLE-Compound-File-Signatur beginnt.
        ''' </summary>
        ''' <param name="data">Zu prüfender Header/Payload.</param>
        ''' <returns><c>True</c> bei gültiger OLE-Signatur, sonst <c>False</c>.</returns>
        Friend Shared Function IsOleCompoundHeader(data As Byte()) As Boolean
            Dim i As Integer

            If data Is Nothing Then Return False
            If data.Length < OleSignature.Length Then Return False

            For i = 0 To OleSignature.Length - 1
                If data(i) <> OleSignature(i) Then Return False
            Next

            Return True
        End Function

        ''' <summary>
        '''     Verfeinert einen OLE-Bytepuffer auf den gruppierten Office-Zieltyp.
        ''' </summary>
        ''' <param name="data">Kompletter oder teilweiser OLE-Payload.</param>
        ''' <returns>Gemappter Office-Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Friend Shared Function TryRefineBytes(data As Byte()) As FileType
            If data Is Nothing OrElse data.Length = 0 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                Return RefineByMarkers(data)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        ''' <summary>
        '''     Verfeinert einen Stream auf Legacy-Office-Marker mit harter Probe-Grenze.
        ''' </summary>
        ''' <param name="stream">Lesbarer Quellstream.</param>
        ''' <param name="maxProbeBytes">Maximale Probegröße; wird intern defensiv gekappt.</param>
        ''' <returns>Gemappter Office-Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Friend Shared Function TryRefineStream(stream As Stream, maxProbeBytes As Integer) As FileType
            Dim probeLimit As Integer
            Dim chunk(4095) As Byte
            Dim readTotal As Integer
            Dim readCount As Integer
            Dim targetStream As MemoryStream
            Dim buffer As Byte()

            If Not StreamGuard.IsReadable(stream) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            probeLimit = maxProbeBytes
            If probeLimit <= 0 Then probeLimit = DefaultMaxProbeBytes
            If probeLimit > DefaultMaxProbeBytes Then probeLimit = DefaultMaxProbeBytes

            Try
                StreamGuard.RewindToStart(stream)
                targetStream = New MemoryStream(probeLimit)
                Try
                    While readTotal < probeLimit
                        readCount = stream.Read(chunk, 0, Math.Min(chunk.Length, probeLimit - readTotal))
                        If readCount <= 0 Then Exit While
                        targetStream.Write(chunk, 0, readCount)
                        readTotal += readCount
                    End While

                    buffer = targetStream.ToArray()
                    Return RefineByMarkers(buffer)
                Finally
                    targetStream.Dispose()
                End Try
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        ''' <summary>
        '''     Führt die eigentliche Marker-basierte Typentscheidung für Legacy-Office aus.
        ''' </summary>
        ''' <param name="data">OLE-Bytepuffer.</param>
        ''' <returns>Gruppierter Office-Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function RefineByMarkers(data As Byte()) As FileType
            Dim hasWord As Boolean
            Dim hasExcel As Boolean
            Dim hasPowerPoint As Boolean
            Dim markerCount As Integer

            If Not IsOleCompoundHeader(data) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            hasWord = ContainsMarker(data, WordMarker)
            hasExcel = ContainsMarker(data, ExcelWorkbookMarker) OrElse ContainsMarker(data, ExcelBookMarker)
            hasPowerPoint = ContainsMarker(data, PowerPointMarker)

            markerCount = 0
            If hasWord Then markerCount += 1
            If hasExcel Then markerCount += 1
            If hasPowerPoint Then markerCount += 1

            If markerCount <> 1 Then Return FileTypeRegistry.Resolve(FileKind.Unknown)
            If hasWord Then Return FileTypeRegistry.Resolve(FileKind.Docx)
            If hasExcel Then Return FileTypeRegistry.Resolve(FileKind.Xlsx)
            If hasPowerPoint Then Return FileTypeRegistry.Resolve(FileKind.Pptx)

            Return FileTypeRegistry.Resolve(FileKind.Unknown)
        End Function

        ''' <summary>
        '''     Prüft, ob ein Marker als zusammenhängende Bytefolge im Payload vorkommt.
        ''' </summary>
        ''' <param name="data">Quellpuffer.</param>
        ''' <param name="marker">Gesuchte Marker-Bytefolge.</param>
        ''' <returns><c>True</c> bei Treffer, sonst <c>False</c>.</returns>
        Private Shared Function ContainsMarker(data As Byte(), marker As Byte()) As Boolean
            Dim i As Integer
            Dim j As Integer

            If data Is Nothing OrElse marker Is Nothing Then Return False
            If marker.Length = 0 Then Return False
            If data.Length < marker.Length Then Return False

            For i = 0 To data.Length - marker.Length
                For j = 0 To marker.Length - 1
                    If data(i + j) <> marker(j) Then Exit For
                Next

                If j = marker.Length Then Return True
            Next

            Return False
        End Function
    End Class

    ''' <summary>
    '''     Defensiver Logger-Schutz.
    '''     Logging darf niemals zu Erkennungsfehlern oder Exceptions führen.
    ''' </summary>
    Friend NotInheritable Class LogGuard
        Private Sub New()
        End Sub

        Friend Shared Sub Debug(logger As ILogger, message As String)
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Debug) Then Return
            Try
                logger.LogDebug("{Message}", message)
            Catch ex As Exception When _
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException OrElse
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub

        Friend Shared Sub Warn(logger As ILogger, message As String)
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Warning) Then Return
            Try
                logger.LogWarning("{Message}", message)
            Catch ex As Exception When _
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException OrElse
                TypeOf ex Is FormatException OrElse
                TypeOf ex Is ArgumentException
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub

        Friend Shared Sub [Error](logger As ILogger, message As String, ex As Exception)
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(LogLevel.Error) Then Return
            Try
                logger.LogError(ex, "{Message}", message)
            Catch logEx As Exception When _
                TypeOf logEx Is InvalidOperationException OrElse
                TypeOf logEx Is ObjectDisposedException OrElse
                TypeOf logEx Is FormatException OrElse
                TypeOf logEx Is ArgumentException
                ' Keine Rekursion im Logger-Schutz: Logging-Fehler werden bewusst fail-closed unterdrückt.
            End Try
        End Sub
    End Class
End Namespace
