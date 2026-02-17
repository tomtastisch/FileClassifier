' ============================================================================
' FILE: HashPrimitivesProvider.vb
' TFM: netstandard2.0
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Security.Cryptography

Namespace Global.Tomtastisch.FileClassifier
    Friend NotInheritable Class HashPrimitivesProvider
        Implements IHashPrimitives

        Private Shared ReadOnly _hexCodec As IHexCodec = New LowerHexCodec()
        Private Shared ReadOnly _sha256 As ISha256Primitives = New Sha256Primitives(_hexCodec)
        Private Shared ReadOnly _fastHash64 As IFastHash64 = New FastHash64Primitives()

        Public ReadOnly Property ProviderMarker As String Implements IHashPrimitives.ProviderMarker
            Get
                Return "NetStandard2_0"
            End Get
        End Property

        Public ReadOnly Property HexCodec As IHexCodec Implements IHashPrimitives.HexCodec
            Get
                Return _hexCodec
            End Get
        End Property

        Public ReadOnly Property Sha256 As ISha256Primitives Implements IHashPrimitives.Sha256
            Get
                Return _sha256
            End Get
        End Property

        Public ReadOnly Property FastHash64 As IFastHash64 Implements IHashPrimitives.FastHash64
            Get
                Return _fastHash64
            End Get
        End Property

        Private NotInheritable Class LowerHexCodec
            Implements IHexCodec

            Private Shared ReadOnly HexDigits As Char() = "0123456789abcdef".ToCharArray()

            Public Function EncodeLowerHex(data As Byte()) As String Implements IHexCodec.EncodeLowerHex
                Dim safeData = If(data, Array.Empty(Of Byte)())
                If safeData.Length = 0 Then Return String.Empty

                Dim chars(safeData.Length * 2 - 1) As Char
                Dim c = 0
                For Each b In safeData
                    chars(c) = HexDigits((b >> 4) And &HF)
                    c += 1
                    chars(c) = HexDigits(b And &HF)
                    c += 1
                Next
                Return New String(chars)
            End Function
        End Class

        Private NotInheritable Class Sha256Primitives
            Implements ISha256Primitives

            Private ReadOnly _codec As IHexCodec

            Public Sub New(codec As IHexCodec)
                _codec = codec
            End Sub

            Public Function ComputeHash(data As Byte()) As Byte() Implements ISha256Primitives.ComputeHash
                Dim safeData = If(data, Array.Empty(Of Byte)())
                Using sha As Security.Cryptography.SHA256 = Security.Cryptography.SHA256.Create()
                    Return sha.ComputeHash(safeData)
                End Using
            End Function

            Public Function ComputeHashHex(data As Byte()) As String Implements ISha256Primitives.ComputeHashHex
                Return _codec.EncodeLowerHex(ComputeHash(data))
            End Function
        End Class

        Private NotInheritable Class FastHash64Primitives
            Implements IFastHash64

            Public Function ComputeHashUInt64(data As Byte()) As ULong Implements IFastHash64.ComputeHashUInt64
                Dim safeData = If(data, Array.Empty(Of Byte)())
                Return IO.Hashing.XxHash3.HashToUInt64(safeData)
            End Function

            Public Function ComputeHashHex(data As Byte()) As String Implements IFastHash64.ComputeHashHex
                Return ComputeHashUInt64(data).ToString("x16", CultureInfo.InvariantCulture)
            End Function
        End Class
    End Class
End Namespace
