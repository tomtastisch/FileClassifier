' ============================================================================
' FILE: FileTypeProjectOptions.vb
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
    '''     Konfigurationsobjekt für Dateityp-Erkennung, Archivgrenzen und deterministische Hash-Optionen.
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Sicherheitsannahmen:
    '''         1) Grenzwerte sind konservativ gesetzt, um Ressourcenangriffe (z. B. ZIP-Bombs) zu reduzieren.
    '''         2) Der Logger darf ausschließlich Beobachtbarkeit liefern und keine Entscheidungslogik beeinflussen.
    '''     </para>
    '''     <para>
    '''         Die Instanz wird vor Verwendung normiert; nicht zulässige Minimalwerte werden auf sichere Untergrenzen angehoben.
    '''     </para>
    ''' </remarks>
    Public NotInheritable Class FileTypeProjectOptions
        Private Const MinPositiveLong As Long = 1
        Private Const MinPositiveInt As Integer = 1
        Private Const MinNonNegativeInt As Integer = 0
        Private Const NormalizedValueCount As Integer = 14
        Private Const ValueIndexMaxBytes As Integer = 0
        Private Const ValueIndexSniffBytes As Integer = 1
        Private Const ValueIndexMaxZipEntries As Integer = 2
        Private Const ValueIndexMaxZipTotalUncompressedBytes As Integer = 3
        Private Const ValueIndexMaxZipEntryUncompressedBytes As Integer = 4
        Private Const ValueIndexMaxZipCompressionRatio As Integer = 5
        Private Const ValueIndexMaxZipNestingDepth As Integer = 6
        Private Const ValueIndexMaxZipNestedBytes As Integer = 7
        Private Const ValueIndexRejectArchiveLinks As Integer = 8
        Private Const ValueIndexAllowUnknownArchiveEntrySize As Integer = 9
        Private Const ValueIndexHashIncludePayloadCopies As Integer = 10
        Private Const ValueIndexHashIncludeFastHash As Integer = 11
        Private Const ValueIndexHashIncludeSecureHash As Integer = 12
        Private Const ValueIndexHashMaterializedFileName As Integer = 13

        ''' <summary>
        '''     Erzwingt Header-only-Erkennung für Nicht-ZIP-Typen.
        '''     Sonderregel: ZIP-Container werden weiterhin sicher inhaltlich verfeinert (OOXML/ZIP).
        '''     Default ist True und read-only.
        ''' </summary>
        Public ReadOnly Property HeaderOnlyNonZip As Boolean

        ''' <summary>
        '''     Harte Obergrenze für Datei-/Byte-Payloads.
        '''     Alles darüber wird fail-closed verworfen.
        ''' </summary>
        Public Property MaxBytes As Long = 200L * 1024L * 1024L

        ''' <summary>
        '''     Maximale Header-Länge für Sniffing/Magic.
        ''' </summary>
        Public Property SniffBytes As Integer = 64 * 1024

        ''' <summary>Maximal erlaubte Anzahl ZIP-Entries.</summary>
        Public Property MaxZipEntries As Integer = 5000

        ''' <summary>Maximale Summe unkomprimierter ZIP-Bytes.</summary>
        Public Property MaxZipTotalUncompressedBytes As Long = 500L * 1024L * 1024L

        ''' <summary>Maximal erlaubte unkomprimierte Bytes pro ZIP-Entry.</summary>
        Public Property MaxZipEntryUncompressedBytes As Long = 200L * 1024L * 1024L

        ''' <summary>
        '''     Maximal erlaubtes Kompressionsverhältnis (u/c).
        '''     Dient als einfacher Schutz gegen stark komprimierte Bomben.
        ''' </summary>
        Public Property MaxZipCompressionRatio As Integer = 50

        ''' <summary>
        '''     Maximale Rekursionstiefe für verschachtelte ZIP-Dateien (0 = aus).
        ''' </summary>
        Public Property MaxZipNestingDepth As Integer = 2

        ''' <summary>
        '''     Harte In-Memory-Grenze für verschachtelte ZIP-Entries.
        ''' </summary>
        Public Property MaxZipNestedBytes As Long = 50L * 1024L * 1024L

        ''' <summary>
        '''     Standard-Policy: Link-Entries (symlink/hardlink) werden fail-closed verworfen.
        ''' </summary>
        Public Property RejectArchiveLinks As Boolean = True

        ''' <summary>
        '''     Erlaubt Archive-Entries mit unbekannter Größe nur bei explizitem Opt-In.
        '''     Default ist fail-closed (False).
        ''' </summary>
        Public Property AllowUnknownArchiveEntrySize As Boolean = False

        ''' <summary>Optionaler Logger für Diagnosezwecke.</summary>
        Public Property Logger As Microsoft.Extensions.Logging.ILogger = Nothing

        ''' <summary>
        '''     Optionen für deterministische Hash-/Evidence-Funktionen.
        ''' </summary>
        Public Property DeterministicHash As HashOptions = New HashOptions()

        ''' <summary>
        '''     Initialisiert eine neue Instanz mit sicheren Standardwerten.
        ''' </summary>
        ''' <remarks>
        '''     Diese öffentliche Initialisierung erzwingt <c>HeaderOnlyNonZip=True</c>.
        ''' </remarks>
        Public Sub New()
            Me.New(True)
        End Sub

        Friend Sub New(headerOnlyNonZipValue As Boolean)
            HeaderOnlyNonZip = headerOnlyNonZipValue
        End Sub

        Friend Function Clone() As FileTypeProjectOptions
            Dim cloned = New FileTypeProjectOptions(HeaderOnlyNonZip) With {
                        .MaxBytes = MaxBytes,
                        .SniffBytes = SniffBytes,
                        .MaxZipEntries = MaxZipEntries,
                        .MaxZipTotalUncompressedBytes = MaxZipTotalUncompressedBytes,
                        .MaxZipEntryUncompressedBytes = MaxZipEntryUncompressedBytes,
                        .MaxZipCompressionRatio = MaxZipCompressionRatio,
                        .MaxZipNestingDepth = MaxZipNestingDepth,
                        .MaxZipNestedBytes = MaxZipNestedBytes,
                        .RejectArchiveLinks = RejectArchiveLinks,
                        .AllowUnknownArchiveEntrySize = AllowUnknownArchiveEntrySize,
                        .Logger = Logger,
                        .DeterministicHash = HashOptions.Normalize(DeterministicHash)
                    }
            cloned.NormalizeInPlace()
            
            Return cloned
        End Function

        Friend Sub NormalizeInPlace()
            Dim normalizedValues() As Object      = Nothing
            Dim hashOptions        As HashOptions = HashOptions.Normalize(DeterministicHash)

            If CsCoreRuntimeBridge.TryNormalizeProjectOptionsValues(
                headerOnlyNonZip:=HeaderOnlyNonZip,
                maxBytes:=MaxBytes,
                sniffBytes:=SniffBytes,
                maxZipEntries:=MaxZipEntries,
                maxZipTotalUncompressedBytes:=MaxZipTotalUncompressedBytes,
                maxZipEntryUncompressedBytes:=MaxZipEntryUncompressedBytes,
                maxZipCompressionRatio:=MaxZipCompressionRatio,
                maxZipNestingDepth:=MaxZipNestingDepth,
                maxZipNestedBytes:=MaxZipNestedBytes,
                rejectArchiveLinks:=RejectArchiveLinks,
                allowUnknownArchiveEntrySize:=AllowUnknownArchiveEntrySize,
                hashIncludePayloadCopies:=hashOptions.IncludePayloadCopies,
                hashIncludeFastHash:=hashOptions.IncludeFastHash,
                hashIncludeSecureHash:=hashOptions.IncludeSecureHash,
                hashMaterializedFileName:=hashOptions.MaterializedFileName,
                normalizedValues:=normalizedValues
            ) Then
                If TryApplyNormalizedValues(normalizedValues) Then
                    Return
                End If
            End If

            MaxBytes = Max(MinPositiveLong, MaxBytes)
            SniffBytes = Max(MinPositiveInt, SniffBytes)
            MaxZipEntries = Max(MinPositiveInt, MaxZipEntries)
            MaxZipTotalUncompressedBytes = Max(MinPositiveLong, MaxZipTotalUncompressedBytes)
            MaxZipEntryUncompressedBytes = Max(MinPositiveLong, MaxZipEntryUncompressedBytes)
            MaxZipCompressionRatio = Max(MinNonNegativeInt, MaxZipCompressionRatio)
            MaxZipNestingDepth = Max(MinNonNegativeInt, MaxZipNestingDepth)
            MaxZipNestedBytes = Max(MinPositiveLong, MaxZipNestedBytes)
            DeterministicHash = hashOptions
        End Sub

        Private Function TryApplyNormalizedValues _
            (
                normalizedValues() As Object
            ) As Boolean

            Try
                If normalizedValues Is Nothing OrElse normalizedValues.Length <> NormalizedValueCount Then
                    Return False
                End If

                MaxBytes = CLng(normalizedValues(ValueIndexMaxBytes))
                SniffBytes = CInt(normalizedValues(ValueIndexSniffBytes))
                MaxZipEntries = CInt(normalizedValues(ValueIndexMaxZipEntries))
                MaxZipTotalUncompressedBytes = CLng(normalizedValues(ValueIndexMaxZipTotalUncompressedBytes))
                MaxZipEntryUncompressedBytes = CLng(normalizedValues(ValueIndexMaxZipEntryUncompressedBytes))
                MaxZipCompressionRatio = CInt(normalizedValues(ValueIndexMaxZipCompressionRatio))
                MaxZipNestingDepth = CInt(normalizedValues(ValueIndexMaxZipNestingDepth))
                MaxZipNestedBytes = CLng(normalizedValues(ValueIndexMaxZipNestedBytes))
                RejectArchiveLinks = CBool(normalizedValues(ValueIndexRejectArchiveLinks))
                AllowUnknownArchiveEntrySize = CBool(normalizedValues(ValueIndexAllowUnknownArchiveEntrySize))

                DeterministicHash = HashOptions.Normalize(
                    New HashOptions With {
                        .IncludePayloadCopies = CBool(normalizedValues(ValueIndexHashIncludePayloadCopies)),
                        .IncludeFastHash = CBool(normalizedValues(ValueIndexHashIncludeFastHash)),
                        .IncludeSecureHash = CBool(normalizedValues(ValueIndexHashIncludeSecureHash)),
                        .MaterializedFileName = CStr(normalizedValues(ValueIndexHashMaterializedFileName))
                    }
                )

                Return True
            Catch ex As Exception When _
                TypeOf ex Is InvalidCastException OrElse
                TypeOf ex Is OverflowException OrElse
                TypeOf ex Is IndexOutOfRangeException OrElse
                TypeOf ex Is ArgumentException OrElse
                TypeOf ex Is FormatException
                Return False
            End Try
        End Function

        Private Shared Function Max(minimum As Integer, value As Integer) As Integer
            If value < minimum Then Return minimum
            Return value
        End Function

        Private Shared Function Max(minimum As Long, value As Long) As Long
            If value < minimum Then Return minimum
            Return value
        End Function

        Friend Shared Function DefaultOptions() As FileTypeProjectOptions
            Dim options = New FileTypeProjectOptions()
            options.NormalizeInPlace()
            Return options
        End Function
    End Class
End Namespace
