Option Strict On
Option Explicit On

Namespace FileTypeDetection

    ''' <summary>
    ''' Detailiertes, auditierbares Ergebnis einer Detektion.
    ''' </summary>
    Public NotInheritable Class DetectionDetail

        Public ReadOnly Property DetectedType As FileType
        Public ReadOnly Property ReasonCode As String
        Public ReadOnly Property UsedZipContentCheck As Boolean
        Public ReadOnly Property UsedStructuredRefinement As Boolean
        Public ReadOnly Property ExtensionVerified As Boolean

        Friend Sub New(
            detectedType As FileType,
            reasonCode As String,
            usedZipContentCheck As Boolean,
            usedStructuredRefinement As Boolean,
            extensionVerified As Boolean)

            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.ReasonCode = If(reasonCode, String.Empty)
            Me.UsedZipContentCheck = usedZipContentCheck
            Me.UsedStructuredRefinement = usedStructuredRefinement
            Me.ExtensionVerified = extensionVerified
        End Sub

    End Class

End Namespace
