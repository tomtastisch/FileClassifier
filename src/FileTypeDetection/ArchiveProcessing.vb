' ============================================================================
' FILE: ArchiveProcessing.vb
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
    '''     Öffentliche Fassade für fail-closed Archivvalidierung und sichere In-Memory-Extraktion.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Verantwortung: Diese Klasse bündelt konsumierbare Archivoperationen ohne direkte Dateisystempersistenz.
    '''     </para>
    '''     <para>
    '''         Security: Validierung und Extraktion verwenden dieselben Sicherheitsgrenzen wie die Kern-Detektion
    '''         (z. B. Entry-Limits, Größenlimits, Traversal-Schutz).
    '''     </para>
    '''     <para>
    '''         Fehlerbehandlung: Alle öffentlichen Operationen arbeiten fail-closed und liefern bei Verstoß oder Fehler
    '''         einen negativen Rückgabewert.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class ArchiveProcessing
        Private Sub New()
        End Sub

        ''' <summary>
        '''     Prüft fail-closed, ob ein Dateipfad einen sicheren Archiv-Container repräsentiert.
        ''' </summary>
        ''' <remarks>
        '''     Die Prüfung delegiert auf die zentrale Archivvalidierung der Detektions-API.
        ''' </remarks>
        ''' <param name="path">Pfad zur zu prüfenden Datei.</param>
        ''' <returns><c>True</c>, wenn das Archiv die Sicherheitsregeln erfüllt; andernfalls <c>False</c>.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann bei Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.Security.SecurityException">Kann bei sicherheitsrelevantem Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.IO.IOException">Kann bei I/O-Zugriff intern auftreten und wird fail-closed behandelt.</exception>
        Public Shared Function TryValidate _
            (
                path As String
            ) As Boolean
            Return FileTypeDetector.TryValidateArchive(path)
        End Function

        ''' <summary>
        '''     Prüft fail-closed, ob ein Byte-Array einen sicheren Archiv-Container repräsentiert.
        ''' </summary>
        ''' <remarks>
        '''     Die Prüfung erfolgt ausschließlich im Speicher und verwendet den aktuellen globalen Options-Snapshot.
        ''' </remarks>
        ''' <param name="data">Zu prüfende Archivbytes.</param>
        ''' <returns><c>True</c>, wenn das Payload als sicher bewertet wird; andernfalls <c>False</c>.</returns>
        Public Shared Function TryValidate _
            (
                data As Byte()
            ) As Boolean
            Dim opt = FileTypeOptions.GetSnapshot()
            Return ArchivePayloadGuard.IsSafeArchivePayload(data, opt)
        End Function

        ''' <summary>
        '''     Extrahiert eine Archivdatei sicher in den Arbeitsspeicher.
        ''' </summary>
        ''' <remarks>
        '''     Es erfolgt keine Persistenz auf dem Dateisystem. Bei Fehlern wird fail-closed eine leere Ergebnisliste
        '''     zurückgegeben.
        ''' </remarks>
        ''' <param name="path">Pfad zur zu extrahierenden Archivdatei.</param>
        ''' <param name="verifyBeforeExtract">
        '''     <c>True</c> aktiviert eine vorgelagerte Typprüfung; <c>False</c> extrahiert direkt unter Sicherheitsregeln.
        ''' </param>
        ''' <returns>Read-only Liste der extrahierten Einträge oder eine leere Liste bei Fehlern.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann bei Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.Security.SecurityException">Kann bei sicherheitsrelevantem Dateizugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.IO.IOException">Kann bei I/O-Zugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.IO.InvalidDataException">Kann bei ungültigen Archivstrukturen intern auftreten und wird fail-closed behandelt.</exception>
        Public Shared Function ExtractToMemory _
            (
                path As String,
                verifyBeforeExtract As Boolean
            ) _
            As IReadOnlyList(Of ZipExtractedEntry)
            Return New FileTypeDetector().ExtractArchiveSafeToMemory(path, verifyBeforeExtract)
        End Function

        ''' <summary>
        '''     Extrahiert Archivbytes sicher in den Arbeitsspeicher.
        ''' </summary>
        ''' <remarks>
        '''     Die Operation validiert implizit die Archivstruktur über den Entry-Collector und liefert fail-closed eine
        '''     leere Liste bei ungültigen oder nicht extrahierbaren Eingaben.
        ''' </remarks>
        ''' <param name="data">Zu extrahierende Archivbytes.</param>
        ''' <returns>Read-only Liste der extrahierten Einträge oder eine leere Liste bei Fehlern.</returns>
        Public Shared Function TryExtractToMemory _
            (
                data As Byte()
            ) As IReadOnlyList(Of ZipExtractedEntry)
            Dim opt = FileTypeOptions.GetSnapshot()
            Dim emptyResult As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If data Is Nothing OrElse data.Length = 0 Then Return emptyResult

            Dim entries As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            If Not ArchiveEntryCollector.TryCollectFromBytes(data, opt, entries) Then Return emptyResult
            Return entries
        End Function
    End Class
End Namespace
