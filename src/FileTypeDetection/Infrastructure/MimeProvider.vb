' ============================================================================
' FILE: MimeProvider.vb
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
    '''     Kapselt das MIME-Mapping als austauschbares Infrastrukturdetail.
    '''     SSOT-Regel:
    '''     - Alle MIME-Zuordnungen für die Registry laufen ausschließlich über diese Klasse.
    '''     - Die eigentliche Dateityp-Erkennung darf niemals von MIME abhängen.
    ''' </summary>
    Friend NotInheritable Class MimeProvider
        Friend Shared ReadOnly Instance As New MimeProvider()

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Liefert den MIME-Typ für eine Endung (mit oder ohne führenden Punkt).
        '''     Gibt bei Fehlern oder unbekannter Endung einen leeren String zurück.
        ''' </summary>
        ''' <param name="extWithDot">Dateiendung mit oder ohne führenden Punkt.</param>
        ''' <returns>Kanonischer MIME-Typ oder leerer String.</returns>
        Friend Shared Function GetMime _
            (
                extWithDot As String
            ) As String

            Dim ext As String = If(String.IsNullOrWhiteSpace(extWithDot), String.Empty, extWithDot)

            If ext.Length = 0 Then Return String.Empty

            If Not ext.StartsWith("."c) Then ext = "." & ext

            Try
                Return HeyRed.Mime.MimeTypesMap.GetMimeType(ext)
            Catch ex As Exception When _
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is InvalidOperationException OrElse
                TypeOf ex Is NotSupportedException
                Return String.Empty
            End Try
        End Function
    End Class

    ''' <summary>
    '''     Liefert Diagnose-Information für Tests ohne Öffnung der öffentlichen API.
    ''' </summary>
    Friend NotInheritable Class MimeProviderDiagnostics
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Name des aktiven Backends entsprechend Compile-Time-Toggle.
        ''' </summary>
        Friend Shared ReadOnly Property ActiveBackendName As String
            Get
                Return "HeyRedMime"
            End Get
        End Property
    End Class
End Namespace
