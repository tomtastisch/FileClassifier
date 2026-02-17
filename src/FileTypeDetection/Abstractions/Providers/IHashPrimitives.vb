' ============================================================================
' FILE: IHashPrimitives.vb
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Interne Abstraktion für deterministische Lower-Hex-Kodierung von Bytefolgen.
    ''' </summary>
    ''' <remarks>
    '''     Zweck: Kapselt die TFM-sensitive Hex-Ausgabe hinter einem einheitlichen Vertrag.
    ''' </remarks>
    Friend Interface IHexCodec
        Function EncodeLowerHex(data As Byte()) As String
    End Interface

    ''' <summary>
    '''     Interne Abstraktion für SHA256-Grundoperationen.
    ''' </summary>
    ''' <remarks>
    '''     Zweck: Vereinheitlicht Byte- und Hex-Ausgabe von SHA256 über alle TFMs.
    ''' </remarks>
    Friend Interface ISha256Primitives
        Function ComputeHash(data As Byte()) As Byte()
        Function ComputeHashHex(data As Byte()) As String
    End Interface

    ''' <summary>
    '''     Interne Abstraktion für deterministische 64-Bit-Fast-Hash-Operationen.
    ''' </summary>
    ''' <remarks>
    '''     Zweck: Entkoppelt FastHash64-Berechnung von TFM-spezifischen APIs.
    ''' </remarks>
    Friend Interface IFastHash64
        Function ComputeHashUInt64(data As Byte()) As ULong
        Function ComputeHashHex(data As Byte()) As String
    End Interface

    ''' <summary>
    '''     Aggregierter interner Vertrag für alle Hash-Primitive des Core-Hashings.
    ''' </summary>
    ''' <remarks>
    '''     Zweck: Stellt einen zentralen Zugriffspunkt für Hex, SHA256 und FastHash64 bereit.
    ''' </remarks>
    Friend Interface IHashPrimitives
        ReadOnly Property ProviderMarker As String
        ReadOnly Property HexCodec As IHexCodec
        ReadOnly Property Sha256 As ISha256Primitives
        ReadOnly Property FastHash64 As IFastHash64
    End Interface
End Namespace
