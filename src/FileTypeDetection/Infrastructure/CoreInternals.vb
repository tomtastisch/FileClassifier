Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    Friend NotInheritable Class InternalIoDefaults
        Friend Const CopyBufferSize As Integer = 8192
        Friend Const FileStreamBufferSize As Integer = 81920
        Friend Const DefaultSniffBytes As Integer = 4096

        Private Sub New()
        End Sub
    End Class

    ''' <summary>
    '''     Zentrale IO-Helfer fuer harte Grenzen.
    '''     SSOT-Regel: bounded copy wird nur hier gepflegt.
    ''' </summary>
    Friend NotInheritable Class StreamBounds
        Private Sub New()
        End Sub

        Friend Shared Sub CopyBounded(input As System.IO.Stream, output As System.IO.Stream, maxBytes As Long)
            Dim buf(InternalIoDefaults.CopyBufferSize - 1) As Byte
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
    '''     Kleine, zentrale Stream-Guards, um duplizierte Pattern-Checks in Archivroutinen zu reduzieren.
    '''     Keine Semantik: reine Abfrage/Positionierung.
    ''' </summary>
    Friend NotInheritable Class StreamGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsReadable(stream As System.IO.Stream) As Boolean
            Return stream IsNot Nothing AndAlso stream.CanRead
        End Function

        Friend Shared Sub RewindToStart(stream As System.IO.Stream)
            If stream Is Nothing Then Return
            If stream.CanSeek Then stream.Position = 0
        End Sub
    End Class

    ''' <summary>
    '''     Sicherheits-Gate fuer Archive-Container.
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
                Using ms As New System.IO.MemoryStream(data, writable:=False)
                    Return IsArchiveSafeStream(ms, opt, descriptor, depth:=0)
                End Using
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Bytes-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Friend Shared Function IsArchiveSafeStream(stream As System.IO.Stream, opt As FileTypeProjectOptions,
                                                   descriptor As ArchiveDescriptor, depth As Integer) As Boolean
            If Not StreamGuard.IsReadable(stream) Then Return False
            If opt Is Nothing Then Return False
            Return ArchiveProcessingEngine.ValidateArchiveStream(stream, opt, depth, descriptor)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Guards fuer signaturbasierte Archiv-Byte-Payloads.
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
    '''     Gemeinsame Guards fuer beliebige Archive-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ArchivePayloadGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsSafeArchivePayload(data As Byte(), opt As FileTypeProjectOptions) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False
            If CLng(data.Length) > opt.MaxBytes Then Return False

            Dim descriptor As ArchiveDescriptor = Nothing
            If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return False
            Return ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Zielpfad-Policy fuer Materialisierung und Archiv-Extraktion.
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

            If System.IO.File.Exists(destinationFull) Then
                If Not overwrite Then Return False
                System.IO.File.Delete(destinationFull)
            ElseIf System.IO.Directory.Exists(destinationFull) Then
                If Not overwrite Then Return False
                System.IO.Directory.Delete(destinationFull, recursive:=True)
            End If

            Return True
        End Function

        Friend Shared Function ValidateNewExtractionTarget(destinationFull As String, opt As FileTypeProjectOptions) _
            As Boolean
            If IsRootPath(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel darf kein Root-Verzeichnis sein.")
                Return False
            End If

            If System.IO.File.Exists(destinationFull) OrElse System.IO.Directory.Exists(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel existiert bereits.")
                Return False
            End If

            Dim parent = System.IO.Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel ohne gueltigen Parent.")
                Return False
            End If

            Return True
        End Function

        Friend Shared Function IsRootPath(destinationFull As String) As Boolean
            If String.IsNullOrWhiteSpace(destinationFull) Then Return False

            Dim rootPath As String
            Try
                rootPath = System.IO.Path.GetPathRoot(destinationFull)
            Catch
                Return False
            End Try

            If String.IsNullOrWhiteSpace(rootPath) Then Return False

            Return String.Equals(
                destinationFull.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar),
                rootPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
        End Function
    End Class

    ''' <summary>
    '''     Gemeinsame Normalisierung fuer relative Archiv-Entry-Pfade.
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
            normalizedPath = String.Empty
            isDirectory = False

            Dim safe = If(rawPath, String.Empty).Trim()
            If safe.Length = 0 Then Return False
            If safe.Contains(ChrW(0)) Then Return False

            safe = safe.Replace("\"c, "/"c)
            If System.IO.Path.IsPathRooted(safe) Then Return False
            safe = safe.TrimStart("/"c)
            If safe.Length = 0 Then Return False

            Dim trimmed = safe.TrimEnd("/"c)
            If trimmed.Length = 0 Then
                If Not allowDirectoryMarker Then Return False
                normalizedPath = safe
                isDirectory = True
                Return True
            End If

            Dim segments = trimmed.Split("/"c)
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
    '''     Verfeinert Archivpakete zu OOXML-Typen anhand kanonischer Paket-Pfade.
    '''     Implementationsprinzip:
    '''     - reduziert False-Positives bei generischen ZIP-Dateien
    '''     - bleibt fail-closed (Fehler => Unknown)
    ''' </summary>
    Friend NotInheritable Class OpenXmlRefiner
        Private Sub New()
        End Sub

        Friend Shared Function TryRefine(streamFactory As Func(Of System.IO.Stream)) As FileType
            If streamFactory Is Nothing Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                Using s = streamFactory()
                    Return TryRefineStream(s)
                End Using
            Catch
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Friend Shared Function TryRefineStream(stream As System.IO.Stream) As FileType
            If Not StreamGuard.IsReadable(stream) Then Return FileTypeRegistry.Resolve(FileKind.Unknown)

            Try
                StreamGuard.RewindToStart(stream)
                Return DetectKindFromArchivePackage(stream)
            Catch
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Private Shared Function DetectKindFromArchivePackage(stream As System.IO.Stream) As FileType
            Try
                Using zip As New System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen:=True)
                    Dim hasContentTypes = False
                    Dim hasDocxMarker = False
                    Dim hasXlsxMarker = False
                    Dim hasPptxMarker = False

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
    '''     Defensiver Logger-Schutz.
    '''     Logging darf niemals zu Erkennungsfehlern oder Exceptions fuehren.
    ''' </summary>
    Friend NotInheritable Class LogGuard
        Private Sub New()
        End Sub

        Friend Shared Sub Debug(logger As Microsoft.Extensions.Logging.ILogger, message As String)
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) Then Return
            Try
                Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(logger, "{Message}", message)
            Catch
            End Try
        End Sub

        Friend Shared Sub Warn(logger As Microsoft.Extensions.Logging.ILogger, message As String)
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning) Then Return
            Try
                Microsoft.Extensions.Logging.LoggerExtensions.LogWarning(logger, "{Message}", message)
            Catch
            End Try
        End Sub

        Friend Shared Sub [Error](logger As Microsoft.Extensions.Logging.ILogger, message As String, ex As Exception)
            If logger Is Nothing Then Return
            If Not logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error) Then Return
            Try
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(logger, ex, "{Message}", message)
            Catch
            End Try
        End Sub
    End Class
End Namespace
