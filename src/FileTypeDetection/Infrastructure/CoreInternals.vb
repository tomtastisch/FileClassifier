Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.IO.Compression
Imports Microsoft.Extensions.Logging

Namespace FileTypeDetection

    Friend NotInheritable Class InternalIoDefaults
        Friend Const CopyBufferSize As Integer = 8192
        Friend Const FileStreamBufferSize As Integer = 81920
        Friend Const DefaultSniffBytes As Integer = 4096

        Private Sub New()
        End Sub
    End Class

    ''' <summary>
    ''' Zentrale IO-Helfer fuer harte Grenzen.
    ''' SSOT-Regel: bounded copy wird nur hier gepflegt.
    ''' </summary>
    Friend NotInheritable Class StreamBounds
        Private Sub New()
        End Sub

        Friend Shared Sub CopyBounded(input As Stream, output As Stream, maxBytes As Long)
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
    ''' Sicherheits-Gate fuer Archive-Container.
    ''' </summary>
    Friend NotInheritable Class ArchiveSafetyGate
        Private Sub New()
        End Sub

        Friend Shared Function IsArchiveSafeBytes(data As Byte(), opt As FileTypeDetectorOptions, descriptor As ArchiveDescriptor) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False
            If descriptor Is Nothing OrElse descriptor.ContainerType = ArchiveContainerType.Unknown Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return IsArchiveSafeStream(ms, opt, descriptor, depth:=0)
                End Using
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[ArchiveGate] Bytes-Fehler: {ex.Message}")
                Return False
            End Try
        End Function

        Friend Shared Function IsArchiveSafeStream(stream As Stream, opt As FileTypeDetectorOptions, descriptor As ArchiveDescriptor, depth As Integer) As Boolean
            If stream Is Nothing OrElse Not stream.CanRead Then Return False
            If opt Is Nothing Then Return False
            Return ArchiveProcessingEngine.ValidateArchiveStream(stream, opt, depth, descriptor)
        End Function
    End Class

    ''' <summary>
    ''' Gemeinsame Guards fuer signaturbasierte Archiv-Byte-Payloads.
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
    ''' Gemeinsame Guards fuer beliebige Archive-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ArchivePayloadGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsSafeArchivePayload(data As Byte(), opt As FileTypeDetectorOptions) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False
            If CLng(data.Length) > opt.MaxBytes Then Return False

            Dim descriptor As ArchiveDescriptor = Nothing
            If Not ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then Return False
            Return ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor)
        End Function
    End Class

    ''' <summary>
    ''' Gemeinsame Zielpfad-Policy fuer Materialisierung und Archiv-Extraktion.
    ''' </summary>
    Friend NotInheritable Class DestinationPathGuard
        Private Sub New()
        End Sub

        Friend Shared Function PrepareMaterializationTarget(destinationFull As String, overwrite As Boolean, opt As FileTypeDetectorOptions) As Boolean
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

        Friend Shared Function ValidateNewExtractionTarget(destinationFull As String, opt As FileTypeDetectorOptions) As Boolean
            If IsRootPath(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel darf kein Root-Verzeichnis sein.")
                Return False
            End If

            If File.Exists(destinationFull) OrElse Directory.Exists(destinationFull) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel existiert bereits.")
                Return False
            End If

            Dim parent = Path.GetDirectoryName(destinationFull)
            If String.IsNullOrWhiteSpace(parent) Then
                LogGuard.Warn(opt.Logger, "[PathGuard] Ziel ohne gueltigen Parent.")
                Return False
            End If

            Return True
        End Function

        Friend Shared Function IsRootPath(destinationFull As String) As Boolean
            If String.IsNullOrWhiteSpace(destinationFull) Then Return False

            Dim rootPath As String = Nothing
            Try
                rootPath = Path.GetPathRoot(destinationFull)
            Catch
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
    ''' Gemeinsame Normalisierung fuer relative Archiv-Entry-Pfade.
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
            If Path.IsPathRooted(safe) Then Return False
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
    ''' Verfeinert Archivpakete zu OOXML-Typen anhand kanonischer Paket-Pfade.
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
                Return DetectKindFromArchivePackage(stream)
            Catch
                Return FileTypeRegistry.Resolve(FileKind.Unknown)
            End Try
        End Function

        Private Shared Function DetectKindFromArchivePackage(stream As Stream) As FileType
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
