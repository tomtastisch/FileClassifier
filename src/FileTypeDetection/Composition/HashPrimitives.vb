' ============================================================================
' FILE: HashPrimitives.vb
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Interner Kompositionspunkt für TFM-spezifische Hash-Primitive.
    ''' </summary>
    ''' <remarks>
    '''     Zweck: Stellt genau eine compile-time gebundene Providerinstanz für den Core bereit.
    ''' </remarks>
    Friend NotInheritable Class HashPrimitives
        Private Shared ReadOnly _current As IHashPrimitives = New HashPrimitivesProvider()

        Private Sub New()
        End Sub

        Friend Shared ReadOnly Property Current As IHashPrimitives
            Get
                Return _current
            End Get
        End Property
    End Class
End Namespace
