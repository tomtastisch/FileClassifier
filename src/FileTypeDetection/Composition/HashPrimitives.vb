' ============================================================================
' FILE: HashPrimitives.vb
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
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
