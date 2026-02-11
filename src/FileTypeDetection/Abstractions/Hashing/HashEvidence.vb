Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Nachweisobjekt fuer einen deterministischen Hash-Schritt.
    ''' </summary>
    Public NotInheritable Class HashEvidence
        Public ReadOnly Property SourceType As HashSourceType
        Public ReadOnly Property Label As String
        Public ReadOnly Property DetectedType As FileType
        Public ReadOnly Property Entry As ZipExtractedEntry
        Public ReadOnly Property CompressedBytes As Global.System.Collections.Immutable.ImmutableArray(Of Byte)
        Public ReadOnly Property UncompressedBytes As Global.System.Collections.Immutable.ImmutableArray(Of Byte)
        Public ReadOnly Property EntryCount As Integer
        Public ReadOnly Property TotalUncompressedBytes As Long
        Public ReadOnly Property Digests As HashDigestSet
        Public ReadOnly Property Notes As String

        Friend Sub New(
                       sourceType As HashSourceType,
                       label As String,
                       detectedType As FileType,
                       entry As ZipExtractedEntry,
                       compressedBytes As Byte(),
                       uncompressedBytes As Byte(),
                       entryCount As Integer,
                       totalUncompressedBytes As Long,
                       digests As HashDigestSet,
                       notes As String)
            Me.SourceType = sourceType
            Me.Label = If(label, String.Empty)
            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.Entry = entry
            Me.EntryCount = Math.Max(0, entryCount)
            Me.TotalUncompressedBytes = Math.Max(0, totalUncompressedBytes)
            Me.Digests = If(digests, HashDigestSet.Empty)
            Me.Notes = If(notes, String.Empty)
            Me.CompressedBytes = ToImmutable(compressedBytes)
            Me.UncompressedBytes = ToImmutable(uncompressedBytes)
        End Sub

        Friend Shared Function CreateFailure(sourceType As HashSourceType, label As String, notes As String) _
            As HashEvidence
            Return New HashEvidence(
                sourceType:=sourceType,
                label:=label,
                detectedType:=FileTypeRegistry.Resolve(FileKind.Unknown),
                entry:=Nothing,
                compressedBytes:=Array.Empty(Of Byte)(),
                uncompressedBytes:=Array.Empty(Of Byte)(),
                entryCount:=0,
                totalUncompressedBytes:=0,
                digests:=HashDigestSet.Empty,
                notes:=notes)
        End Function

        Private Shared Function ToImmutable(data As Byte()) As Global.System.Collections.Immutable.ImmutableArray(Of Byte)
            If data Is Nothing OrElse data.Length = 0 Then
                Return Global.System.Collections.Immutable.ImmutableArray(Of Byte).Empty
            End If
            Return Global.System.Collections.Immutable.ImmutableArray.Create(data)
        End Function
    End Class
End Namespace
