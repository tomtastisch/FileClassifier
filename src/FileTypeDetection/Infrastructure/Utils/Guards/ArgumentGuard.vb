' ============================================================================
' FILE: ArgumentGuard.vb
'
' INTERNE POLICY (DIN-/Norm-orientiert, verbindlich)
' - Datei- und Type-Struktur gemäß docs/governance/045_CODE_QUALITY_POLICY_DE.MD
' - Try/Catch konsistent im Catch-Filter-Schema
' - Variablen im Deklarationsblock, spaltenartig ausgerichtet
'
' Kontext:
' - Minimale Guard-Utilities für Argumentprüfung (fail-closed via Exceptions).
' ============================================================================

Option Strict On
Option Explicit On

Imports System

Namespace Global.Tomtastisch.FileClassifier.Infrastructure.Utils

    ''' <summary>
    '''     Utility-Funktionen für Guard-Clauses (Argumentprüfung).
    ''' </summary>
    ''' <remarks>
    '''     <para>
    '''         Zweck:
    '''         - Zentralisierte, konsistente Argumentprüfungen.
    '''         - Reduziert Boilerplate in Konstruktoren und Public APIs.
    '''     </para>
    '''     <para>
    '''         Fail-Closed:
    '''         - Bei Verstoß wird eine passende Exception ausgelöst (ArgumentNull/Argument/ArgumentOutOfRange).
    '''         - Keine stillen Korrekturen, keine Side-Effects.
    '''     </para>
    ''' </remarks>
    Friend NotInheritable Class ArgumentGuard

        ''' <summary>
        '''     Verhindert die Instanziierung; Nutzung ausschließlich über statische Members.
        ''' </summary>
        Private Sub New()
        End Sub


        ' =====================================================================
        ' Public API (Shared; Utility, stateless)
        ' =====================================================================

        ''' <summary>
        '''     Erzwingt, dass <paramref name="value"/> nicht <c>Nothing</c> ist.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Nullprüfung,
        '''         2) bei <c>Nothing</c>: <see cref="ArgumentNullException"/>.
        '''     </para>
        ''' </remarks>
        ''' <typeparam name="T">Beliebiger Referenztyp.</typeparam>
        ''' <param name="value">Zu prüfender Wert.</param>
        ''' <param name="paramName">Parametername für Exception-Metadaten.</param>
        ''' <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="value"/> <c>Nothing</c> ist.</exception>
        Public Shared Sub NotNothing(Of T) _
            (
                value As T,
                paramName As String
            )

            ' Deklarationsblock
            Dim isNull As Boolean

            If CsCoreRuntimeBridge.TryNotNull(value, paramName) Then
                Return
            End If

            isNull = (value Is Nothing)
            If isNull Then
                Throw New ArgumentNullException(paramName)
            End If

        End Sub

        ''' <summary>
        '''     Erzwingt, dass <paramref name="value"/> nicht <c>Nothing</c> ist und die erwartete Länge hat.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Nullprüfung,
        '''         2) Längenprüfung,
        '''         3) bei Abweichung: <see cref="ArgumentException"/> mit Erwartungs-/Istwert.
        '''     </para>
        ''' </remarks>
        ''' <param name="value">Array, das geprüft werden soll.</param>
        ''' <param name="expectedLength">Erwartete Länge.</param>
        ''' <param name="paramName">Parametername für Exception-Metadaten.</param>
        ''' <exception cref="ArgumentNullException">
        '''     Wird ausgelöst, wenn <paramref name="value"/> <c>Nothing</c> ist.
        ''' </exception>
        ''' <exception cref="ArgumentException">
        '''     Wird ausgelöst, wenn die Länge nicht <paramref name="expectedLength"/> entspricht.
        ''' </exception>
        Public Shared Sub RequireLength _
            (
                value As Array,
                expectedLength As Integer,
                paramName As String
            )

            ' Deklarationsblock
            Dim actualLength As Integer

            If CsCoreRuntimeBridge.TryRequireLength(value, expectedLength, paramName) Then
                Return
            End If

            If value Is Nothing Then
                Throw New ArgumentNullException(paramName)
            End If

            actualLength = value.Length
            If actualLength <> expectedLength Then
                Throw New ArgumentException(
                    $"Expected length {expectedLength}, but was {actualLength}.",
                    paramName
                )
            End If

        End Sub

        ''' <summary>
        '''     Erzwingt, dass <paramref name="value"/> als Wert in <paramref name="enumType"/> definiert ist.
        ''' </summary>
        ''' <remarks>
        '''     <para>
        '''         Ablaufstruktur:
        '''         1) Prüft <paramref name="enumType"/> auf <c>Nothing</c> und Enum-Typ,
        '''         2) prüft Definition via <c>Enum.IsDefined(enumType, value)</c>,
        '''         3) bei Verstoß: <see cref="ArgumentOutOfRangeException"/>.
        '''     </para>
        '''     <para>
        '''         Hinweis:
        '''         - Diese Guard-Funktion ist bewusst untyped, um auch Validierung bei dynamischen Enum-Zugriffen abzudecken.
        '''     </para>
        ''' </remarks>
        ''' <param name="enumType">Enum-Typ, gegen den geprüft wird.</param>
        ''' <param name="value">Zu prüfender Enum-Wert (boxed).</param>
        ''' <param name="paramName">Parametername für Exception-Metadaten.</param>
        ''' <exception cref="ArgumentNullException">
        '''     Wird ausgelöst, wenn <paramref name="enumType"/> <c>Nothing</c> ist.
        ''' </exception>
        ''' <exception cref="ArgumentException">
        '''     Wird ausgelöst, wenn <paramref name="enumType"/> kein Enum ist.
        ''' </exception>
        ''' <exception cref="ArgumentOutOfRangeException">
        '''     Wird ausgelöst, wenn <paramref name="value"/> nicht definiert ist.
        ''' </exception>
        Public Shared Sub EnumDefined _
            (
                enumType As Type,
                value As Object,
                paramName As String
            )

            ' Deklarationsblock
            Dim isEnumValueDefined As Boolean

            If CsCoreRuntimeBridge.TryRequireEnumDefined(enumType, value, paramName) Then
                Return
            End If

            If enumType Is Nothing Then
                Throw New ArgumentNullException(NameOf(enumType))
            End If

            If Not enumType.IsEnum Then
                Throw New ArgumentException("enumType muss ein Enum-Typ sein.", NameOf(enumType))
            End If

            isEnumValueDefined = [Enum].IsDefined(enumType, value)
            If Not isEnumValueDefined Then
                Throw New ArgumentOutOfRangeException(paramName)
            End If

        End Sub

    End Class

End Namespace
