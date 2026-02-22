using System;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Deterministische Richtlinienhelfer für Hash-Evidenzbezeichner, Hinweise und sichere Schlüsselauflösung.
/// </summary>
public static class EvidencePolicyUtility
{
    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Normalisiert einen Evidenzbezeichner auf einen deterministischen Ersatzwert, wenn er leer ist.
    /// </summary>
    public static string NormalizeLabel(string? label, string? defaultLabel)
    {
        var normalized = (label ?? string.Empty).Trim();
        if (normalized.Length > 0)
        {
            return normalized;
        }

        var fallback = (defaultLabel ?? string.Empty).Trim();
        return fallback.Length > 0 ? fallback : "payload.bin";
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Hängt eine optionale Notiz deterministisch an bestehende Notizen an.
    /// </summary>
    public static string AppendNoteIfAny(string? baseNotes, string? toAppend)
    {
        var left = (baseNotes ?? string.Empty).Trim();
        var right = (toAppend ?? string.Empty).Trim();

        if (right.Length == 0)
        {
            return left;
        }

        if (left.Length == 0)
        {
            return right;
        }

        return left + " " + right;
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Löst einen HMAC-Schlüssel aus einer Umgebungsvariable auf und liefert<br/>
    /// [istAufgelöst(bool), schlüssel(byte[]), hinweis(string)] für die Laufzeit-Brücke.
    /// </summary>
    public static object[] ResolveHmacKeyFromEnvironment(string? environmentVariableName)
    {
        var variableName = (environmentVariableName ?? string.Empty).Trim();
        if (variableName.Length == 0)
        {
            return
            [
                false,
                Array.Empty<byte>(),
                "Secure hashing requested but env var name is empty; HMAC digests omitted."
            ];
        }

        var b64 = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(b64))
        {
            return
            [
                false,
                Array.Empty<byte>(),
                $"Secure hashing requested but env var '{variableName}' is missing; HMAC digests omitted."
            ];
        }

        try
        {
            var key = Convert.FromBase64String(b64.Trim());
            if (key.Length == 0)
            {
                return
                [
                    false,
                    Array.Empty<byte>(),
                    $"Secure hashing requested but env var '{variableName}' is empty; HMAC digests omitted."
                ];
            }

            return [true, key, string.Empty];
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return
            [
                false,
                Array.Empty<byte>(),
                $"Secure hashing requested but env var '{variableName}' is invalid Base64; HMAC digests omitted."
            ];
        }
    }
}
