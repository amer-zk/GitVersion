namespace GitVersion.Attributes;

/// <summary>
/// <para>The <c>format</c> keyword allows for basic semantic identification of certain kinds of string values that are commonly used. For example, because JSON doesn't have a "DateTime" type, dates need to be encoded as strings. <c>format</c> allows the schema author to indicate that the string value should be interpreted as a date. By default, <c>format</c> is just an annotation and does not effect validation.</para>
/// <para>Optionally, validator implementations can [...] enable <c>format</c> to function as an assertion rather than just an annotation.</para>
/// <see href="https://json-schema.org/understanding-json-schema/reference/string#format"/>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyFormatAttribute : JsonAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonPropertyFormatAttribute"/> with the specified format for string-encoded property values. JSON validators may use this annotation to constrain properties to certain pre-define, string-encoded types.
    /// </summary>
    /// <param name="format">The string format.</param>
    public JsonPropertyFormatAttribute(Format format) => Format = format;

    /// <summary>
    /// The format of the string.
    /// </summary>
    public Format Format { get; }
}

/// <summary>
/// Enum bindings for <c>Json.Schema.Formats</c> of JsonSchema.Net 5.2.7
/// </summary>
public enum Format
{
    Date,
    DateTime,
    Duration,
    Email,
    Hostname,
    IdnEmail,
    IdnHostname,
    Ipv4,
    Ipv6,
    Iri,
    IriReference,
    JsonPointer,
    Regex,
    RelativeJsonPointer,
    Time,
    Uri,
    UriReference,
    UriTemplate,
    Uuid
}
