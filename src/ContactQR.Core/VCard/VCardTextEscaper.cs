using System.Text;

namespace ContactQR.Core.VCard;

/// <summary>
/// Escapes text for safe placement inside a vCard 3.0 property value, per RFC 2426.
/// </summary>
/// <remarks>
/// Un-escaped commas and semicolons are the single most common vCard defect in the wild
/// (PRD FR-2.4). A company name such as <c>Acme Interiors Pvt Ltd, Mumbai</c> is, unescaped,
/// parsed as two <c>ORG</c> components and displays wrongly on the scanning phone. This type
/// exists so that every value passes through one audited implementation.
/// </remarks>
public static class VCardTextEscaper
{
    /// <summary>
    /// Escapes the characters RFC 2426 reserves within a property value: backslash, semicolon,
    /// comma and line breaks.
    /// </summary>
    /// <param name="value">The raw text to escape. May be empty.</param>
    /// <returns>The escaped text, safe to place after a property name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Backslash is handled first. Escaping it after the others would double-escape the
        // backslashes this method has just introduced.
        var escaped = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            switch (character)
            {
                case '\\':
                    escaped.Append("\\\\");
                    break;
                case ';':
                    escaped.Append("\\;");
                    break;
                case ',':
                    escaped.Append("\\,");
                    break;
                case '\r':
                    // A CRLF pair collapses to a single escaped newline rather than two.
                    escaped.Append("\\n");
                    if (index + 1 < value.Length && value[index + 1] == '\n')
                    {
                        index++;
                    }

                    break;
                case '\n':
                    escaped.Append("\\n");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }

        return escaped.ToString();
    }
}
