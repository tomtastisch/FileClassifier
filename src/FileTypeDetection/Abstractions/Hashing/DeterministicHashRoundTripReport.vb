Option Strict On
Option Explicit On

Namespace FileTypeDetection

    ''' <summary>
    ''' Ergebnisbericht fuer deterministische h1-h4 RoundTrip-Pruefungen.
    ''' </summary>
    Public NotInheritable Class DeterministicHashRoundTripReport

        Public ReadOnly Property InputPath As String
        Public ReadOnly Property IsArchiveInput As Boolean
        Public ReadOnly Property H1 As DeterministicHashEvidence
        Public ReadOnly Property H2 As DeterministicHashEvidence
        Public ReadOnly Property H3 As DeterministicHashEvidence
        Public ReadOnly Property H4 As DeterministicHashEvidence
        Public ReadOnly Property LogicalH1EqualsH2 As Boolean
        Public ReadOnly Property LogicalH1EqualsH3 As Boolean
        Public ReadOnly Property LogicalH1EqualsH4 As Boolean
        Public ReadOnly Property PhysicalH1EqualsH2 As Boolean
        Public ReadOnly Property PhysicalH1EqualsH3 As Boolean
        Public ReadOnly Property PhysicalH1EqualsH4 As Boolean
        Public ReadOnly Property LogicalConsistent As Boolean
        Public ReadOnly Property Notes As String

        Friend Sub New(inputPath As String, isArchiveInput As Boolean, h1 As DeterministicHashEvidence, h2 As DeterministicHashEvidence, h3 As DeterministicHashEvidence, h4 As DeterministicHashEvidence, notes As String)
            Me.InputPath = If(inputPath, String.Empty)
            Me.IsArchiveInput = isArchiveInput
            Me.H1 = If(h1, DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.Unknown, "h1", "missing"))
            Me.H2 = If(h2, DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.Unknown, "h2", "missing"))
            Me.H3 = If(h3, DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.Unknown, "h3", "missing"))
            Me.H4 = If(h4, DeterministicHashEvidence.CreateFailure(DeterministicHashSourceType.Unknown, "h4", "missing"))
            Me.Notes = If(notes, String.Empty)

            LogicalH1EqualsH2 = EqualLogical(Me.H1, Me.H2)
            LogicalH1EqualsH3 = EqualLogical(Me.H1, Me.H3)
            LogicalH1EqualsH4 = EqualLogical(Me.H1, Me.H4)
            PhysicalH1EqualsH2 = EqualPhysical(Me.H1, Me.H2)
            PhysicalH1EqualsH3 = EqualPhysical(Me.H1, Me.H3)
            PhysicalH1EqualsH4 = EqualPhysical(Me.H1, Me.H4)
            LogicalConsistent = LogicalH1EqualsH2 AndAlso LogicalH1EqualsH3 AndAlso LogicalH1EqualsH4
        End Sub

        Private Shared Function EqualLogical(leftEvidence As DeterministicHashEvidence, rightEvidence As DeterministicHashEvidence) As Boolean
            If leftEvidence Is Nothing OrElse rightEvidence Is Nothing Then Return False
            If leftEvidence.Digests Is Nothing OrElse rightEvidence.Digests Is Nothing Then Return False
            If Not leftEvidence.Digests.HasLogicalHash OrElse Not rightEvidence.Digests.HasLogicalHash Then Return False
            Return String.Equals(leftEvidence.Digests.LogicalSha256, rightEvidence.Digests.LogicalSha256, StringComparison.Ordinal)
        End Function

        Private Shared Function EqualPhysical(leftEvidence As DeterministicHashEvidence, rightEvidence As DeterministicHashEvidence) As Boolean
            If leftEvidence Is Nothing OrElse rightEvidence Is Nothing Then Return False
            If leftEvidence.Digests Is Nothing OrElse rightEvidence.Digests Is Nothing Then Return False
            If Not leftEvidence.Digests.HasPhysicalHash OrElse Not rightEvidence.Digests.HasPhysicalHash Then Return False
            Return String.Equals(leftEvidence.Digests.PhysicalSha256, rightEvidence.Digests.PhysicalSha256, StringComparison.Ordinal)
        End Function
    End Class

End Namespace
