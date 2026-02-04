Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.IO.Compression
Imports Microsoft.Extensions.Logging

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
    ''' Sicherheits-Gate fuer ZIP-Container.
    '''
    ''' Sicherheitsgrenzen:
    ''' - zu viele Entries
    ''' - zu grosse unkomprimierte Datenmengen
    ''' - hohe Kompressionsraten
    ''' - tiefe/nicht begrenzte ZIP-Verschachtelung
    ''' </summary>
    Friend NotInheritable Class ZipSafetyGate
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
            Return ZipProcessingEngine.ValidateZipStream(stream, opt, depth)
        End Function

    End Class

    ''' <summary>
    ''' Gemeinsame Guards fuer ZIP-Byte-Payloads.
    ''' </summary>
    Friend NotInheritable Class ZipPayloadGuard
        Private Sub New()
        End Sub

        Friend Shared Function IsZipByMagic(data As Byte()) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            Return FileTypeRegistry.DetectByMagic(data) = FileKind.Zip
        End Function

        Friend Shared Function IsSafeZipPayload(data As Byte(), opt As FileTypeDetectorOptions) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            If opt Is Nothing Then Return False
            If CLng(data.Length) > opt.MaxBytes Then Return False
            If Not IsZipByMagic(data) Then Return False
            Return ZipSafetyGate.IsZipSafeBytes(data, opt)
        End Function
    End Class

    ''' <summary>
    ''' Gemeinsame Zielpfad-Policy fuer Materialisierung und ZIP-Extraktion.
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
