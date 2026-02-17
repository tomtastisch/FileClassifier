' ============================================================================
' FILE: HashPrimitivesProvider.vb
' TFM: net8.0/net10.0
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' ============================================================================

Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Security.Cryptography

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Providerimplementierung der Hash-Primitive für `net8.0` und `net10.0`.
    ''' </summary>
    ''' <remarks>
    '''     Zweck: Kapselt moderne SHA256-, Hex- und FastHash64-APIs hinter stabilen internen Verträgen.
    ''' </remarks>
    Friend NotInheritable Class HashPrimitivesProvider
        Implements IHashPrimitives

        Private Shared ReadOnly _hexCodec As IHexCodec = New LowerHexCodec()
        Private Shared ReadOnly _sha256 As ISha256Primitives = New Sha256Primitives(_hexCodec)
        Private Shared ReadOnly _fastHash64 As IFastHash64 = New FastHash64Primitives()

        Public ReadOnly Property ProviderMarker As String Implements IHashPrimitives.ProviderMarker
            Get
                Return "Net8_0Plus"
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

        ''' <summary>
        '''     Deterministische Lower-Hex-Kodierung über `Convert.ToHexString`.
        ''' </summary>
        ''' <remarks>
        '''     Zweck: Stellt die einheitliche Kleinschreibung der Hex-Ausgabe sicher.
        ''' </remarks>
        Private NotInheritable Class LowerHexCodec
            Implements IHexCodec

            Public Function EncodeLowerHex(data As Byte()) As String Implements IHexCodec.EncodeLowerHex
                Dim safeData = If(data, Array.Empty(Of Byte)())
                Return Convert.ToHexString(safeData).ToLowerInvariant()
            End Function
        End Class

        ''' <summary>
        '''     SHA256-Primitive auf Basis von `SHA256.HashData`.
        ''' </summary>
        ''' <remarks>
        '''     Zweck: Liefert konsistente SHA256-Bytes und -Hex für aktuelle TFMs.
        ''' </remarks>
        Private NotInheritable Class Sha256Primitives
            Implements ISha256Primitives

            Private ReadOnly _codec As IHexCodec

            Public Sub New(codec As IHexCodec)
                _codec = codec
            End Sub

            Public Function ComputeHash(data As Byte()) As Byte() Implements ISha256Primitives.ComputeHash
                Dim safeData = If(data, Array.Empty(Of Byte)())
                Return Security.Cryptography.SHA256.HashData(safeData)
            End Function

            Public Function ComputeHashHex(data As Byte()) As String Implements ISha256Primitives.ComputeHashHex
                Return _codec.EncodeLowerHex(ComputeHash(data))
            End Function
        End Class

        ''' <summary>
        '''     FastHash64-Primitive auf Basis von `System.IO.Hashing.XxHash3`.
        ''' </summary>
        ''' <remarks>
        '''     Zweck: Liefert deterministische UInt64- und Hex-Werte für schnelle Hashvergleiche.
        ''' </remarks>
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
