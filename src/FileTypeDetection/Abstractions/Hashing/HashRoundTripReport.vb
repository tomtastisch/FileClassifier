' ============================================================================
' FILE: HashRoundTripReport.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
'
' Kontext:
' - Report-Typ zur Bewertung der Konsistenz mehrerer Hash-Evidences über definierte Slots (H1 bis Hn).
' - Fail-closed: fehlende Evidence wird deterministisch als Failure-Eintrag materialisiert.
'
' Hinweise:
' - Keine Behavior-Änderungen durch reines Reformatting: Vergleichslogik bleibt unverändert.
' - Externe API: zentraler Zugriff über Evidence(slot), LogicalEquals(slot), PhysicalEquals(slot).
' ============================================================================

Option Strict On
Option Explicit On

Imports System
Imports Tomtastisch.FileClassifier.Infrastructure.Utils

Namespace Global.Tomtastisch.FileClassifier

    ''' <summary>
    '''     Bericht über die Konsistenz mehrerer Hash-Evidences in festen Slots (H1 bis Hn).
    ''' </summary>
    ''' <remarks>
    '''     Zweck:
    '''     - Normalisiert fehlende Slots fail-closed zu deterministischen Failure-Evidences.
    '''     - Vergleicht H1 gegen H2 bis Hn jeweils logisch und physisch.
    '''
    '''     Verantwortlichkeiten:
    '''     - Slot-Normalisierung (EnsureEvidence).
    '''     - Berechnung der Vergleichsflags (LogicalEquals/PhysicalEquals).
    '''     - Aggregation LogicalConsistent (AND über alle logischen Gleichheiten).
    '''
    '''     Nicht-Ziele:
    '''     - Kein I/O, keine Policy-Engines, keine Logger-Integration.
    '''     - Slot-Ermittlung erfolgt zentral über EnumUtils (Enum.GetValues).
    ''' </remarks>
    Public NotInheritable Class HashRoundTripReport

        ' =====================================================================
        ' Konstanten / Shared ReadOnly (Single Source of Truth)
        ' =====================================================================

        Public Enum HashSlot
            H1 = 1
            H2 = 2
            H3 = 3
            H4 = 4
        End Enum

        Private Shared ReadOnly RequiredSlots As HashSlot() = _
                                    EnumUtils.GetValues(Of HashSlot)(
                                        sortOrder:=EnumUtils.EnumSortOrder.Ascending
                                    )

        ' =====================================================================
        ' Felder / Properties (Typzustand)
        ' =====================================================================

        Public ReadOnly Property InputPath As String
        Public ReadOnly Property IsArchiveInput As Boolean
        Public ReadOnly Property Notes As String

        Private ReadOnly _evidences As HashEvidence()   ' index = slot-1
        Private ReadOnly _logicalEq As Boolean()        ' index 0..n-2 entspricht H2 bis Hn
        Private ReadOnly _physicalEq As Boolean()        ' index 0..n-2 entspricht H2 bis Hn

        Public ReadOnly Property LogicalConsistent As Boolean


        ''' <summary>
        '''     Liefert die Slots, die in dieser Report-Version geführt werden (in Reihenfolge).
        ''' </summary>
        Public ReadOnly Property Slots As HashSlot()
            Get
                Return IterableUtils.CloneArray(RequiredSlots)
            End Get

        End Property


        ' =====================================================================
        ' Konstruktor(en)
        ' =====================================================================

        ''' <summary>
        '''     Erstellt einen Bericht aus Evidences in Slot-Reihenfolge (H1, H2, ...).
        ''' </summary>
        ''' <param name="inputPath">Pfad/Identifier der geprüften Eingabe.</param>
        ''' <param name="isArchiveInput">True, wenn die Eingabe als Archiv verarbeitet wurde.</param>
        ''' <param name="notes">Hinweise (freier Text).</param>
        ''' <param name="evidences">Evidence-Varargs in Slot-Reihenfolge; exakt so viele wie Slots().</param>
        ''' <exception cref="ArgumentException">Wird ausgelöst, wenn evidences Nothing ist oder die Slot-Anzahl nicht passt.</exception>
        Friend Sub New _
            (
                inputPath As String,
                isArchiveInput As Boolean,
                notes As String,
                ParamArray evidences As HashEvidence()
            )

            ' Deklarationsblock (Pflicht, spaltenartig)
            Dim slotCount As Integer = RequiredSlots.Length
            Dim i As Integer
            Dim idx As Integer
            Dim baseEvidence As HashEvidence
            Dim otherEvidence As HashEvidence
            Dim otherSlot As HashSlot
            Dim eqLogical As Boolean
            Dim consistentLocal As Boolean = True

            ' -----------------------------------------------------------------
            ' Guard-Clauses (fail-closed)
            ' -----------------------------------------------------------------
            ArgumentGuard.RequireLength(evidences, slotCount, NameOf(evidences))

            ' -----------------------------------------------------------------
            ' Snapshot / Assignment (Input)
            ' -----------------------------------------------------------------
            Me.InputPath = If(inputPath, String.Empty)
            Me.IsArchiveInput = isArchiveInput
            Me.Notes = If(notes, String.Empty)

            ' -----------------------------------------------------------------
            ' Normalisierung / Canonicalization (Slots)
            ' -----------------------------------------------------------------
            _evidences = New HashEvidence(slotCount - 1) {}
            _logicalEq = New Boolean(slotCount - 2) {}
            _physicalEq = New Boolean(slotCount - 2) {}

            For i = 0 To slotCount - 1
                Dim slot As HashSlot = RequiredSlots(i)
                _evidences(SlotIndex(slot)) = EnsureEvidence(evidences(i), slot)
            Next

            ' -----------------------------------------------------------------
            ' Branches (Vergleiche: H1 gegen H2..Hn)
            ' -----------------------------------------------------------------
            baseEvidence = _evidences(SlotIndex(HashSlot.H1))

            For idx = 0 To slotCount - 2

                otherSlot = RequiredSlots(idx + 1)
                otherEvidence = _evidences(SlotIndex(otherSlot))

                eqLogical = EqualLogical(baseEvidence, otherEvidence)

                _logicalEq(idx) = eqLogical
                _physicalEq(idx) = EqualPhysical(baseEvidence, otherEvidence)

                consistentLocal = consistentLocal AndAlso eqLogical

            Next

            LogicalConsistent = consistentLocal

        End Sub


        ' =====================================================================
        ' Public API
        ' =====================================================================

        ''' <summary>
        '''     Liefert die Evidence für einen Slot (H1 bis Hn).
        ''' </summary>
        ''' <param name="slot">Der Slot, dessen Evidence geliefert werden soll.</param>
        ''' <returns>Die normalisierte Evidence (nie Nothing).</returns>
        Public Function Evidence(slot As HashSlot) As HashEvidence

            ' Deklarationsblock
            Dim index As Integer

            ArgumentGuard.EnumDefined(GetType(HashSlot), slot, NameOf(slot))
            index = SlotIndex(slot)

            Return _evidences(index)

        End Function

        ''' <summary>
        '''     Liefert das Ergebnis des logischen Vergleichs von H1 mit Hx für einen Slot H2 bis Hn.
        ''' </summary>
        ''' <param name="otherSlot">Der Vergleichsslot (H2 bis Hn).</param>
        ''' <returns>True, wenn logisch gleich; sonst False.</returns>
        Public Function LogicalEquals(otherSlot As HashSlot) As Boolean

            ' Deklarationsblock
            Dim index As Integer

            If otherSlot = HashSlot.H1 Then
                Throw New ArgumentException("Use H2..Hn.", NameOf(otherSlot))
            End If

            ArgumentGuard.EnumDefined(GetType(HashSlot), otherSlot, NameOf(otherSlot))
            index = OtherIndex(otherSlot)

            Return _logicalEq(index)

        End Function

        ''' <summary>
        '''     Liefert das Ergebnis des physischen Vergleichs von H1 mit Hx für einen Slot H2 bis Hn.
        ''' </summary>
        ''' <param name="otherSlot">Der Vergleichsslot (H2 bis Hn).</param>
        ''' <returns>True, wenn physisch gleich; sonst False.</returns>
        Public Function PhysicalEquals(otherSlot As HashSlot) As Boolean

            ' Deklarationsblock
            Dim index As Integer

            If otherSlot = HashSlot.H1 Then
                Throw New ArgumentException("Use H2..Hn.", NameOf(otherSlot))
            End If

            ArgumentGuard.EnumDefined(GetType(HashSlot), otherSlot, NameOf(otherSlot))
            index = OtherIndex(otherSlot)

            Return _physicalEq(index)

        End Function


        ' =====================================================================
        ' Internal/Private Helpers
        ' =====================================================================

        Private Shared Function EnsureEvidence(evidence As HashEvidence, slot As HashSlot) As HashEvidence
            If evidence IsNot Nothing Then Return evidence

            Return HashEvidence.CreateFailure(
                HashSourceType.Unknown,
                SlotLabel(slot),
                "missing"
            )
        End Function

        Private Shared Function SlotLabel(slot As HashSlot) As String
            Return "h" & CInt(slot).ToString()
        End Function

        Private Shared Function SlotIndex(slot As HashSlot) As Integer
            Return CInt(slot) - 1
        End Function

        Private Shared Function OtherIndex(otherSlot As HashSlot) As Integer
            Return SlotIndex(otherSlot) - 1
        End Function

        ' Hinweis: Vergleichslogik bleibt unverändert; keine Änderung der Semantik.
        Private Shared Function EqualLogical(leftEvidence As HashEvidence, rightEvidence As HashEvidence) As Boolean
            If leftEvidence Is Nothing OrElse rightEvidence Is Nothing Then Return False
            If leftEvidence.Digests Is Nothing OrElse rightEvidence.Digests Is Nothing Then Return False
            If Not leftEvidence.Digests.HasLogicalHash OrElse Not rightEvidence.Digests.HasLogicalHash Then Return False

            Return String.Equals(
                leftEvidence.Digests.LogicalSha256,
                rightEvidence.Digests.LogicalSha256,
                StringComparison.Ordinal
            )
        End Function

        Private Shared Function EqualPhysical(leftEvidence As HashEvidence, rightEvidence As HashEvidence) As Boolean
            If leftEvidence Is Nothing OrElse rightEvidence Is Nothing Then Return False
            If leftEvidence.Digests Is Nothing OrElse rightEvidence.Digests Is Nothing Then Return False
            If Not leftEvidence.Digests.HasPhysicalHash OrElse Not rightEvidence.Digests.HasPhysicalHash Then Return False

            Return String.Equals(
                leftEvidence.Digests.PhysicalSha256,
                rightEvidence.Digests.PhysicalSha256,
                StringComparison.Ordinal
            )
        End Function

    End Class

End Namespace
