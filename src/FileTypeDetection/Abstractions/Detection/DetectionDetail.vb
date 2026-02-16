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

        Friend Sub New(
                       detectedType As FileType,
                       reasonCode As String,
                       usedZipContentCheck As Boolean,
                       usedStructuredRefinement As Boolean,
                       extensionVerified As Boolean)

            Me.DetectedType = If(detectedType, FileTypeRegistry.Resolve(FileKind.Unknown))
            Me.ReasonCode = If(reasonCode, String.Empty)
            Me.UsedZipContentCheck = usedZipContentCheck
            Me.UsedStructuredRefinement = usedStructuredRefinement
            Me.ExtensionVerified = extensionVerified
        End Sub
    End Class
End Namespace
