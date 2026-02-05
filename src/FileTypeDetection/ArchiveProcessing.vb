Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace FileTypeDetection

    ''' <summary>
    ''' Oeffentliche Archiv-Fassade fuer Validierung und sichere Extraktion.
    ''' </summary>
    Public NotInheritable Class ArchiveProcessing
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Prueft fail-closed, ob ein Dateipfad ein sicherer Archiv-Container ist.
        ''' </summary>
        Public Shared Function TryValidate(path As String) As Boolean
            Return New FileTypeDetector().TryValidateArchive(path)
        End Function

        ''' <summary>
        ''' Prueft fail-closed, ob ein Byte-Array ein sicherer Archiv-Container ist.
        ''' </summary>
        Public Shared Function TryValidate(data As Byte()) As Boolean
            Dim opt = FileTypeOptions.GetSnapshot()
            Return ArchivePayloadGuard.IsSafeArchivePayload(data, opt)
        End Function

        ''' <summary>
        ''' Extrahiert eine Archivdatei sicher in Memory.
        ''' </summary>
        Public Shared Function ExtractToMemory(path As String, verifyBeforeExtract As Boolean) As IReadOnlyList(Of ZipExtractedEntry)
            Return New FileTypeDetector().ExtractArchiveSafeToMemory(path, verifyBeforeExtract)
        End Function

        ''' <summary>
        ''' Extrahiert Archiv-Bytes sicher in Memory.
        ''' </summary>
        Public Shared Function TryExtractToMemory(data As Byte()) As IReadOnlyList(Of ZipExtractedEntry)
            Dim opt = FileTypeOptions.GetSnapshot()
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If data Is Nothing OrElse data.Length = 0 Then Return emptyResult

            Dim entries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If Not ArchiveEntryCollector.TryCollectFromBytes(data, opt, entries) Then Return emptyResult
            Return entries
        End Function
    End Class

End Namespace
