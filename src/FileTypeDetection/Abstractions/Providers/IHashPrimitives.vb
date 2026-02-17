' ============================================================================
' FILE: IHashPrimitives.vb
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    Friend Interface IHexCodec
        Function EncodeLowerHex(data As Byte()) As String
    End Interface

    Friend Interface ISha256Primitives
        Function ComputeHash(data As Byte()) As Byte()
        Function ComputeHashHex(data As Byte()) As String
    End Interface

    Friend Interface IFastHash64
        Function ComputeHashUInt64(data As Byte()) As ULong
        Function ComputeHashHex(data As Byte()) As String
    End Interface

    Friend Interface IHashPrimitives
        ReadOnly Property ProviderMarker As String
        ReadOnly Property HexCodec As IHexCodec
        ReadOnly Property Sha256 As ISha256Primitives
        ReadOnly Property FastHash64 As IFastHash64
    End Interface
End Namespace
