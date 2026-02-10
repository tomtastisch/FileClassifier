Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Deterministische Hash-Sammlung fuer einen Verarbeitungsschritt.
    ''' </summary>
    Public NotInheritable Class HashDigestSet
        Public ReadOnly Property PhysicalSha256 As String
        Public ReadOnly Property LogicalSha256 As String
        Public ReadOnly Property FastPhysicalXxHash3 As String
        Public ReadOnly Property FastLogicalXxHash3 As String
        Public ReadOnly Property HasPhysicalHash As Boolean
        Public ReadOnly Property HasLogicalHash As Boolean

        Friend Sub New(
                       physicalSha256 As String,
                       logicalSha256 As String,
                       fastPhysicalXxHash3 As String,
                       fastLogicalXxHash3 As String,
                       hasPhysicalHash As Boolean,
                       hasLogicalHash As Boolean)
            Me.PhysicalSha256 = Normalize(physicalSha256)
            Me.LogicalSha256 = Normalize(logicalSha256)
            Me.FastPhysicalXxHash3 = Normalize(fastPhysicalXxHash3)
            Me.FastLogicalXxHash3 = Normalize(fastLogicalXxHash3)
            Me.HasPhysicalHash = hasPhysicalHash
            Me.HasLogicalHash = hasLogicalHash
        End Sub

        Friend Shared ReadOnly Property Empty As HashDigestSet
            Get
                Return New HashDigestSet(
                    physicalSha256:=String.Empty,
                    logicalSha256:=String.Empty,
                    fastPhysicalXxHash3:=String.Empty,
                    fastLogicalXxHash3:=String.Empty,
                    hasPhysicalHash:=False,
                    hasLogicalHash:=False)
            End Get
        End Property

        Private Shared Function Normalize(value As String) As String
            Return If(value, String.Empty).Trim().ToLowerInvariant()
        End Function
    End Class
End Namespace
