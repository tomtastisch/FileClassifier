' ============================================================================
' FILE: EvidenceHashing.vb
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
    '''     Öffentliche Fassade für deterministische Hash-Nachweise und RoundTrip-Konsistenzberichte.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Die Fassade orchestriert Dateieinlesung, Typdetektion und Archivsicht und delegiert die eigentliche
    '''         Hash-Berechnung an interne, zustandslose Utility-Komponenten.
    '''     </para>
    '''     <para>
    '''         Fail-Closed-Verhalten: Ungültige Eingaben, Größenlimit-Verstöße und I/O-Fehler liefern stets ein
    '''         deterministisches <c>HashEvidence.CreateFailure(...)</c>-Ergebnis mit unverändertem Fehltext.
    '''     </para>
    '''     <para>
    '''         Side-Effects: <c>VerifyRoundTrip</c> materialisiert kanonische Bytes in ein temporäres Dateisystemziel
    '''         und bereinigt den temporären Ordner anschließend best-effort.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class EvidenceHashing
        Private Const LogicalManifestVersion As String  = "FTD-LOGICAL-HASH-V1"
        Private Const DefaultPayloadLabel As String     = "payload.bin"
        Private Const HmacKeyEnvVarB64 As String        = "FILECLASSIFIER_HMAC_KEY_B64"

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für eine Datei mit Standard-Hashoptionen.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:
        '''         1) Delegation auf die Überladung mit expliziten Optionen,
        '''         2) Anwendung der Snapshot-Defaults aus <c>FileTypeOptions</c>.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashFile _
            (
                path As String
            ) As HashEvidence

            Return HashFile(path, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für eine Datei.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:
        '''         1) Snapshot und Normalisierung der Hashoptionen,
        '''         2) Guard-Validierung (Pfad/Existenz/Bounded Read),
        '''         3) Typdetektion,
        '''         4) Archivzweig über kanonisches Manifest oder Fallback-Zweig über Rohpayload,
        '''         5) Rückgabe als deterministisches <c>HashEvidence</c>.
        '''     </para>
        '''     <para>
        '''         Fail-Closed: Bei Guard-/I/O-Fehlern wird eine Failure-Evidence mit unverändertem Fehltext erzeugt.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashFile _
            (
                path As String,
                options As HashOptions
            ) As HashEvidence

            Dim detectorOptions   As FileTypeProjectOptions              = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions                         = ResolveHashOptions(detectorOptions, options)
            Dim fileBytes         As Byte()                              = Array.Empty(Of Byte)()
            Dim readError         As String                              = String.Empty
            Dim entries           As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()
            Dim detectedType      As FileType

            If String.IsNullOrWhiteSpace(path) OrElse Not IO.File.Exists(path) Then
                Return Failure(HashSourceType.FilePath, path, "Datei nicht gefunden.")
            End If

            If Not EvidenceHashingIO.TryReadFileBounded(path, detectorOptions, fileBytes, readError) Then
                Return Failure(HashSourceType.FilePath, path, readError)
            End If

            detectedType = New FileTypeDetector().Detect(path)
            If ArchiveEntryCollector.TryCollectFromFile(path, detectorOptions, entries) Then
                Return EvidenceHashingCore.BuildEvidenceFromEntries(
                    sourceType:=HashSourceType.FilePath,
                    label:=IO.Path.GetFileName(path),
                    detectedType:=detectedType,
                    compressedBytes:=fileBytes,
                    entries:=entries,
                    hashOptions:=normalizedOptions,
                    notes:="Archive content hashed via canonical manifest.")
            End If

            Return EvidenceHashingCore.BuildEvidenceFromRawPayload(
                sourceType:=HashSourceType.FilePath,
                label:=IO.Path.GetFileName(path),
                detectedType:=detectedType,
                payload:=fileBytes,
                hashOptions:=normalizedOptions,
                notes:="Raw payload hashed directly.")
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für Rohbytes mit Standardlabel.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:
        '''         1) Delegation auf die Überladung mit Label und Optionen,
        '''         2) Verwendung des stabilen Standardlabels <c>payload.bin</c>.
        '''     </para>
        ''' </remarks>
        ''' <param name="data">Zu hashende Rohbytes.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashBytes _
            (
                data As Byte()
            ) As HashEvidence

            Return HashBytes(data, DefaultPayloadLabel, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für Rohbytes mit benutzerdefiniertem Label.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf: <br/>
        '''         1) Delegation auf die Überladung mit expliziten Optionen, <br/>
        '''         2) Label-Normalisierung im Zielpfad.
        '''     </para>
        ''' </remarks>
        ''' <param name="data">Zu hashende Rohbytes.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashBytes _
            (
                data As Byte(),
                label As String
            ) As HashEvidence

            Return HashBytes(data, label, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis für Rohbytes.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf: <br/>
        '''         1) Snapshot und Normalisierung der Hashoptionen,<br/>
        '''         2) Guard-Validierung (null/MaxBytes),<br/>
        '''         3) Typdetektion,<br/>
        '''         4) Archivzweig mit kanonischem Manifest oder Rohpayload-Zweig,<br/>
        '''         5) Rückgabe als deterministisches <c>HashEvidence</c>.
        '''     </para>
        '''     <para>
        '''         <b>Fail-Closed:</b> Bei Guard-Verletzung wird eine Failure-Evidence mit unverändertem Fehltext erzeugt.
        '''     </para>
        ''' </remarks>
        ''' <param name="data">Zu hashende Rohbytes.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashBytes _
            (
                data As Byte(),
                label As String,
                options As HashOptions
            ) As HashEvidence

            Dim detectorOptions   As FileTypeProjectOptions              = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions                         = ResolveHashOptions(detectorOptions, options)
            Dim detectedType      As FileType
            Dim entries           As IReadOnlyList(Of ZipExtractedEntry) = Array.Empty(Of ZipExtractedEntry)()

            If data Is Nothing Then
                Return Failure(HashSourceType.RawBytes, label, "Payload ist null.")
            End If

            If CLng(data.Length) > detectorOptions.MaxBytes Then
                Return Failure(HashSourceType.RawBytes, label, "Payload größer als MaxBytes.")
            End If

            detectedType = New FileTypeDetector().Detect(data)
            If ArchiveEntryCollector.TryCollectFromBytes(data, detectorOptions, entries) Then
                Return EvidenceHashingCore.BuildEvidenceFromEntries(
                    sourceType:=HashSourceType.RawBytes,
                    label:=NormalizeLabel(label),
                    detectedType:=detectedType,
                    compressedBytes:=data,
                    entries:=entries,
                    hashOptions:=normalizedOptions,
                    notes:="Archive bytes hashed via canonical manifest.")
            End If

            Return EvidenceHashingCore.BuildEvidenceFromRawPayload(
                sourceType:=HashSourceType.RawBytes,
                label:=NormalizeLabel(label),
                detectedType:=detectedType,
                payload:=data,
                hashOptions:=normalizedOptions,
                notes:="Raw payload hashed directly.")
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis aus extrahierten Archiveinträgen mit Standardlabel.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:<br/>
        '''         1) Delegation auf die Überladung mit Label und Optionen,<br/>
        '''         2) Verwendung des stabilen Labels <c>archive-entries</c>.
        '''     </para>
        ''' </remarks>
        ''' <param name="entries">Read-only Liste normalisierbarer Archiveinträge.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry)
            ) As HashEvidence

            Return HashEntries(entries, "archive-entries", options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis aus extrahierten Archiveinträgen mit benutzerdefiniertem Label.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:<br/>
        '''         1) Delegation auf die Überladung mit expliziten Optionen,<br/>
        '''         2) Label-Normalisierung im Zielpfad.
        '''     </para>
        ''' </remarks>
        ''' <param name="entries">Read-only Liste normalisierbarer Archiveinträge.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry),
                label As String
            ) As HashEvidence

            Return HashEntries(entries, label, options:=Nothing)
        End Function

        ''' <summary>
        '''     Erstellt einen deterministischen Hash-Nachweis aus extrahierten Archiveinträgen.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:<br/>
        '''         1) Snapshot und Normalisierung der Hashoptionen,<br/>
        '''         2) Deterministische Entry-Normalisierung (Pfad, Deduplizierung, Sortierung),<br/>
        '''         3) Manifestbildung und Digest-Berechnung,<br/>
        '''         4) Rückgabe als <c>HashEvidence</c>.
        '''     </para>
        '''     <para>
        '''         <b>Fail-Closed:</b> Null-Entries, ungültige Pfade oder Duplikate nach Normalisierung führen zu Failure-Evidence.
        '''     </para>
        ''' </remarks>
        ''' <param name="entries">Read-only Liste normalisierbarer Archiveinträge.</param>
        ''' <param name="label">Fachliches Label für den Nachweis.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>Hash-Evidence; bei Fehlern ein fail-closed Nachweisobjekt mit Fehlerhinweis.</returns>
        Public Shared Function HashEntries _
            (
                entries As IReadOnlyList(Of ZipExtractedEntry),
                label As String,
                options As HashOptions
            ) As HashEvidence

            Dim projectOptions    As FileTypeProjectOptions = FileTypeOptions.GetSnapshot()
            Dim normalizedOptions As HashOptions            = ResolveHashOptions(projectOptions, options)

            Return EvidenceHashingCore.BuildEvidenceFromEntries(
                sourceType:=HashSourceType.ArchiveEntries,
                label:=NormalizeLabel(label),
                detectedType:=FileTypeRegistry.Resolve(FileKind.Zip),
                compressedBytes:=Array.Empty(Of Byte)(),
                entries:=entries,
                hashOptions:=normalizedOptions,
                notes:="Entries hashed via canonical manifest.")
        End Function

        ''' <summary>
        '''     Führt den deterministischen h1-h4-RoundTrip mit Standard-Hashoptionen aus.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:<br/>
        '''         1) Delegation auf die Überladung mit expliziten Optionen,<br/>
        '''         2) Anwendung der Snapshot-Defaults aus <c>FileTypeOptions</c>.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <returns>RoundTrip-Bericht mit Konsistenzkennzahlen und Notes.</returns>
        Public Shared Function VerifyRoundTrip _
            (
                path As String
            ) As HashRoundTripReport

            Return VerifyRoundTrip(path, options:=Nothing)
        End Function

        ''' <summary>
        '''     Führt den deterministischen h1-h4-RoundTrip aus und bewertet logische sowie physische Konsistenz.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablauf:<br/>
        '''         1) h1: Hash des Eingabeobjekts,<br/>
        '''         2) h2: Hash der kanonischen Archivsicht bzw. der Originalbytes,<br/>
        '''         3) h3: Hash der logisch kanonisierten Bytes,<br/>
        '''         4) h4: Hash nach Materialisierung der kanonischen Bytes.
        '''     </para>
        '''     <para>
        '''         <b>Side-Effects:</b> Die Materialisierung erzeugt temporär ein Dateiziel im System-Temp-Pfad und entfernt
        '''         den Temp-Ordner anschließend best-effort mit Catch-Filter-Handling.
        '''     </para>
        '''     <para>
        '''         <b>Fail-Closed:</b> Fehlerpfade liefern einen vollständigen Bericht mit Failure-Evidences.
        '''     </para>
        ''' </remarks>
        ''' <param name="path">Pfad zur Eingabedatei.</param>
        ''' <param name="options">Optionale Hashparameter; bei <c>Nothing</c> werden globale Defaults verwendet.</param>
        ''' <returns>RoundTrip-Bericht mit Konsistenzkennzahlen und Notes.</returns>
        Public Shared Function VerifyRoundTrip _
            (
                path As String,
                options As HashOptions
            ) As HashRoundTripReport

            Return EvidenceHashingRoundTrip.VerifyRoundTrip(path, options)
        End Function

        Friend Shared Function ResolveHashOptionsCore _
            (
                projectOptions As FileTypeProjectOptions,
                options As HashOptions
            ) As HashOptions

            Return ResolveHashOptions(projectOptions, options)
        End Function

        Friend Shared Function LogicalManifestVersionCore() As String
            Return LogicalManifestVersion
        End Function

        Friend Shared Function DefaultPayloadLabelCore() As String
            Return DefaultPayloadLabel
        End Function

        Friend Shared Function HmacKeyEnvVarB64Core() As String
            Return HmacKeyEnvVarB64
        End Function

        Private Shared Function Failure _
            (
                sourceType As HashSourceType,
                label As String,
                notes As String
            ) As HashEvidence

            Return HashEvidence.CreateFailure(sourceType, label, notes)
        End Function

        Private Shared Function NormalizeLabel _
            (
                label As String
            ) As String

            Return EvidenceHashingCore.NormalizeLabel(label)
        End Function

        Private Shared Function ResolveHashOptions _
            (
                projectOptions As FileTypeProjectOptions,
                options As HashOptions
            ) As HashOptions

            If options IsNot Nothing Then Return HashOptions.Normalize(options)
            If projectOptions IsNot Nothing Then Return HashOptions.Normalize(projectOptions.DeterministicHash)

            Return HashOptions.Normalize(Nothing)
        End Function
    End Class
End Namespace
