Option Strict On
Option Explicit On

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Kapselt das MIME-Mapping als austauschbares Infrastrukturdetail.
    '''     SSOT-Regel:
    '''     - Alle MIME-Zuordnungen fuer die Registry laufen ausschliesslich ueber diese Klasse.
    '''     - Die eigentliche Dateityp-Erkennung darf niemals von MIME abhaengen.
    ''' </summary>
    Friend NotInheritable Class MimeProvider
        Friend Shared ReadOnly Instance As New MimeProvider()

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Liefert den MIME-Typ fuer eine Endung (mit oder ohne fuehrenden Punkt).
        '''     Gibt bei Fehlern oder unbekannter Endung einen leeren String zurueck.
        ''' </summary>
        ''' <param name="extWithDot">Dateiendung mit oder ohne fuehrenden Punkt.</param>
        ''' <returns>Kanonischer MIME-Typ oder leerer String.</returns>
        Friend Shared Function GetMime(extWithDot As String) As String
            If String.IsNullOrWhiteSpace(extWithDot) Then Return String.Empty

            Dim ext = extWithDot
            If Not ext.StartsWith("."c) Then ext = "." & ext

            Try
                Return HeyRed.Mime.MimeTypesMap.GetMimeType(ext)
            Catch
                Return String.Empty
            End Try
        End Function
    End Class

    ''' <summary>
    '''     Liefert Diagnose-Information fuer Tests ohne Oeffnung der oeffentlichen API.
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
