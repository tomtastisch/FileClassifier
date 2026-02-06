Option Strict On
Option Explicit On

Imports System.Collections.Immutable

Namespace FileTypeDetection
    ''' <summary>
    '''     Nachweisobjekt fuer einen deterministischen Hash-Schritt.
    ''' </summary>
    Public NotInheritable Class DeterministicHashEvidence
        Public ReadOnly Property SourceType As DeterministicHashSourceType
        Public ReadOnly Property Label As String
        Public ReadOnly Property DetectedType As FileType
        Public ReadOnly Property Entry As ZipExtractedEntry
        Public ReadOnly Property CompressedBytes As ImmutableArray(Of Byte)
        Public ReadOnly Property UncompressedBytes As ImmutableArray(Of Byte)
        Public ReadOnly Property EntryCount As Integer
        Public ReadOnly Property TotalUncompressedBytes As Long
        Public ReadOnly Property Digests As DeterministicHashDigestSet
        Public ReadOnly Property Notes As String

        Friend Sub New(
                       sourceType As DeterministicHashSourceType,
                       label As String,
                       detectedType As FileType,
                       entry As ZipExtractedEntry,
                       compressedBytes As Byte(),
                       uncompressedBytes As Byte(),
                       entryCount As Integer,
                       totalUncompressedBytes As Long,
                       digests As DeterministicHashDigestSet,
                       notes As String)
            Me.SourceType = sourceType
            Me.Label = If(label, String.Empty)
            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.Entry = entry
            Me.EntryCount = Math.Max(0, entryCount)
            Me.TotalUncompressedBytes = Math.Max(0, totalUncompressedBytes)
            Me.Digests = If(digests, DeterministicHashDigestSet.Empty)
            Me.Notes = If(notes, String.Empty)
            Me.CompressedBytes = ToImmutable(compressedBytes)
            Me.UncompressedBytes = ToImmutable(uncompressedBytes)
        End Sub

        Friend Shared Function CreateFailure(sourceType As DeterministicHashSourceType, label As String, notes As String) _
            As DeterministicHashEvidence
            Return New DeterministicHashEvidence(
                sourceType:=sourceType,
                label:=label,
                detectedType:=FileTypeRegistry.Resolve(FileKind.Unknown),
                entry:=Nothing,
                compressedBytes:=Array.Empty(Of Byte)(),
                uncompressedBytes:=Array.Empty(Of Byte)(),
                entryCount:=0,
                totalUncompressedBytes:=0,
                digests:=DeterministicHashDigestSet.Empty,
                notes:=notes)
        End Function

        Private Shared Function ToImmutable(data As Byte()) As ImmutableArray(Of Byte)
            If data Is Nothing OrElse data.Length = 0 Then
                Return ImmutableArray(Of Byte).Empty
            End If
            Return ImmutableArray.Create(data)
        End Function
    End Class
End Namespace
