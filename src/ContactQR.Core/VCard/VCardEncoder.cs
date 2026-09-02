using System.Text;
using ContactQR.Core.Contacts;

namespace ContactQR.Core.VCard;

/// <summary>
/// Encodes a <see cref="ClientRecord"/> as a vCard 3.0 payload (RFC 2426) for QR encoding.
/// </summary>
/// <remarks>
/// <para>
/// This is the highest-correctness-risk component in the product and the part every free
/// online generator gets wrong. It is deliberately pure — a record in, a string out, with no
/// I/O, no shared state and no network — so that it is exhaustively unit-testable
/// (PRD FR-2.10).
/// </para>
/// <para>
/// Deliberate deviation from RFC 2426: long lines are <b>not</b> folded at 75 octets
/// (PRD FR-2.6). Folding adds bytes to an already-constrained payload and a meaningful
/// minority of mobile parsers mishandle folded lines. This must be confirmed across the
/// device matrix rather than assumed — see PRD open question Q9.
/// </para>
/// </remarks>
public static class VCardEncoder
{
    /// <summary>
    /// vCard 3.0 requires CRLF line terminators. Some parsers tolerate a bare LF and iOS
    /// behaviour is inconsistent, which produces intermittent device-specific failures that
    /// are very hard to diagnose after cards are printed (PRD FR-2.3).
    /// </summary>
    private const string LineBreak = "\r\n";

    /// <summary>
    /// Encodes a client as a vCard 3.0 payload.
    /// </summary>
    /// <param name="client">The client to encode.</param>
    /// <returns>A complete vCard 3.0 document with CRLF terminators.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The client has no given name or no primary phone. These are the only two fields that
    /// block generation (PRD FR-1.2).
    /// </exception>
    public static string Encode(ClientRecord client)
    {
        ArgumentNullException.ThrowIfNull(client);
        GuardRequiredFields(client);

        // Property order is fixed so that identical input always produces a byte-identical
        // QR, which is what makes a reprint reproducible and a diff meaningful (PRD FR-2.8).
        var vcard = new StringBuilder();

        AppendLine(vcard, "BEGIN:VCARD");
        AppendLine(vcard, "VERSION:3.0");
        AppendStructuredName(vcard, client);
        AppendProperty(vcard, "FN", client.FullName);
        AppendProperty(vcard, "ORG", client.Company);
        AppendProperty(vcard, "TITLE", client.JobTitle);
        AppendContactPoints(vcard, client, ContactPointKind.Phone);
        AppendContactPoints(vcard, client, ContactPointKind.Email);
        AppendContactPoints(vcard, client, ContactPointKind.Url);
        AppendAddress(vcard, client.Address);
        AppendProperty(vcard, "NOTE", client.Note);
        AppendLine(vcard, "END:VCARD");

        return vcard.ToString();
    }

    /// <summary>
    /// Measures the encoded size of a client in UTF-8 bytes — the unit that actually consumes
    /// QR capacity.
    /// </summary>
    /// <param name="client">The client to measure.</param>
    /// <returns>The payload size in bytes.</returns>
    /// <remarks>
    /// Callers must never count characters. A Devanagari or CJK name costs roughly three bytes
    /// per character and will otherwise push a code over budget invisibly (PRD FR-2.5, EC-2).
    /// </remarks>
    public static int MeasureBytes(ClientRecord client) =>
        Encoding.UTF8.GetByteCount(Encode(client));

    private static void GuardRequiredFields(ClientRecord client)
    {
        if (string.IsNullOrWhiteSpace(client.GivenName))
        {
            throw new ArgumentException(
                "A given name is required to encode a vCard. It is one of only two blocking fields.",
                nameof(client));
        }

        if (client.PrimaryPhone is null)
        {
            throw new ArgumentException(
                "A primary phone number is required to encode a vCard. It is one of only two blocking fields.",
                nameof(client));
        }
    }

    /// <summary>
    /// Emits the structured <c>N</c> property. Both <c>N</c> and <c>FN</c> are mandatory in
    /// vCard 3.0 (PRD FR-2.2).
    /// </summary>
    private static void AppendStructuredName(StringBuilder vcard, ClientRecord client)
    {
        var family = VCardTextEscaper.Escape(client.FamilyName?.Trim() ?? string.Empty);
        var given = VCardTextEscaper.Escape(client.GivenName.Trim());

        // Family;Given;Additional;Prefix;Suffix — all five positions always present.
        AppendLine(vcard, $"N:{family};{given};;;");
    }

    private static void AppendContactPoints(StringBuilder vcard, ClientRecord client, ContactPointKind kind)
    {
        var points = client.ContactPoints
            .Where(point => point.Kind == kind)
            .Where(point => !string.IsNullOrWhiteSpace(point.ValueToEncode))
            .OrderByDescending(point => point.IsPrimary)
            .ThenBy(point => point.SortOrder);

        foreach (var point in points)
        {
            AppendLine(vcard, $"{PropertyNameFor(kind)}{TypeParameterFor(point)}:{VCardTextEscaper.Escape(point.ValueToEncode.Trim())}");
        }
    }

    private static string PropertyNameFor(ContactPointKind kind) => kind switch
    {
        ContactPointKind.Phone => "TEL",
        ContactPointKind.Email => "EMAIL",
        ContactPointKind.Url => "URL",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported contact point kind."),
    };

    /// <summary>
    /// Builds the vCard <c>TYPE</c> parameter. The comma inside a parameter such as
    /// <c>TYPE=WORK,VOICE</c> is a structural delimiter and is deliberately not escaped —
    /// only property <em>values</em> are escaped.
    /// </summary>
    private static string TypeParameterFor(ContactPoint point) => point switch
    {
        { Kind: ContactPointKind.Url } => string.Empty,
        { Kind: ContactPointKind.Phone, Subtype: ContactPointSubtype.Mobile } => ";TYPE=CELL",
        { Kind: ContactPointKind.Phone, Subtype: ContactPointSubtype.Work } => ";TYPE=WORK,VOICE",
        { Kind: ContactPointKind.Phone, Subtype: ContactPointSubtype.Fax } => ";TYPE=WORK,FAX",
        { Kind: ContactPointKind.Phone } => ";TYPE=HOME",
        { Kind: ContactPointKind.Email, Subtype: ContactPointSubtype.Home } => ";TYPE=INTERNET,HOME",
        { Kind: ContactPointKind.Email } => ";TYPE=INTERNET,WORK",
        _ => string.Empty,
    };

    private static void AppendAddress(StringBuilder vcard, PostalAddress? address)
    {
        if (address is null || address.IsEmpty)
        {
            return;
        }

        // PO box; extended; street; locality; region; postcode; country — all seven positions
        // always present, since dropping an empty leading component shifts every later value
        // into the wrong field on the scanning phone (PRD FR-2.7).
        var components = string.Join(
            ';',
            string.Empty,
            string.Empty,
            EscapeOrEmpty(address.Street),
            EscapeOrEmpty(address.City),
            EscapeOrEmpty(address.State),
            EscapeOrEmpty(address.PostalCode),
            EscapeOrEmpty(address.Country));

        AppendLine(vcard, $"ADR;TYPE=WORK:{components}");
    }

    /// <summary>Emits a simple property, omitting it entirely when the value is empty.</summary>
    private static void AppendProperty(StringBuilder vcard, string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        AppendLine(vcard, $"{propertyName}:{VCardTextEscaper.Escape(value.Trim())}");
    }

    private static string EscapeOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : VCardTextEscaper.Escape(value.Trim());

    private static void AppendLine(StringBuilder vcard, string line) =>
        vcard.Append(line).Append(LineBreak);
}
