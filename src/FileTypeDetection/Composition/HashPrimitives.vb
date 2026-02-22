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

        ''' <summary>
        '''     Liefert die zentrale, TFM-spezifische Implementierung der Hash-Primitive als SSOT.
        ''' </summary>
        ''' <remarks>
        '''     Vertrag:
        '''     - Genau eine Instanz pro Prozess (Shared, ReadOnly).
        '''     - Initialisierung erfolgt deterministisch beim ersten Zugriff auf die Property.
        '''     - Die konkrete Providerwahl ist compile-time gebunden (TFM/Projekt-Referenzen).
        ''' </remarks>
        Friend Shared ReadOnly Property Current As IHashPrimitives = New HashPrimitivesProvider()

        ''' <summary>
        '''     Verhindert die Instanziierung; diese Klasse dient ausschließlich als statischer Zugriffspunkt.
        ''' </summary>
        Private Sub New()
        End Sub

    End Class
End Namespace
