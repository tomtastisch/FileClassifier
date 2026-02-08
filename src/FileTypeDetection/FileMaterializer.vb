Option Strict On
Option Explicit On

Imports System.IO

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Einheitliche Byte-basierte Materialisierung mit optionaler sicherer ZIP-Extraktion.
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

        Public Shared Function Persist(data As Byte(), destinationPath As String, overwrite As Boolean,
                                       secureExtract As Boolean) As Boolean
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

            If secureExtract Then
                Dim descriptor As ArchiveDescriptor = Nothing
                If ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then
                    If Not ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor) Then
                        LogGuard.Warn(opt.Logger, "[Materialize] Archiv-Validierung fehlgeschlagen.")
                        Return False
                    End If

                    Return MaterializeArchiveBytes(data, destinationFull, overwrite, opt, descriptor)
                End If

                If ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(data) Then
                    LogGuard.Warn(opt.Logger, "[Materialize] Archiv kann nicht gelesen werden.")
                    Return False
                End If
            End If

            Return MaterializeRawBytes(data, destinationFull, overwrite, opt)
        End Function

        Private Shared Function MaterializeRawBytes(data As Byte(), destinationFull As String, overwrite As Boolean,
                                                    opt As FileTypeProjectOptions) As Boolean
            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then _
                    Return False

                Dim parent = Path.GetDirectoryName(destinationFull)
                If String.IsNullOrWhiteSpace(parent) Then Return False
                Directory.CreateDirectory(parent)

                Using _
                    fs As _
                        New FileStream(destinationFull, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    fs.Write(data, 0, data.Length)
                End Using

                Return True
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Materialize] Byte-Persistenz fehlgeschlagen.", ex)
                Return False
            End Try
        End Function

        Private Shared Function MaterializeArchiveBytes(data As Byte(), destinationFull As String, overwrite As Boolean,
                                                        opt As FileTypeProjectOptions, descriptor As ArchiveDescriptor) _
            As Boolean
            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then _
                    Return False

                Using ms As New MemoryStream(data, writable:=False)
                    Return ArchiveExtractor.TryExtractArchiveStream(ms, destinationFull, opt, descriptor)
                End Using
            Catch ex As Exception
                LogGuard.Error(opt.Logger, "[Materialize] Archiv-Extraktion fehlgeschlagen.", ex)
                Return False
            End Try
        End Function
    End Class
End Namespace
