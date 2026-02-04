Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports SharpCompress.Archives
Imports SharpCompress.Common

Namespace FileTypeDetection

    ''' <summary>
    ''' Einheitliche Byte-basierte Materialisierung mit optionaler sicherer ZIP-Extraktion.
    ''' </summary>
    Public NotInheritable Class FileMaterializer
        Private Sub New()
        End Sub

        Public Shared Function Persist(data As Byte(), destinationPath As String) As Boolean
            Return Persist(data, destinationPath, overwrite:=False, secureExtract:=False)
        End Function

        Public Shared Function Persist(data As Byte(), destinationPath As String, overwrite As Boolean) As Boolean
            Return Persist(data, destinationPath, overwrite, secureExtract:=False)
        End Function

        Public Shared Function Persist(data As Byte(), destinationPath As String, overwrite As Boolean, secureExtract As Boolean) As Boolean
            Dim opt = FileTypeOptions.GetSnapshot()
            If data Is Nothing Then Return False
            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Materialize] Daten zu gross ({data.Length} > {opt.MaxBytes}).")
                Return False
            End If
            If String.IsNullOrWhiteSpace(destinationPath) Then Return False

            Dim destinationFull As String
            Try
                destinationFull = Path.GetFullPath(destinationPath)
            Catch ex As Exception
                LogGuard.Warn(opt.Logger, $"[Materialize] Ungueltiger Zielpfad: {ex.Message}")
                Return False
            End Try

            If secureExtract AndAlso ZipPayloadGuard.IsZipByMagic(data) Then
                If Not IsReadableZipArchive(data, opt) Then
                    LogGuard.Warn(opt.Logger, "[Materialize] ZIP kann nicht gelesen werden.")
                    Return False
                End If

                If Not ZipSafetyGate.IsZipSafeBytes(data, opt) Then
                    LogGuard.Warn(opt.Logger, "[Materialize] ZIP-Validierung fehlgeschlagen.")
                    Return False
                End If

                Return MaterializeZipBytes(data, destinationFull, overwrite, opt)
            End If

            Return MaterializeRawBytes(data, destinationFull, overwrite, opt)
        End Function

        Private Shared Function MaterializeRawBytes(data As Byte(), destinationFull As String, overwrite As Boolean, opt As FileTypeDetectorOptions) As Boolean
            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then Return False

                Dim parent = Path.GetDirectoryName(destinationFull)
                If String.IsNullOrWhiteSpace(parent) Then Return False
                Directory.CreateDirectory(parent)

                Using fs As New FileStream(destinationFull, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.SequentialScan)
                    fs.Write(data, 0, data.Length)
                End Using

                Return True
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Materialize] Byte-Persistenz fehlgeschlagen.", ex)
                Return False
            End Try
        End Function

        Private Shared Function MaterializeZipBytes(data As Byte(), destinationFull As String, overwrite As Boolean, opt As FileTypeDetectorOptions) As Boolean
            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then Return False

                Using ms As New MemoryStream(data, writable:=False)
                    Return ZipExtractor.TryExtractZipStream(ms, destinationFull, opt)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Materialize] ZIP-Extraktion fehlgeschlagen.", ex)
                Return False
            End Try
        End Function

        Private Shared Function IsReadableZipArchive(data As Byte(), opt As FileTypeDetectorOptions) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Using archive = ArchiveFactory.Open(ms)
                        Return archive IsNot Nothing AndAlso archive.Type = ArchiveType.Zip
                    End Using
                End Using
            Catch ex As Exception
                LogGuard.Debug(opt.Logger, $"[Materialize] SharpCompress ZIP-Check fehlgeschlagen: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

End Namespace
