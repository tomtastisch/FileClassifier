Option Strict On
Option Explicit On

Imports Microsoft.Extensions.Logging

Namespace FileTypeDetection

    ''' <summary>
    ''' Konfiguration fuer Dateityp-Erkennung und ZIP-Sicherheitsgrenzen.
    '''
    ''' Sicherheitsannahmen:
    ''' - Grenzen sind konservativ, um Memory-/CPU-DoS (z. B. Zip-Bomb) zu reduzieren.
    ''' - Logger darf Beobachtbarkeit liefern, aber niemals das Ergebnis beeinflussen.
    ''' </summary>
    Public NotInheritable Class FileTypeProjectOptions
        Private Const MinPositiveLong As Long = 1
        Private Const MinPositiveInt As Integer = 1
        Private Const MinNonNegativeInt As Integer = 0

        ''' <summary>
        ''' Erzwingt Header-only-Erkennung fuer Nicht-ZIP-Typen.
        ''' Sonderregel: ZIP-Container werden weiterhin sicher inhaltlich verfeinert (OOXML/ZIP).
        ''' Default ist True und read-only.
        ''' </summary>
        Public ReadOnly Property HeaderOnlyNonZip As Boolean

        ''' <summary>
        ''' Harte Obergrenze fuer Datei-/Byte-Payloads.
        ''' Alles darueber wird fail-closed verworfen.
        ''' </summary>
        Public Property MaxBytes As Long = 200L * 1024L * 1024L

        ''' <summary>
        ''' Maximale Header-Laenge fuer Sniffing/Magic.
        ''' </summary>
        Public Property SniffBytes As Integer = 64 * 1024

        ''' <summary>Maximal erlaubte Anzahl ZIP-Entries.</summary>
        Public Property MaxZipEntries As Integer = 5000

        ''' <summary>Maximale Summe unkomprimierter ZIP-Bytes.</summary>
        Public Property MaxZipTotalUncompressedBytes As Long = 500L * 1024L * 1024L

        ''' <summary>Maximal erlaubte unkomprimierte Bytes pro ZIP-Entry.</summary>
        Public Property MaxZipEntryUncompressedBytes As Long = 200L * 1024L * 1024L

        ''' <summary>
        ''' Maximal erlaubtes Kompressionsverhaeltnis (u/c).
        ''' Dient als einfacher Schutz gegen stark komprimierte Bomben.
        ''' </summary>
        Public Property MaxZipCompressionRatio As Integer = 50

        ''' <summary>
        ''' Maximale Rekursionstiefe fuer verschachtelte ZIP-Dateien (0 = aus).
        ''' </summary>
        Public Property MaxZipNestingDepth As Integer = 2

        ''' <summary>
        ''' Harte In-Memory-Grenze fuer verschachtelte ZIP-Entries.
        ''' </summary>
        Public Property MaxZipNestedBytes As Long = 50L * 1024L * 1024L

        ''' <summary>
        ''' Standard-Policy: Link-Entries (symlink/hardlink) werden fail-closed verworfen.
        ''' </summary>
        Public Property RejectArchiveLinks As Boolean = True

        ''' <summary>
        ''' Erlaubt Archive-Entries mit unbekannter Groesse nur bei explizitem Opt-In.
        ''' Default ist fail-closed (False).
        ''' </summary>
        Public Property AllowUnknownArchiveEntrySize As Boolean = False

        ''' <summary>Optionaler Logger fuer Diagnosezwecke.</summary>
        Public Property Logger As ILogger = Nothing

        ''' <summary>
        ''' Optionen fuer deterministische Hash-/Evidence-Funktionen.
        ''' </summary>
        Public Property DeterministicHash As DeterministicHashOptions = New DeterministicHashOptions()

        Public Sub New()
            Me.New(True)
        End Sub

        Friend Sub New(headerOnlyNonZip As Boolean)
            Me.HeaderOnlyNonZip = headerOnlyNonZip
        End Sub

        Friend Function Clone() As FileTypeProjectOptions
            Dim cloned = New FileTypeProjectOptions(Me.HeaderOnlyNonZip) With {
                .MaxBytes = Me.MaxBytes,
                .SniffBytes = Me.SniffBytes,
                .MaxZipEntries = Me.MaxZipEntries,
                .MaxZipTotalUncompressedBytes = Me.MaxZipTotalUncompressedBytes,
                .MaxZipEntryUncompressedBytes = Me.MaxZipEntryUncompressedBytes,
                .MaxZipCompressionRatio = Me.MaxZipCompressionRatio,
                .MaxZipNestingDepth = Me.MaxZipNestingDepth,
                .MaxZipNestedBytes = Me.MaxZipNestedBytes,
                .RejectArchiveLinks = Me.RejectArchiveLinks,
                .AllowUnknownArchiveEntrySize = Me.AllowUnknownArchiveEntrySize,
                .Logger = Me.Logger,
                .DeterministicHash = DeterministicHashOptions.Normalize(Me.DeterministicHash)
            }
            cloned.NormalizeInPlace()
            Return cloned
        End Function

        Friend Sub NormalizeInPlace()
            MaxBytes = Max(MinPositiveLong, MaxBytes)
            SniffBytes = Max(MinPositiveInt, SniffBytes)
            MaxZipEntries = Max(MinPositiveInt, MaxZipEntries)
            MaxZipTotalUncompressedBytes = Max(MinPositiveLong, MaxZipTotalUncompressedBytes)
            MaxZipEntryUncompressedBytes = Max(MinPositiveLong, MaxZipEntryUncompressedBytes)
            MaxZipCompressionRatio = Max(MinNonNegativeInt, MaxZipCompressionRatio)
            MaxZipNestingDepth = Max(MinNonNegativeInt, MaxZipNestingDepth)
            MaxZipNestedBytes = Max(MinPositiveLong, MaxZipNestedBytes)
            DeterministicHash = DeterministicHashOptions.Normalize(DeterministicHash)
        End Sub

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
