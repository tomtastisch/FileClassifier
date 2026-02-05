Option Strict On
Option Explicit On

Imports System.Collections.Immutable

Namespace FileTypeDetection

    ''' <summary>
    ''' Nachweisobjekt fuer deterministische Hash-Berechnungen.
    ''' Vereint erkannte Typmetadaten, optionale Archive-Entry-Sicht und komprimierte/unkomprimierte Bytes.
    ''' </summary>
    Public NotInheritable Class DeterministicHashEvidence

        Public ReadOnly Property Label As String
        Public ReadOnly Property DetectedType As FileType
        Public ReadOnly Property Entry As ZipExtractedEntry
        Public ReadOnly Property CompressedBytes As ImmutableArray(Of Byte)
        Public ReadOnly Property UncompressedBytes As ImmutableArray(Of Byte)
        Public ReadOnly Property Hash As String

        Friend Sub New(
            label As String,
            detectedType As FileType,
            hashValue As String,
            compressedBytes As Byte(),
            uncompressedBytes As Byte(),
            entry As ZipExtractedEntry)
            Me.Label = If(label, String.Empty)
            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.Hash = If(hashValue, String.Empty)
            Me.Entry = entry

            If compressedBytes Is Nothing OrElse compressedBytes.Length = 0 Then
                Me.CompressedBytes = ImmutableArray(Of Byte).Empty
            Else
                Me.CompressedBytes = ImmutableArray.Create(compressedBytes)
            End If

            If uncompressedBytes Is Nothing OrElse uncompressedBytes.Length = 0 Then
                Me.UncompressedBytes = ImmutableArray(Of Byte).Empty
            Else
                Me.UncompressedBytes = ImmutableArray.Create(uncompressedBytes)
            End If
        End Sub
    End Class

End Namespace
