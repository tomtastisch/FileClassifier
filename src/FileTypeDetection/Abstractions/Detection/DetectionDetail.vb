' ============================================================================
' FILE: DetectionDetail.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
' ============================================================================

Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Detailliertes, auditierbares Ergebnis einer Detektionsentscheidung.
    ''' </summary>
    ''' <remarks>
    '''     Das Objekt transportiert den erkannten Typ sowie Trace-Merkmale, die für Compliance-, Diagnose- und
    '''     Policy-Auswertungen vorgesehen sind.
    ''' </remarks>
    Public NotInheritable Class DetectionDetail
        ''' <summary>
        '''     Finaler, nach allen Policies ermittelter Dateityp.
        ''' </summary>
        Public ReadOnly Property DetectedType As FileType

        ''' <summary>
        '''     Deterministischer Grundcode für den eingeschlagenen Entscheidungs- oder Fehlerpfad.
        ''' </summary>
        Public ReadOnly Property ReasonCode As String

        ''' <summary>
        '''     Kennzeichnet, ob eine inhaltsbasierte Archivprüfung durchgeführt wurde.
        ''' </summary>
        Public ReadOnly Property UsedZipContentCheck As Boolean

        ''' <summary>
        '''     Kennzeichnet, ob ein strukturiertes Archiv-Refinement (z. B. OOXML) durchgeführt wurde.
        ''' </summary>
        Public ReadOnly Property UsedStructuredRefinement As Boolean

        ''' <summary>
        '''     Kennzeichnet, ob die Endungs-Policy aktiv geprüft und bestätigt wurde.
        ''' </summary>
        Public ReadOnly Property ExtensionVerified As Boolean

        ''' <summary>
        '''     Initialisiert ein detailliertes Detektionsergebnis.
        ''' </summary>
        ''' <param name="detectedType">Finaler, nach allen Policies ermittelter Dateityp.</param>
        ''' <param name="reasonCode">Deterministischer Reason-Code für den Entscheidungspfad.</param>
        ''' <param name="usedZipContentCheck">Kennzeichnet eine inhaltsbasierte Archivprüfung.</param>
        ''' <param name="usedStructuredRefinement">Kennzeichnet ein strukturiertes Archiv-Refinement.</param>
        ''' <param name="extensionVerified">Kennzeichnet die bestätigte Endungsprüfung.</param>
        Friend Sub New _
            (
                detectedType As FileType,
                reasonCode As String,
                usedZipContentCheck As Boolean,
                usedStructuredRefinement As Boolean,
                extensionVerified As Boolean
            )

            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.ReasonCode = If(reasonCode, String.Empty)
            Me.UsedZipContentCheck = usedZipContentCheck
            Me.UsedStructuredRefinement = usedStructuredRefinement
            Me.ExtensionVerified = extensionVerified
        End Sub
    End Class
End Namespace
