' ============================================================================
' FILE: HashDigestSet.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.md
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Deterministische Digest-Sammlung für einen Verarbeitungsschritt.
    ''' </summary>
    ''' <remarks>
    '''     Enthält logische und physische Hashwerte (inklusive optionaler Fast- und HMAC-Digests) in normalisierter Form.
    ''' </remarks>
    Public NotInheritable Class HashDigestSet
        ''' <summary>
        '''     Physischer SHA-256-Digest des Quellpayloads.
        ''' </summary>
        Public ReadOnly Property PhysicalSha256 As String

        ''' <summary>
        '''     Logischer SHA-256-Digest der kanonischen Sicht.
        ''' </summary>
        Public ReadOnly Property LogicalSha256 As String

        ''' <summary>
        '''     Optionaler schneller physischer XxHash3-Digest.
        ''' </summary>
        Public ReadOnly Property FastPhysicalXxHash3 As String

        ''' <summary>
        '''     Optionaler schneller logischer XxHash3-Digest.
        ''' </summary>
        Public ReadOnly Property FastLogicalXxHash3 As String

        ''' <summary>
        '''     Optionaler HMAC-SHA256-Digest für den physischen Payload.
        ''' </summary>
        Public ReadOnly Property HmacPhysicalSha256 As String

        ''' <summary>
        '''     Optionaler HMAC-SHA256-Digest für den logischen Payload.
        ''' </summary>
        Public ReadOnly Property HmacLogicalSha256 As String

        ''' <summary>
        '''     Kennzeichnet, ob ein physischer Hashwert vorliegt.
        ''' </summary>
        Public ReadOnly Property HasPhysicalHash As Boolean

        ''' <summary>
        '''     Kennzeichnet, ob ein logischer Hashwert vorliegt.
        ''' </summary>
        Public ReadOnly Property HasLogicalHash As Boolean

        Friend Sub New(
                       physicalSha256 As String,
                       logicalSha256 As String,
                       fastPhysicalXxHash3 As String,
                       fastLogicalXxHash3 As String,
                       hmacPhysicalSha256 As String,
                       hmacLogicalSha256 As String,
                       hasPhysicalHash As Boolean,
                       hasLogicalHash As Boolean)
            Me.PhysicalSha256 = Normalize(physicalSha256)
            Me.LogicalSha256 = Normalize(logicalSha256)
            Me.FastPhysicalXxHash3 = Normalize(fastPhysicalXxHash3)
            Me.FastLogicalXxHash3 = Normalize(fastLogicalXxHash3)
            Me.HmacPhysicalSha256 = Normalize(hmacPhysicalSha256)
            Me.HmacLogicalSha256 = Normalize(hmacLogicalSha256)
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
                    hmacPhysicalSha256:=String.Empty,
                    hmacLogicalSha256:=String.Empty,
                    hasPhysicalHash:=False,
                    hasLogicalHash:=False)
            End Get
        End Property

        Private Shared Function Normalize(value As String) As String
            Return If(value, String.Empty).Trim().ToLowerInvariant()
        End Function
    End Class
End Namespace
