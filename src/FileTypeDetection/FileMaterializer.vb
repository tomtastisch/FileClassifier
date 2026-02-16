Option Strict On
Option Explicit On

Imports System.IO

Namespace Global.Tomtastisch.FileClassifier
    ''' <summary>
    '''     Materialisiert Byte-Payloads fail-closed in ein Dateiziel oder extrahiert Archive sicher in ein Zielverzeichnis.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Verantwortung: Die Klasse kapselt den deterministischen Persistenzpfad für Rohbytes und den sicheren
    '''         Extraktionspfad für Archivpayloads.
    '''     </para>
    '''     <para>
    '''         Invarianten:
    '''         1) Eingaben werden vor jeder I/O-Operation auf Null, Größenlimits und Zielpfad-Eignung geprüft.
    '''         2) Bei aktivem <c>secureExtract</c> wird vor der Extraktion stets ein Sicherheits-Gate ausgeführt.
    '''         3) Fehlerpfade sind fail-closed und liefern <c>False</c>.
    '''     </para>
    '''     <para>
    '''         Nebenwirkungen: Dateisystemzugriffe (Verzeichniserstellung, Dateischreiben, optionale Archivextraktion)
    '''         sowie Warn-/Fehlerprotokollierung über den konfigurierten Logger.
    '''     </para>
    '''     <para>
    '''         Security/Compliance: Pfadnormalisierung, Archiv-Sicherheitsprüfung und atomare Zielvorbereitung begrenzen
    '''         Traversal-, Overwrite- und Ressourcenrisiken im Sinne einer fail-closed Verarbeitung.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class FileMaterializer

        Private Sub New()
        End Sub

        ''' <summary>
        '''     Persistiert Rohbytes in den angegebenen Zielpfad mit Standardverhalten ohne Überschreiben und ohne sichere Extraktion.
        ''' </summary>
        ''' <remarks>
        '''     Diese Überladung delegiert vollständig auf die Vollüberladung mit <c>overwrite:=False</c> und
        '''     <c>secureExtract:=False</c>.
        ''' </remarks>
        ''' <param name="data">Zu persistierende Nutzdaten.</param>
        ''' <param name="destinationPath">Dateisystemziel für die Materialisierung.</param>
        ''' <returns><c>True</c> bei erfolgreicher Persistierung; andernfalls <c>False</c>.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann aus I/O-Operationen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.Security.SecurityException">Kann bei Zugriff auf geschützte Pfade intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="IOException">Kann bei Datei- oder Verzeichniszugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="InvalidDataException">Kann bei ungültigen Datenzuständen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützten Pfadformaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumenten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <example>
        '''     <code language="vb">
        ''' Dim payload As Byte() = File.ReadAllBytes("input.bin")
        ''' Dim ok As Boolean = FileMaterializer.Persist(payload, "output.bin")
        '''     </code>
        ''' </example>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String
            ) As Boolean

            Return Persist(data, destinationPath, overwrite:=False, secureExtract:=False)

        End Function

        ''' <summary>
        '''     Persistiert Rohbytes in den angegebenen Zielpfad und steuert explizit das Überschreibverhalten.
        ''' </summary>
        ''' <remarks>
        '''     Diese Überladung delegiert vollständig auf die Vollüberladung mit <c>secureExtract:=False</c>.
        ''' </remarks>
        ''' <param name="data">Zu persistierende Nutzdaten.</param>
        ''' <param name="destinationPath">Dateisystemziel für die Materialisierung.</param>
        ''' <param name="overwrite"><c>True</c>, um ein bestehendes Ziel gemäß Zielpfad-Policy zu ersetzen; sonst <c>False</c>.</param>
        ''' <returns><c>True</c> bei erfolgreicher Persistierung; andernfalls <c>False</c>.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann aus I/O-Operationen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.Security.SecurityException">Kann bei Zugriff auf geschützte Pfade intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="IOException">Kann bei Datei- oder Verzeichniszugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="InvalidDataException">Kann bei ungültigen Datenzuständen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützten Pfadformaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumenten intern auftreten und wird fail-closed behandelt.</exception>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String,
                overwrite As Boolean
            ) As Boolean

            Return Persist(data, destinationPath, overwrite, secureExtract:=False)
        End Function

        ''' <summary>
        '''     Führt die vollständige Materialisierungslogik für Rohdaten und optional sichere Archivextraktion aus.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Guard-Clauses für Daten, Größenlimits und Zielpfad.
        '''         2) Pfadnormalisierung auf absoluten Zielpfad.
        '''         3) Optionaler <c>secureExtract</c>-Pfad mit Typbeschreibung, Safety-Gate und Extraktion.
        '''         4) Raw-Fallback als Bytepersistenz.
        '''     </para>
        '''     <para>
        '''         Fail-Closed: Jeder validierte Fehlerpfad führt zu <c>False</c>; es wird keine partielle Erfolgsmeldung erzeugt.
        '''     </para>
        ''' </remarks>
        ''' <param name="data">Zu materialisierende Nutzdaten.</param>
        ''' <param name="destinationPath">Datei- oder Verzeichnisziel abhängig vom Verarbeitungspfad.</param>
        ''' <param name="overwrite"><c>True</c>, um ein vorhandenes Ziel gemäß Zielpfad-Policy zu ersetzen.</param>
        ''' <param name="secureExtract"><c>True</c>, um Archivpayloads sicher zu validieren und zu extrahieren; sonst Rohpersistenz.</param>
        ''' <returns><c>True</c> bei erfolgreicher Materialisierung; andernfalls <c>False</c>.</returns>
        ''' <exception cref="UnauthorizedAccessException">Kann bei Pfad- und I/O-Zugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="System.Security.SecurityException">Kann bei sicherheitsrelevanten Dateisystemoperationen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="IOException">Kann bei Dateisystemzugriff intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="InvalidDataException">Kann bei ungültigen Archiv- oder Payloaddaten intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="NotSupportedException">Kann bei nicht unterstützten Pfad-/I/O-Konstellationen intern auftreten und wird fail-closed behandelt.</exception>
        ''' <exception cref="ArgumentException">Kann bei ungültigen Argumenten intern auftreten und wird fail-closed behandelt.</exception>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String,
                overwrite As Boolean,
                secureExtract As Boolean
            ) As Boolean

            Dim opt = FileTypeOptions.GetSnapshot()

            ' Guard-Clauses: Null-, Größen- und Zielpfadprüfung.
            If data Is Nothing Then Return False

            If CLng(data.Length) > opt.MaxBytes Then
                LogGuard.Warn(opt.Logger, $"[Materialize] Daten zu gross ({data.Length} > {opt.MaxBytes}).")
                Return False
            End If

            If String.IsNullOrWhiteSpace(destinationPath) Then Return False

            ' Pfadnormalisierung: Absoluten Zielpfad auflösen.
            Dim destinationFull As String
            Dim errorMessage As String = Nothing

            Try
                destinationFull = Path.GetFullPath(destinationPath)

            Catch ex As Exception When _
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is UnauthorizedAccessException OrElse
                TypeOf ex Is System.Security.SecurityException OrElse
                TypeOf ex Is NotSupportedException OrElse
                TypeOf ex Is PathTooLongException OrElse
                TypeOf ex Is IOException

                LogGuard.Warn(opt.Logger, $"[Materialize] Ungueltiger Zielpfad: {errorMessage}")
                Return False
            End Try

            ' Secure-Extract-Branch: describe -> safety gate -> extract.
            If secureExtract Then
                Dim descriptor As ArchiveDescriptor = Nothing

                If ArchiveTypeResolver.TryDescribeBytes(data, opt, descriptor) Then
                    If Not ArchiveSafetyGate.IsArchiveSafeBytes(data, opt, descriptor) Then
                        LogGuard.Warn(opt.Logger, "[Materialize] Archiv-Validierung fehlgeschlagen.")
                        Return False
                    End If

                    Return MaterializeArchiveBytes(data, destinationFull, overwrite, opt, descriptor)
                End If

                ' Unlesbares Archiv trotz Signaturhinweis wird fail-closed abgelehnt.
                If ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(data) Then
                    LogGuard.Warn(opt.Logger, "[Materialize] Archiv kann nicht gelesen werden.")
                    Return False
                End If
            End If

            ' Raw-Fallback: Persistenz als Datei, wenn keine sichere Extraktion erfolgt.
            Return MaterializeRawBytes(data, destinationFull, overwrite, opt)
        End Function

        Private Shared Function MaterializeRawBytes _
            (
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions
            ) As Boolean

            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then _
                    Return False

                Dim parent = Path.GetDirectoryName(destinationFull)
                If String.IsNullOrWhiteSpace(parent) Then Return False
                Directory.CreateDirectory(parent)

                Using _
                    fs As _
                        New FileStream(destinationFull, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                       InternalIoDefaults.FileStreamBufferSize, FileOptions.SequentialScan)
                    fs.Write(data, 0, data.Length)
                End Using

                Return True

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is System.Security.SecurityException OrElse _
                TypeOf ex Is IOException OrElse _
                TypeOf ex Is InvalidDataException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is ArgumentException

                LogGuard.Error(opt.Logger, "[Materialize] Byte-Persistenz fehlgeschlagen.", ex)
                Return False
            End Try
        End Function

        Private Shared Function MaterializeArchiveBytes _
            (
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As FileTypeProjectOptions,
                descriptor As ArchiveDescriptor
            ) As Boolean

            Try
                If Not DestinationPathGuard.PrepareMaterializationTarget(destinationFull, overwrite, opt) Then _
                    Return False

                Using ms As New MemoryStream(data, writable:=False)
                    Return ArchiveExtractor.TryExtractArchiveStream(ms, destinationFull, opt, descriptor)
                End Using

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is System.Security.SecurityException OrElse _
                TypeOf ex Is IOException OrElse _
                TypeOf ex Is InvalidDataException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is ArgumentException

                LogGuard.Error(opt.Logger, "[Materialize] Archiv-Extraktion fehlgeschlagen.", ex)
                Return False
            End Try
        End Function
    End Class
End Namespace
