Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace FileTypeDetection

    ''' <summary>
    ''' Oeffentliche ZIP-Fassade fuer Validierung und sichere Extraktion.
    ''' </summary>
    Public NotInheritable Class ZipProcessing
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Prueft fail-closed, ob ein Dateipfad ein sicherer ZIP-Container ist.
        ''' </summary>
        Public Shared Function TryValidate(path As String) As Boolean
            Return New FileTypeDetector().TryValidateZip(path)
        End Function

        ''' <summary>
        ''' Prueft fail-closed, ob ein Byte-Array ein sicherer ZIP-Container ist.
        ''' </summary>
        Public Shared Function TryValidate(data As Byte()) As Boolean
            Dim opt = FileTypeOptions.GetSnapshot()
            Return ZipPayloadGuard.IsSafeZipPayload(data, opt)
        End Function

        ''' <summary>
        ''' Extrahiert eine ZIP-Datei sicher in ein neues Zielverzeichnis.
        ''' </summary>
        Public Shared Function ExtractToDirectory(path As String, destinationDirectory As String, verifyBeforeExtract As Boolean) As Boolean
            Return New FileTypeDetector().ExtractZipSafe(path, destinationDirectory, verifyBeforeExtract)
        End Function

        ''' <summary>
        ''' Extrahiert eine ZIP-Datei sicher in Memory.
        ''' </summary>
        Public Shared Function ExtractToMemory(path As String, verifyBeforeExtract As Boolean) As IReadOnlyList(Of ZipExtractedEntry)
            Return New FileTypeDetector().ExtractZipSafeToMemory(path, verifyBeforeExtract)
        End Function

        ''' <summary>
        ''' Extrahiert ZIP-Bytes sicher in Memory.
        ''' </summary>
        Public Shared Function TryExtractToMemory(data As Byte()) As IReadOnlyList(Of ZipExtractedEntry)
            Dim opt = FileTypeOptions.GetSnapshot()
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If Not ZipPayloadGuard.IsSafeZipPayload(data, opt) Then Return emptyResult

            Try
                Using ms As New MemoryStream(data, writable:=False)
                    Return ZipExtractor.TryExtractZipStreamToMemory(ms, opt)
                End Using
            Catch
                Return emptyResult
            End Try
        End Function
    End Class

End Namespace
