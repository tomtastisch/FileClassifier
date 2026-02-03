Option Strict On
Option Explicit On

Imports System

#If USE_ASPNETCORE_MIME Then
Imports Microsoft.AspNetCore.StaticFiles
#Else
Imports HeyRed.Mime
#End If

Namespace FileTypeDetection

    ''' <summary>
    ''' Kapselt das MIME-Mapping als austauschbares Infrastrukturdetail.
    '''
    ''' SSOT-Regel:
    ''' - Alle MIME-Zuordnungen fuer die Registry laufen ausschliesslich ueber diese Klasse.
    ''' - Die eigentliche Dateityp-Erkennung darf niemals von MIME abhaengen.
    '''
    ''' Toggle:
    ''' - Default: HeyRed.Mime (Package "Mime").
    ''' - Alternativ: ASP.NET Core ContentTypeProvider via Build-Define "USE_ASPNETCORE_MIME".
    ''' </summary>
    Friend NotInheritable Class MimeProvider

        Friend Shared ReadOnly Instance As New MimeProvider()
#If USE_ASPNETCORE_MIME Then
        Private Shared ReadOnly _aspNetProvider As New FileExtensionContentTypeProvider()
#End If

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Liefert den MIME-Typ fuer eine Endung (mit oder ohne fuehrenden Punkt).
        ''' Gibt bei Fehlern oder unbekannter Endung einen leeren String zurueck.
        ''' </summary>
        ''' <param name="extWithDot">Dateiendung mit oder ohne fuehrenden Punkt.</param>
        ''' <returns>Kanonischer MIME-Typ oder leerer String.</returns>
        Friend Function GetMime(extWithDot As String) As String
            If String.IsNullOrWhiteSpace(extWithDot) Then Return String.Empty

            Dim ext = extWithDot
            If Not ext.StartsWith(".", StringComparison.Ordinal) Then ext = "." & ext

#If USE_ASPNETCORE_MIME Then
            Try
                Dim mime As String = Nothing
                If _aspNetProvider.TryGetContentType("x" & ext, mime) AndAlso Not String.IsNullOrWhiteSpace(mime) Then
                    Return mime
                End If
                Return String.Empty
            Catch
                Return String.Empty
            End Try
#Else
            Try
                Return MimeTypesMap.GetMimeType(ext)
            Catch
                Return String.Empty
            End Try
#End If
        End Function

    End Class

    ''' <summary>
    ''' Liefert Diagnose-Information fuer Tests ohne Oeffnung der oeffentlichen API.
    ''' </summary>
    Friend NotInheritable Class MimeProviderDiagnostics
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Name des aktiven Backends entsprechend Compile-Time-Toggle.
        ''' </summary>
        Friend Shared ReadOnly Property ActiveBackendName As String
            Get
#If USE_ASPNETCORE_MIME Then
                Return "AspNetCore"
#Else
                Return "HeyRedMime"
#End If
            End Get
        End Property
    End Class

End Namespace
