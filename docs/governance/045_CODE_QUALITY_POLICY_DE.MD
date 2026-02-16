<!-- LANG_SWITCH:BEGIN -->
[DE](045_CODE_QUALITY_POLICY_DE.MD) | [EN](145_CODE_QUALITY_POLICY_DE.MD)
<!-- LANG_SWITCH:END -->

# Code-Quality- & Dokumentations-Policy (DE)
Status: verbindlich (Projekt-Policy)
Geltungsbereich: src/FileTypeDetection/* (VB.NET und C#), Public API + interne Implementierung

## 1. Zweck
Diese Policy definiert ein einheitliches, auditierbares Schema für:
- Code-Aufbau (Lesbarkeit, Semantik, Member-Ordnung)
- Fehlerbehandlung (fail-closed, konsistentes Logging)
- Dokumentation (deutsch, vollständig, mit echten Umlauten)
- reproduzierbare Codex-Änderungen ohne Verhaltensdrift

## 2. Normative Orientierung (DIN/ISO/IEC/IEEE)
Hinweis: Es existiert keine einzelne „DIN für Code-Formatierung“. Wir definieren hier ein internes Regelwerk,
das sich an etablierten Normen für Informationsqualität und Softwarequalität orientiert:

- ISO/IEC/IEEE 26514:2022-01 — Systems and software engineering: Design and development of information for users
  Quelle: https://www.dinmedia.de/en/standard/iso-iec-ieee-26514/350342422

- ISO/IEC 25010:2023 — Systems and software engineering: Product quality model (SQuaRE)
  Quelle: ISO-Katalogeintrag 78176 (über die ISO-Suche abrufbar)

- ISO/IEC/IEEE 15289:2019 — Content of life-cycle information items (documentation)
  Quelle: ISO-Katalogeintrag 74909 (über die ISO-Suche abrufbar)

Optional (für generische Nutzungsinformationen):
- DIN EN IEC/IEEE 82079-1:2021-09 — Erstellung von Nutzungsinformationen (Gebrauchsanleitungen) für Produkte
  Quelle: https://www.dinmedia.de/en/standard/din-en-iec-ieee-82079-1/342226844

## 3. Grundsätze (hart)
- Keine Umschreibung deutscher Umlaute: ä ö ü Ä Ö Ü ß (keine ae/oe/ue/ss).
- Dateien bleiben UTF-8; keine Encoding-Konvertierung.
- Keine Behavior-Änderungen durch Formatierung/Doku:
    - keine Signaturänderungen
    - keine Semantikänderungen in Exceptions/Returns/Side effects
    - keine neuen externen Dependencies

## 4. Shared vs. Instanz (Konstruktionsregel)
### 4.1 Shared (Utility) ist erlaubt, wenn ALLE Punkte erfüllt sind
- Typ ist zustandslos (stateless)
- keine Dependency Injection / keine ersetzbaren Abhängigkeiten notwendig
- kein Polymorphismus/Test-Doubles über Interfaces erforderlich
- typisch: NotInheritable + Private Sub New() + ausschließlich Shared Members

### 4.2 Instanz-/Service-Typ ist Pflicht, wenn mindestens ein Punkt zutrifft
- Abhängigkeiten sollen injizierbar/tauschbar sein (I/O, Logger, Policies, Clock, Provider, etc.)
- Testbarkeit via DI/Interfaces ist erforderlich
- Konfiguration/Zustand pro Instanz ist fachlich sinnvoll

Wichtig: Im Rahmen von Codex-Format-/Doku-Überarbeitung werden Typ-Arten NICHT geändert,
außer der Typ ist bereits eindeutig als Utility/Service etabliert.

## 5. Datei- und Type-Struktur (Pflicht-Reihenfolge)
### 5.1 Datei-Reihenfolge
1) Datei-Header-Kommentar (Policy-/Kontextblock, keine XML-Docs)
2) Option Strict On / Option Explicit On (VB)
3) Imports
4) Namespace
5) Types

### 5.2 Type-Layout (Member-Ordnung)
1) XML-Doku am Type (summary + remarks)
2) Konstanten (Const) / Shared ReadOnly
3) Felder/Properties (nur wenn Typzustand existiert)
4) Konstruktor(en)
5) Public API (alle Public Members, geordnet)
6) Internal/Private Helpers
7) Nested Types (nur wenn nötig; am Ende)

### 5.3 Semantische Blöcke in Funktionen
Blöcke werden sichtbar getrennt (Leerzeile + Kommentartrenner):
- Options/Snapshot
- Guard-Clauses (fail-closed)
- Normalisierung/Canonicalization (z.B. Pfad)
- Branches (z.B. SecureExtract)
- Fallback
- I/O Helpers separat

## 6. Variablenregel (Pflicht)
- Alle lokalen Variablen werden im „Deklarationsblock“ am Anfang der Funktion definiert.
- Platzierung:
    - funktionslokal, wenn nur dort benötigt
    - als Klassenvariable nur, wenn über mehrere Member sinnvoll geteilt wird
- Alignment: spaltenartig (Tabs/Spaces), so dass Dim / Name / As / Initialwert vertikal ausgerichtet sind.
- Ausnahme: Using-/For-Header dürfen lokale Variablen enthalten, wenn das idiomatisch ist und Lesbarkeit erhöht;
  Default bleibt: zentraler Deklarationsblock.

## 6.1 Namenskonventionen (Pflicht, VB.NET + C#)

Ziel:
- Einheitliche Benennung für Lesbarkeit, Konsistenz und reduzierte IDE-/Analyser-Warnungen.
- Keine „Mischformen“ (z.B. camelCase in Public API, snake_case bei lokalen Variablen, etc.).

Grundsatz:
- Public API ist ein Vertrag: Public/Protected müssen strikt konventionskonform sein.
- Private/Local folgt ebenfalls dem Standard, außer es gibt technische Gründe (z.B. Interop/externes Schema).

### 6.1.1 Allgemeine Regeln (beide Sprachen)
- Keine deutschen Umlaute in Bezeichnernamen (Identifiers). Umlaute sind nur in Kommentaren/Dokumentation erlaubt.
- Keine Unterstriche in Public API-Namen (Ausnahme: echte Interop-/Extern-Verträge, die so vorgegeben sind).
- Keine unklaren Abkürzungen:
    - Erlaubt: IO, URI, URL, HTTP, JSON, XML, SHA256, HMAC, ZIP, TAR, RFC, GUID, UTF8 (als bekannte Akronyme)
    - Vermeiden: „mgr“, „proc“, „util“, „tmp“ in Public API
- Akronyme:
    - PascalCase bleibt erhalten: „HttpClient“, „ZipArchive“, „UriBuilder“
    - Keine ALLCAPS innerhalb eines Wortes außer etablierte Typen/Namen erzwingen es.

### 6.1.2 VB.NET Naming (Option Strict/Explicit On)
VB.NET-Konventionen (Projektstandard):
- Namespace / Type / Member:
    - Namespace: PascalCase
    - Class/Module/Structure/Enum/Interface: PascalCase
    - Public/Protected Methods/Functions/Properties/Events: PascalCase
    - Public/Protected Constants: PascalCase
- Parameter:
    - camelCase (z.B. destinationPath, overwrite, secureExtract)
- Lokale Variablen:
    - camelCase (z.B. destinationFull, descriptor, parent)
- Private Fields:
    - camelCase mit Präfix "_" (underscore) ist erlaubt und empfohlen, wenn Felder existieren:
        - Beispiel: _logger, _options
    - Falls dein Repo bereits einen anderen Standard nutzt (z.B. "m_" Präfix), dann gilt SSOT = bestehender Repo-Standard.
- Boolean-Namen:
    - Keine negativen Namen in Public API („NotX“), bevorzugt positive Bedeutung:
        - IsEnabled statt NotDisabled
    - Lokale Booleans beginnen mit is/has/can/should:
        - isOk, hasValue, canExtract, shouldOverwrite
- Asynchronität (falls vorhanden):
    - Async-Suffix nur, wenn es echte Async/Task-Semantik gibt (in VB/C# analog)
    - Kein Async-Suffix bei synchronen Methoden.

### 6.1.3 C# Naming (falls src/FileTypeDetection auch C# enthält)
C#-Konventionen (Projektstandard):
- Namespace / Type / Member:
    - Namespace: PascalCase
    - Class/Struct/Record/Enum/Interface: PascalCase (Interfaces mit I-Präfix, z.B. IArchiveExtractor)
    - Public/Protected Methods/Properties/Events: PascalCase
    - Private Fields: _camelCase
    - Const: PascalCase (oder ALL_CAPS nur wenn Repo das bereits so macht; SSOT = bestehender Repo-Standard)
- Parameter und Locals:
    - camelCase
- Boolean-Namen:
    - is/has/can/should (z.B. isSafe, hasSignature, canExtract)

### 6.1.4 Unterstrich-Regeln (wann "_" erlaubt ist)
- Public API:
    - Keine "_" in Method/Property/Type-Namen.
- Private Fields:
    - "_" Präfix ist erlaubt/empfohlen (_logger).
- Lokale Variablen/Parameter:
    - Kein "_" Präfix, außer zur Konfliktauflösung (z.B. @class in C# statt _class; in VB entsprechend vermeiden).
- Methoden-Namen:
    - Kein "_" innerhalb des Namens (z.B. nicht Try_NormalizePath).

### 6.1.5 DoD-Erweiterung (Naming)
- Keine neuen IDE-Warnungen durch Naming-Drift (insb. Public API).
- Neue oder angepasste Bezeichner entsprechen dieser Konvention.
- Falls bestehende Namen abweichen:
    - In diesem Ticket nur korrigieren, wenn es keine API-/Semantikänderung erzeugt und eindeutig „Style-only“ ist.
    - Public API-Namen werden NICHT umbenannt, wenn das einen Breaking Change bedeuten würde.

## 7. Exception-Handling & Logging (Fail-Closed)
- Catch-Filter-Form bevorzugt (VB):
  Catch ex As Exception When TypeOf ex Is ...
- Verboten ist der redundante Pseudo-Filter:
  Catch ex As Exception When TypeOf ex Is Exception
  Wenn alle Exceptions behandelt werden sollen, ist stattdessen `Catch ex As Exception` zu verwenden.
- Keine catch-all ohne Filter, außer wenn zwingend notwendig UND dokumentiert.
- Keine stillen Swallows: bei Fehler -> loggen (Warn/Error) + fail-closed Rückgabe/Exception wie bisher.
- Log-Text ist konsistent, deutsch, mit Umlauten.
- Keine Änderung der Exception-Semantik (kein Wrapping, keine neuen Throws).

## 8. Dokumentationsstandard (Public API)
### 8.1 Sprache/Zeichen
- Deutsch, echte Umlaute.
- Keine maschinelle Umschreibung.

### 8.2 Mindestumfang pro Public Type/Member
- Type:
    - <summary>: 1–3 Sätze Zweck/Scope
    - <remarks>: strukturierte Abschnitte:
      Zweck, Verantwortlichkeiten, Nicht-Ziele, Nebenwirkungen, Fehlerfälle, Security-Hinweise, Threading (falls relevant)
- Public Functions/Methods:
    - <summary>, <param> (alle), <returns> (immer)
    - <exception> nur wenn die API tatsächlich Exceptions nach außen propagiert; sonst über „fail-closed“ in remarks beschreiben.
    - <example> wenn sinnvoll (kurz, korrekt)

### 8.3 „DIN-/Norm-orientiert“ bedeutet konkret
- klare Begriffe, keine schwammigen Formulierungen („macht Dinge“)
- konsistente Terminologie (z.B. „Materialisierung“, „Normalisierung“, „Archivprüfung“)
- eindeutig dokumentierte Fehlerpfade und Nebenwirkungen

## 9. Definition of Done (DoD)
- Für jedes File in src/FileTypeDetection/*:
    - Layout entspricht Abschnitt 5–7
    - Public API ist nach Abschnitt 8 dokumentiert
    - Umlaute bleiben korrekt (kein ae/oe/ue)
    - Build/Test erfolgreich
    - Keine Public Signatures geändert
    - Kein Behavior-Drift (keine neuen Dependencies, keine Semantikänderung)

## 10. STYLE REFERENCE (SSOT)
- Der folgende Codeblock ist die verbindliche Style-/Semantik-Referenz für Aufbau, Umbrüche, Alignment,
  Signaturformat und Try/Catch-Struktur.
- Wende dieses Schema auf alle Dateien unter src/FileTypeDetection/* an.
- Abweichungen nur, wenn technisch zwingend und dann im Diff-Summary begründen.

### REFERENCE CODE (do not change; layout-template only):
```
' ============================================================================
' FILE: ExampleMaterializer.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, als Regelwerk)
' Normative Orientierung (Informationsstruktur / Qualität):
' - ISO/IEC/IEEE 26514:2022-01 (User-/API-Informationen)
' - ISO/IEC 25010 (Qualitätsmodell; hier angewandt auf Maintainability/Usability/Robustness)
'
' DATEI-STRUKTUR (Pflicht-Reihenfolge)
'  1) Option Strict/Explicit
'  2) Imports
'  3) Namespace
'  4) Types in Reihenfolge:
'     4.1) Type-Header (''' summary + remarks)
'     4.2) Const / Shared ReadOnly
'     4.3) Felder/Properties (nur bei Instanztyp)
'     4.4) Konstruktor(en)
'     4.5) Public API
'     4.6) Private Helpers (I/O)
'     4.7) Guards/Policy
'     4.8) Nested Types (am Ende, nur falls nötig)
'
' SHARED VS. INSTANZ (harte Regel)
' - Shared NUR bei stateless Utility:
'   - NotInheritable + Private Sub New()
'   - keine Dependency Injection erforderlich
'   - kein Polymorphismus/Test-Doubles via Interfaces
'
' - Instanztyp (Service) wenn:
'   - Abhängigkeiten injizierbar sein müssen (Logger, Policy, I/O, Clock, Provider, …)
'   - Testbarkeit via Interfaces notwendig ist
'   - Zustand/Konfiguration pro Instanz sinnvoll ist
'
' VARIABLENREGEL (hart)
' - Alle Dim-Deklarationen im Deklarationsblock am Anfang der Funktion.
' - Spaltenartig ausgerichtet (Dim / Name / As / Initialwert), mittels Tabs/Spaces.
' ============================================================================

Option Strict On
Option Explicit On

Imports System
Imports System.IO

Namespace Global.Tomtastisch.FileClassifier

    ''' <summary>
    '''     Einheitliche, fail-closed Materialisierung von Byte-Daten auf ein Ziel (Datei oder Verzeichnis).
    ''' </summary>
    ''' <remarks>
    '''     Zweck:
    '''     - Persistiert Bytes deterministisch als Datei.
    '''     - Optional: sichere Archiv-Extraktion (wenn Archiv erkannt und als sicher bewertet).
    '''
    '''     Verantwortlichkeiten:
    '''     - Guard-Clauses (Null/Größe/Pfad).
    '''     - Pfad-Normalisierung (Canonical Path).
    '''     - Delegation an Rohbytes- oder Archiv-Materialisierung.
    '''
    '''     Fehlerverhalten:
    '''     - Fail-closed: Rückgabe False bei ungültigen Eingaben oder I/O-Fehlern.
    '''     - Exceptions werden innerhalb der I/O-Helfer behandelt (Logging + False).
    ''' </remarks>
    Public NotInheritable Class ExampleMaterializer

        ''' <summary>
        '''     Verhindert Instanziierung (Utility-Typ).
        ''' </summary>
        Private Sub New()
        End Sub


        ' =====================================================================
        ' Public API (Shared; Utility, stateless)
        ' =====================================================================

        ''' <summary>
        '''     Persistiert <paramref name="data"/> an <paramref name="destinationPath"/> ohne Overwrite und ohne Extraktion.
        ''' </summary>
        ''' <param name="data">Die zu persistierenden Bytes (Nothing => False).</param>
        ''' <param name="destinationPath">Zielpfad zur Datei (leer/whitespace => False).</param>
        ''' <returns>True bei erfolgreicher Materialisierung, sonst False (fail-closed).</returns>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String
            ) As Boolean

            ' Deklarationsblock (Pflicht, spaltenartig)
            Dim overwrite            As Boolean = False
            Dim secureExtract        As Boolean = False

            Return Persist(data, destinationPath, overwrite, secureExtract)

        End Function

        ''' <summary>
        '''     Persistiert <paramref name="data"/> an <paramref name="destinationPath"/> mit optionalem Overwrite.
        ''' </summary>
        ''' <param name="data">Die zu persistierenden Bytes (Nothing => False).</param>
        ''' <param name="destinationPath">Zielpfad zur Datei (leer/whitespace => False).</param>
        ''' <param name="overwrite">True erlaubt das Überschreiben/Ersetzen eines bestehenden Ziels.</param>
        ''' <returns>True bei erfolgreicher Materialisierung, sonst False (fail-closed).</returns>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String,
                overwrite As Boolean
            ) As Boolean

            ' Deklarationsblock (Pflicht, spaltenartig)
            Dim secureExtract        As Boolean = False

            Return Persist(data, destinationPath, overwrite, secureExtract)

        End Function

        ''' <summary>
        '''     Persistiert Bytes als Datei oder extrahiert optional ein als sicher bewertetes Archiv.
        ''' </summary>
        ''' <remarks>
        '''     Ablauf (Schema):
        '''     1) Options/Snapshot laden
        '''     2) Guard-Clauses (data/size/path)
        '''     3) Zielpfad normalisieren (TryNormalizePath)
        '''     4) Branch: secureExtract (Describe -> SafetyGate -> Extract)
        '''     5) Fallback: Rohbytes schreiben
        '''
        '''     Fail-Closed:
        '''     - Bei ungültiger Eingabe, unsicherem Archiv oder I/O-Fehlern wird False zurückgegeben.
        ''' </remarks>
        ''' <param name="data">Die zu verarbeitenden Bytes.</param>
        ''' <param name="destinationPath">Zielpfad (Datei oder Zielordner bei Extraktion).</param>
        ''' <param name="overwrite">Overwrite-Regel.</param>
        ''' <param name="secureExtract">True aktiviert Archivprüfung und Extraktion (wenn möglich).</param>
        ''' <returns>True bei Erfolg, sonst False (fail-closed).</returns>
        Public Shared Function Persist _
            (
                data As Byte(),
                destinationPath As String,
                overwrite As Boolean,
                secureExtract As Boolean
            ) As Boolean

            ' Deklarationsblock (Pflicht, spaltenartig)
            Dim opt                 As MaterializerOptions = GetOptionsSnapshot()
            Dim destinationFull     As String              = Nothing

            Dim isOk                As Boolean             = False
            Dim canExtract          As Boolean             = False


            ' -----------------------------------------------------------------
            ' Guard-Clauses (fail-closed)
            ' -----------------------------------------------------------------
            If data Is Nothing Then Return False

            If CLng(data.Length) > opt.MaxBytes Then
                opt.LogWarn($"[Materialize] Daten zu groß ({data.Length} > {opt.MaxBytes}).")
                Return False
            End If

            If String.IsNullOrWhiteSpace(destinationPath) Then Return False


            ' -----------------------------------------------------------------
            ' Pfad-Normalisierung / Zielermittlung
            ' -----------------------------------------------------------------
            isOk = TryNormalizePath(destinationPath, destinationFull, opt)
            If Not isOk Then Return False


            ' -----------------------------------------------------------------
            ' Optional: Secure Extract (nur wenn angefordert)
            ' -----------------------------------------------------------------
            If secureExtract Then

                Dim descriptor         As String = Nothing

                canExtract = TryDescribeArchive(data, descriptor, opt)
                If canExtract Then

                    If Not IsArchiveSafe(data, descriptor, opt) Then
                        opt.LogWarn("[Materialize] Archiv-Validierung fehlgeschlagen.")
                        Return False
                    End If

                    Return MaterializeArchiveBytes(data, destinationFull, overwrite, opt, descriptor)

                End If

            End If


            ' -----------------------------------------------------------------
            ' Fallback: Rohbytes (wenn kein Extract möglich/gewünscht)
            ' -----------------------------------------------------------------
            Return MaterializeRawBytes(data, destinationFull, overwrite, opt)

        End Function



        ' =====================================================================
        ' Private Helpers (I/O + Error-Handling konsistent)
        ' =====================================================================

        ''' <summary>
        '''     Persistiert Bytes als Datei an <paramref name="destinationFull"/>.
        ''' </summary>
        ''' <remarks>
        '''     I/O-Helfer:
        '''     - Behandelt I/O-Exceptions intern (Logging + False).
        '''     - Propagiert keine Exceptions nach außen (fail-closed).
        ''' </remarks>
        ''' <param name="data">Die zu schreibenden Bytes.</param>
        ''' <param name="destinationFull">Normalisierter Zielpfad (muss nicht leer sein).</param>
        ''' <param name="overwrite">Overwrite-Regel.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <returns>True bei Erfolg, sonst False.</returns>
        Private Shared Function MaterializeRawBytes _
            (
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As MaterializerOptions
            ) As Boolean

            ' Deklarationsblock
            Dim parent              As String = Nothing

            Try

                If Not PrepareTarget(destinationFull, overwrite, opt) Then _
                    Return False

                parent = Path.GetDirectoryName(destinationFull)
                If String.IsNullOrWhiteSpace(parent) Then Return False

                Directory.CreateDirectory(parent)

                Using _
                    fs As _
                        New FileStream(destinationFull,
                                       FileMode.CreateNew,
                                       FileAccess.Write,
                                       FileShare.None,
                                       bufferSize:=64 * 1024,
                                       options:=FileOptions.SequentialScan)

                    fs.Write(data, 0, data.Length)

                End Using

                Return True

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is System.Security.SecurityException OrElse _
                TypeOf ex Is IOException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is ArgumentException

                opt.LogError("[Materialize] Byte-Persistenz fehlgeschlagen.", ex)
                Return False

            End Try

        End Function

        ''' <summary>
        '''     Extrahiert ein Archiv aus <paramref name="data"/> nach <paramref name="destinationFull"/>.
        ''' </summary>
        ''' <remarks>
        '''     I/O-Helfer:
        '''     - Erwartet, dass Archivtyp bereits erkannt und als sicher bewertet wurde.
        '''     - Behandelt I/O-Fehler intern (Logging + False).
        ''' </remarks>
        ''' <param name="data">Archivdaten als Bytes.</param>
        ''' <param name="destinationFull">Zielordner (normalisiert).</param>
        ''' <param name="overwrite">Overwrite-Regel.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <param name="descriptor">Archiv-Descriptor (z.B. Typ/Signatur).</param>
        ''' <returns>True bei erfolgreicher Extraktion, sonst False.</returns>
        Private Shared Function MaterializeArchiveBytes _
            (
                data As Byte(),
                destinationFull As String,
                overwrite As Boolean,
                opt As MaterializerOptions,
                descriptor As String
            ) As Boolean

            ' Deklarationsblock
            Dim ms                  As MemoryStream = Nothing

            Try

                If Not PrepareTarget(destinationFull, overwrite, opt) Then _
                    Return False

                ms = New MemoryStream(data, writable:=False)
                Using ms
                    Return FakeExtract(ms, destinationFull, descriptor, opt)
                End Using

            Catch ex As Exception When _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is System.Security.SecurityException OrElse _
                TypeOf ex Is IOException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is ArgumentException

                opt.LogError("[Materialize] Archiv-Extraktion fehlgeschlagen.", ex)
                Return False

            End Try

        End Function



        ' =====================================================================
        ' Local Policy/Guards (keine I/O-Details, nur Regelwerk)
        ' =====================================================================

        ''' <summary>
        '''     Liefert einen konsistenten Options-Snapshot (keine Side-Effects).
        ''' </summary>
        ''' <remarks>
        '''     Erwartung:
        '''     - Der Snapshot darf keine I/O- oder Environment-Side-Effects auslösen.
        '''     - Wird früh in Public API geladen, um Logging/MaxBytes konsistent zu haben.
        ''' </remarks>
        ''' <returns>Options-Snapshot für diese Operation.</returns>
        Private Shared Function GetOptionsSnapshot() As MaterializerOptions

            ' Deklarationsblock
            Dim maxBytes             As Long = 10_000_000

            Return New MaterializerOptions(maxBytes:=maxBytes)

        End Function

        ''' <summary>
        '''     Normalisiert <paramref name="destinationPath"/> fail-closed zu einem kanonischen Pfad.
        ''' </summary>
        ''' <remarks>
        '''     Fehlerverhalten:
        '''     - Gibt False zurück, wenn Pfad nicht normalisierbar ist.
        '''     - Loggt Warnungen mit Exception-Message.
        ''' </remarks>
        ''' <param name="destinationPath">Eingabepfad (nicht leer).</param>
        ''' <param name="destinationFull">Ausgabe: normalisierter Vollpfad oder Nothing.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <returns>True, wenn Normalisierung erfolgreich war, sonst False.</returns>
        Private Shared Function TryNormalizePath _
            (
                destinationPath As String,
                ByRef destinationFull As String,
                opt As MaterializerOptions
            ) As Boolean

            ' Deklarationsblock
            Dim normalized           As String = Nothing

            Try

                normalized = Path.GetFullPath(destinationPath)
                destinationFull = normalized
                Return True

            Catch ex As Exception When _
                TypeOf ex Is ArgumentException OrElse _
                TypeOf ex Is UnauthorizedAccessException OrElse _
                TypeOf ex Is System.Security.SecurityException OrElse _
                TypeOf ex Is NotSupportedException OrElse _
                TypeOf ex Is PathTooLongException OrElse _
                TypeOf ex Is IOException

                opt.LogWarn($"[Materialize] Ungültiger Zielpfad: {ex.Message}")
                destinationFull = Nothing
                Return False

            End Try

        End Function

        ''' <summary>
        '''     Bereitet den Materialisierungs-Target gemäß Overwrite-Regel vor.
        ''' </summary>
        ''' <remarks>
        '''     Hinweis:
        '''     - In echten Implementierungen: Existenzprüfung, Overwrite-Policy, Zielvalidierung.
        '''     - Darf keine Exceptions nach außen propagieren (fail-closed im Aufrufer).
        ''' </remarks>
        ''' <param name="destinationFull">Normalisierter Zielpfad.</param>
        ''' <param name="overwrite">Overwrite-Regel.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <returns>True, wenn Ziel vorbereitet ist, sonst False.</returns>
        Private Shared Function PrepareTarget _
            (
                destinationFull As String,
                overwrite As Boolean,
                opt As MaterializerOptions
            ) As Boolean

            ' Deklarationsblock
            Dim isValid              As Boolean = True

            Return isValid

        End Function

        ''' <summary>
        '''     Versucht, Archiv-Metadaten/Typ aus <paramref name="data"/> zu bestimmen.
        ''' </summary>
        ''' <remarks>
        '''     Vertrag:
        '''     - Setzt <paramref name="descriptor"/> nur bei Erfolg.
        '''     - Liefert False, wenn keine Archivstruktur erkannt wird.
        ''' </remarks>
        ''' <param name="data">Eingabebytes.</param>
        ''' <param name="descriptor">Ausgabe: Descriptor oder Nothing.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <returns>True bei erkannter Archivstruktur, sonst False.</returns>
        Private Shared Function TryDescribeArchive _
            (
                data As Byte(),
                ByRef descriptor As String,
                opt As MaterializerOptions
            ) As Boolean

            ' Deklarationsblock
            Dim recognized           As Boolean = False

            descriptor = Nothing
            Return recognized

        End Function

        ''' <summary>
        '''     Bewertet ein erkanntes Archiv als sicher oder unsicher.
        ''' </summary>
        ''' <remarks>
        '''     Security:
        '''     - In echten Implementierungen: ZipSlip-Prüfung, Pfadtraversal, Größenlimits, Bomb-Detektion.
        '''     - Fail-closed: unsicher => False.
        ''' </remarks>
        ''' <param name="data">Archivbytes.</param>
        ''' <param name="descriptor">Archiv-Descriptor.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <returns>True wenn sicher, sonst False.</returns>
        Private Shared Function IsArchiveSafe _
            (
                data As Byte(),
                descriptor As String,
                opt As MaterializerOptions
            ) As Boolean

            ' Deklarationsblock
            Dim safe                As Boolean = True

            Return safe

        End Function

        ''' <summary>
        '''     Beispiel-Extraktion (Platzhalter).
        ''' </summary>
        ''' <remarks>
        '''     In realem Code delegiert dies an einen Archiv-Extraktor.
        '''     </remarks>
        ''' <param name="ms">Archivstream.</param>
        ''' <param name="destinationFull">Zielordner.</param>
        ''' <param name="descriptor">Archiv-Descriptor.</param>
        ''' <param name="opt">Options-/Logging-Kontext.</param>
        ''' <returns>True bei Erfolg, sonst False.</returns>
        Private Shared Function FakeExtract _
            (
                ms As MemoryStream,
                destinationFull As String,
                descriptor As String,
                opt As MaterializerOptions
            ) As Boolean

            ' Deklarationsblock
            Dim ok                  As Boolean = True

            Return ok

        End Function



        ' =====================================================================
        ' Minimaler Options-Typ (nur Beispiel)
        ' =====================================================================

        ''' <summary>
        '''     Minimaler Options-/Logging-Container (Beispiel).
        ''' </summary>
        Private NotInheritable Class MaterializerOptions

            ''' <summary>
            '''     Maximale akzeptierte Byte-Anzahl.
            ''' </summary>
            Public ReadOnly MaxBytes As Long

            ''' <summary>
            '''     Erstellt einen neuen Options-Container.
            ''' </summary>
            ''' <param name="maxBytes">Maximale Byte-Anzahl.</param>
            Public Sub New(maxBytes As Long)
                Me.MaxBytes = maxBytes
            End Sub

            ''' <summary>
            '''     Loggt eine Warnung.
            ''' </summary>
            ''' <param name="message">Warntext.</param>
            Public Sub LogWarn(message As String)
                ' placeholder
            End Sub

            ''' <summary>
            '''     Loggt einen Fehler inkl. Exception.
            ''' </summary>
            ''' <param name="message">Fehlertext.</param>
            ''' <param name="ex">Exception.</param>
            Public Sub LogError(message As String, ex As Exception)
                ' placeholder
            End Sub

        End Class

    End Class

End Namespace
```

## 11. Policy/RoC-Mapping (CI)
Diese Policy ist in der CI-RoC-Matrix auf folgende Regeldateien gemappt:
- `tools/ci/policies/rules/artifact_contract.yaml`
- `tools/ci/policies/rules/docs_drift.yaml`
- `tools/ci/policies/rules/naming_snt.yaml`
- `tools/ci/policies/rules/shell_safety.yaml`
- `tools/ci/policies/rules/versioning_svt.yaml`
