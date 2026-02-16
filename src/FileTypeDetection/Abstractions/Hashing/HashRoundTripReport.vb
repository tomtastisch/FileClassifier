' ============================================================================
' FILE: HashRoundTripReport.vb
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
    '''     Ergebnisbericht für deterministische h1-h4-RoundTrip-Prüfungen.
    ''' </summary>
    ''' <remarks>
    '''     Der Bericht stellt Einzel-Evidence und daraus abgeleitete Konsistenzkennzahlen für logische und physische
    '''     Digest-Vergleiche bereit.
    ''' </remarks>
    Public NotInheritable Class HashRoundTripReport
        ''' <summary>
        '''     Eingabepfad des geprüften Objekts.
        ''' </summary>
        Public ReadOnly Property InputPath As String

        ''' <summary>
        '''     Kennzeichnet, ob die Eingabe als Archiv verarbeitet wurde.
        ''' </summary>
        Public ReadOnly Property IsArchiveInput As Boolean

        ''' <summary>
        '''     Erster Nachweis (Ausgangszustand).
        ''' </summary>
        Public ReadOnly Property H1 As HashEvidence

        ''' <summary>
        '''     Zweiter Nachweis (kanonische Sicht).
        ''' </summary>
        Public ReadOnly Property H2 As HashEvidence

        ''' <summary>
        '''     Dritter Nachweis (logische Bytes).
        ''' </summary>
        Public ReadOnly Property H3 As HashEvidence

        ''' <summary>
        '''     Vierter Nachweis (materialisierte Zielrepräsentation).
        ''' </summary>
        Public ReadOnly Property H4 As HashEvidence

        ''' <summary>
        '''     Kennzeichnet Gleichheit der logischen Digests zwischen h1 und h2.
        ''' </summary>
        Public ReadOnly Property LogicalH1EqualsH2 As Boolean

        ''' <summary>
        '''     Kennzeichnet Gleichheit der logischen Digests zwischen h1 und h3.
        ''' </summary>
        Public ReadOnly Property LogicalH1EqualsH3 As Boolean

        ''' <summary>
        '''     Kennzeichnet Gleichheit der logischen Digests zwischen h1 und h4.
        ''' </summary>
        Public ReadOnly Property LogicalH1EqualsH4 As Boolean

        ''' <summary>
        '''     Kennzeichnet Gleichheit der physischen Digests zwischen h1 und h2.
        ''' </summary>
        Public ReadOnly Property PhysicalH1EqualsH2 As Boolean

        ''' <summary>
        '''     Kennzeichnet Gleichheit der physischen Digests zwischen h1 und h3.
        ''' </summary>
        Public ReadOnly Property PhysicalH1EqualsH3 As Boolean

        ''' <summary>
        '''     Kennzeichnet Gleichheit der physischen Digests zwischen h1 und h4.
        ''' </summary>
        Public ReadOnly Property PhysicalH1EqualsH4 As Boolean

        ''' <summary>
        '''     Gesamtindikator für logische Konsistenz über h1 bis h4.
        ''' </summary>
        Public ReadOnly Property LogicalConsistent As Boolean

        ''' <summary>
        '''     Ergänzende Hinweise zum RoundTrip-Lauf.
        ''' </summary>
        Public ReadOnly Property Notes As String

        Friend Sub New(inputPath As String, isArchiveInput As Boolean, h1 As HashEvidence, h2 As HashEvidence,
                       h3 As HashEvidence, h4 As HashEvidence,
                       notes As String)
            Me.InputPath = If(inputPath, String.Empty)
            Me.IsArchiveInput = isArchiveInput
            Me.H1 =
                If(h1, HashEvidence.CreateFailure(HashSourceType.Unknown, "h1", "missing"))
            Me.H2 =
                If(h2, HashEvidence.CreateFailure(HashSourceType.Unknown, "h2", "missing"))
            Me.H3 =
                If(h3, HashEvidence.CreateFailure(HashSourceType.Unknown, "h3", "missing"))
            Me.H4 =
                If(h4, HashEvidence.CreateFailure(HashSourceType.Unknown, "h4", "missing"))
            Me.Notes = If(notes, String.Empty)

            LogicalH1EqualsH2 = EqualLogical(Me.H1, Me.H2)
            LogicalH1EqualsH3 = EqualLogical(Me.H1, Me.H3)
            LogicalH1EqualsH4 = EqualLogical(Me.H1, Me.H4)
            PhysicalH1EqualsH2 = EqualPhysical(Me.H1, Me.H2)
            PhysicalH1EqualsH3 = EqualPhysical(Me.H1, Me.H3)
            PhysicalH1EqualsH4 = EqualPhysical(Me.H1, Me.H4)
            LogicalConsistent = LogicalH1EqualsH2 AndAlso LogicalH1EqualsH3 AndAlso LogicalH1EqualsH4
        End Sub

        Private Shared Function EqualLogical(leftEvidence As HashEvidence, rightEvidence As HashEvidence) As Boolean
            If leftEvidence Is Nothing OrElse rightEvidence Is Nothing Then Return False
            If leftEvidence.Digests Is Nothing OrElse rightEvidence.Digests Is Nothing Then Return False
            If Not leftEvidence.Digests.HasLogicalHash OrElse Not rightEvidence.Digests.HasLogicalHash Then Return False
            Return _
                String.Equals(leftEvidence.Digests.LogicalSha256, rightEvidence.Digests.LogicalSha256,
                              StringComparison.Ordinal)
        End Function

        Private Shared Function EqualPhysical(leftEvidence As HashEvidence, rightEvidence As HashEvidence) As Boolean
            If leftEvidence Is Nothing OrElse rightEvidence Is Nothing Then Return False
            If leftEvidence.Digests Is Nothing OrElse rightEvidence.Digests Is Nothing Then Return False
            If Not leftEvidence.Digests.HasPhysicalHash OrElse Not rightEvidence.Digests.HasPhysicalHash Then _
                Return False
            Return _
                String.Equals(leftEvidence.Digests.PhysicalSha256, rightEvidence.Digests.PhysicalSha256,
                              StringComparison.Ordinal)
        End Function
    End Class
End Namespace
