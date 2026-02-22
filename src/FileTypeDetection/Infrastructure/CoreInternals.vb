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
Imports Tomtastisch.FileClassifier.Infrastructure.Utils

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Verfeinert ZIP-basierte Office-Container zu Dokumenttypen anhand kanonischer Paketmarker.
    '''     Implementationsprinzip:
    '''     - reduziert False-Positives bei generischen ZIP-Dateien
    '''     - bleibt fail-closed (Fehler => UNKNOWN)
    ''' </summary>
    Friend NotInheritable Class OpenXmlRefiner
        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Verfeinert einen Stream-Factory-Einstieg auf Office/OpenDocument-Zieltypen.
        ''' </summary>
        ''' <param name="streamFactory">Factory für einen lesbaren Quellstream.</param>
        ''' <returns>Verfeinerter Dateityp oder <see cref="FileKind.Unknown"/> bei Fehlern.</returns>
        Friend Shared Function TryRefine(streamFactory As Func(Of Stream)) As FileType
            If streamFactory Is Nothing Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                Using s = streamFactory()
                    Return TryRefineStream(s)
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
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
        '''     Verfeinert einen vorhandenen Stream auf Office/OpenDocument-Zieltypen.
        ''' </summary>
        ''' <param name="stream">Zu prüfender Quellstream.</param>
        ''' <returns>Verfeinerter Dateityp oder <see cref="FileKind.Unknown"/> bei Fehlern.</returns>
        Friend Shared Function TryRefineStream(stream As Stream) As FileType
            If Not StreamGuard.IsReadable(stream) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                StreamGuard.RewindToStart(stream)
                Return DetectKindFromArchivePackage(stream)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
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
        '''     Führt die eigentliche Marker-basierte Paketanalyse für OpenXML/ODF durch.
        ''' </summary>
        ''' <param name="stream">Zu analysierender ZIP-Stream.</param>
        ''' <returns>Gemappter Dokumenttyp oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function DetectKindFromArchivePackage(stream As Stream) As FileType
            Dim hasContentTypes           As Boolean  = False
            Dim hasDocxMarker             As Boolean  = False
            Dim hasXlsxMarker             As Boolean  = False
            Dim hasPptxMarker             As Boolean  = False
            Dim openDocumentKind          As FileKind = FileKind.Unknown
            Dim hasOpenDocumentConflict   As Boolean  = False
            Dim resolvedKind              As FileKind
            Dim name                      As String
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

                    resolvedKind = ResolveArchivePackageKind(
                        hasContentTypes,
                        hasDocxMarker,
                        hasXlsxMarker,
                        hasPptxMarker,
                        openDocumentKind,
                        hasOpenDocumentConflict
                    )
                    Return FileTypeRegistry.Resolve(resolvedKind)
                End Using
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try

        End Function

        Private Shared Function ResolveArchivePackageKind(
                hasContentTypes As Boolean,
                hasDocxMarker As Boolean,
                hasXlsxMarker As Boolean,
                hasPptxMarker As Boolean,
                openDocumentKind As FileKind,
                hasOpenDocumentConflict As Boolean
            ) As FileKind

            Dim kindKeyFromCsCore     As String  = Nothing
            Dim structuredMarkerCount As Integer

            If CsCoreRuntimeBridge.TryResolveArchivePackageKindKey(
                    hasContentTypes:=hasContentTypes,
                    hasDocxMarker:=hasDocxMarker,
                    hasXlsxMarker:=hasXlsxMarker,
                    hasPptxMarker:=hasPptxMarker,
                    openDocumentKindKey:=FileKindToKindKey(openDocumentKind),
                    hasOpenDocumentConflict:=hasOpenDocumentConflict,
                    kindKey:=kindKeyFromCsCore
                ) Then
                Return KindKeyToFileKind(kindKeyFromCsCore)
            End If

            If hasContentTypes Then
                structuredMarkerCount = 0
                If hasDocxMarker Then structuredMarkerCount += 1
                If hasXlsxMarker Then structuredMarkerCount += 1
                If hasPptxMarker Then structuredMarkerCount += 1

                If structuredMarkerCount > 1 Then Return FileKind.Unknown
                If openDocumentKind <> FileKind.Unknown Then Return FileKind.Unknown
                If hasDocxMarker Then Return FileKind.Doc
                If hasXlsxMarker Then Return FileKind.Xls
                If hasPptxMarker Then Return FileKind.Ppt
            End If

            If hasOpenDocumentConflict Then Return FileKind.Unknown
            If openDocumentKind <> FileKind.Unknown Then Return openDocumentKind
            Return FileKind.Unknown
        End Function

        ''' <summary>
        '''     Liest den OpenDocument-MIME-Eintrag aus einem ZIP-Entry und mappt ihn auf die interne Office-Gruppierung.
        ''' </summary>
        ''' <remarks>
        '''     Fail-closed: Unbekannte, leere oder widersprüchliche MIME-Werte
        '''     werden als <see cref="FileKind.Unknown"/> behandelt.
        ''' </remarks>
        ''' <param name="entry">ZIP-Entry, der den ODF-MIME-Inhalt enthalten kann.</param>
        ''' <returns>Gemappter Office-Typ oder <see cref="FileKind.Unknown"/>.</returns>
        Private Shared Function TryDetectOpenDocumentKind(entry As ZipArchiveEntry) As FileKind
            Dim mimeValue      As String
            Dim normalizedMime As String

            If entry Is Nothing Then Return FileKind.Unknown
            If Not String.Equals(entry.FullName, "mimetype", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Unknown

            mimeValue = ReadZipEntryText(entry, maxBytes:=256)
            If String.IsNullOrWhiteSpace(mimeValue) Then Return FileKind.Unknown
            normalizedMime = mimeValue.Trim().ToLowerInvariant()

            Dim kindKeyFromCsCore As String = Nothing
            If CsCoreRuntimeBridge.TryResolveOpenDocumentMimeKindKey(normalizedMime, kindKeyFromCsCore) Then
                Return KindKeyToFileKind(kindKeyFromCsCore)
            End If

            If normalizedMime = "application/vnd.oasis.opendocument.text" Then Return FileKind.Doc
            If normalizedMime = "application/vnd.oasis.opendocument.text-template" Then Return FileKind.Doc
            If normalizedMime = "application/vnd.oasis.opendocument.spreadsheet" Then Return FileKind.Xls
            If normalizedMime = "application/vnd.oasis.opendocument.spreadsheet-template" Then Return FileKind.Xls
            If normalizedMime = "application/vnd.oasis.opendocument.presentation" Then Return FileKind.Ppt
            If normalizedMime = "application/vnd.oasis.opendocument.presentation-template" Then Return FileKind.Ppt

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
        Private Shared Function ReadZipEntryText _
            (
                entry As ZipArchiveEntry,
                maxBytes As Integer
            ) As String
            Dim buffer    As Byte()
            Dim readTotal As Integer
            Dim readCount As Integer

            If entry Is Nothing Then Return String.Empty
            If maxBytes <= 0 Then Return String.Empty
            If entry.Length <= 0 OrElse entry.Length > maxBytes Then Return String.Empty

            Try
                Using entryStream As Stream = entry.Open()
                    buffer = New Byte(CInt(entry.Length) - 1) {}

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
                TypeOf ex Is Security.SecurityException OrElse
                TypeOf ex Is IOException OrElse
                TypeOf ex Is InvalidDataException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is ObjectDisposedException
                Return String.Empty
            End Try
        End Function

        Private Shared Function KindKeyToFileKind(kindKey As String) As FileKind
            If String.Equals(kindKey, "Doc", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Doc
            If String.Equals(kindKey, "Xls", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Xls
            If String.Equals(kindKey, "Ppt", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Ppt
            Return FileKind.Unknown
        End Function

        Private Shared Function FileKindToKindKey(kind As FileKind) As String
            If kind = FileKind.Doc Then Return "Doc"
            If kind = FileKind.Xls Then Return "Xls"
            If kind = FileKind.Ppt Then Return "Ppt"
            Return "Unknown"
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

        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
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
            If Not ByteArrayGuard.HasContent(data) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                Return RefineByMarkers(data)
            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is Security.SecurityException OrElse
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
        Friend Shared Function TryRefineStream _
            (
                stream As Stream,
                maxProbeBytes As Integer
            ) As FileType
            Dim probeLimit   As Integer
            Dim chunk(4095)  As Byte
            Dim readTotal    As Integer
            Dim readCount    As Integer
            Dim targetStream As MemoryStream
            Dim buffer       As Byte()

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
                TypeOf ex Is Security.SecurityException OrElse
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
            Dim hasWord       As Boolean
            Dim hasExcel      As Boolean
            Dim hasPowerPoint As Boolean
            Dim resolvedKind  As FileKind

            If Not IsOleCompoundHeader(data) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            hasWord = ContainsMarker(data, WordMarker)
            hasExcel = ContainsMarker(data, ExcelWorkbookMarker) OrElse ContainsMarker(data, ExcelBookMarker)
            hasPowerPoint = ContainsMarker(data, PowerPointMarker)

            resolvedKind = ResolveLegacyMarkerKind(hasWord, hasExcel, hasPowerPoint)
            Return FileTypeRegistry.Resolve(resolvedKind)
        End Function

        Private Shared Function ResolveLegacyMarkerKind(
                hasWord As Boolean,
                hasExcel As Boolean,
                hasPowerPoint As Boolean
            ) As FileKind

            Dim markerCount       As Integer
            Dim kindKeyFromCsCore As String  = Nothing

            If CsCoreRuntimeBridge.TryResolveLegacyMarkerKindKey(
                    hasWord:=hasWord,
                    hasExcel:=hasExcel,
                    hasPowerPoint:=hasPowerPoint,
                    kindKey:=kindKeyFromCsCore
                ) Then
                Return KindKeyToFileKind(kindKeyFromCsCore)
            End If

            markerCount = 0
            If hasWord Then markerCount += 1
            If hasExcel Then markerCount += 1
            If hasPowerPoint Then markerCount += 1

            If markerCount <> 1 Then Return FileKind.Unknown
            If hasWord Then Return FileKind.Doc
            If hasExcel Then Return FileKind.Xls
            If hasPowerPoint Then Return FileKind.Ppt
            Return FileKind.Unknown
        End Function

        Private Shared Function KindKeyToFileKind(kindKey As String) As FileKind
            If String.Equals(kindKey, "Doc", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Doc
            If String.Equals(kindKey, "Xls", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Xls
            If String.Equals(kindKey, "Ppt", StringComparison.OrdinalIgnoreCase) Then Return FileKind.Ppt
            Return FileKind.Unknown
        End Function

        ''' <summary>
        '''     Prüft, ob ein Marker als zusammenhängende Bytefolge im Payload vorkommt.
        ''' </summary>
        ''' <param name="data">Quellpuffer.</param>
        ''' <param name="marker">Gesuchte Marker-Bytefolge.</param>
        ''' <returns><c>True</c> bei Treffer, sonst <c>False</c>.</returns>
        Private Shared Function ContainsMarker _
            (
                data As Byte(),
                marker As Byte()
            ) As Boolean
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

End Namespace
