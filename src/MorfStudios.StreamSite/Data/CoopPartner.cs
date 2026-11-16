using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorfStudios.StreamSite.Data;

/// <summary>One entry in <c>data/coop-partners.json</c>: someone I play co-op with.</summary>
public sealed record CoopPartner
{
    /// <summary>As the site shows it. <c>coop</c> in the other files uses it, compared with <see cref="Names.Key"/>.</summary>
    public required string Name { get; init; }

    /// <summary>Their Twitch name, like Nyakuzaa, when they have a channel. The site links to it.</summary>
    public string? Twitch { get; init; }

    /// <summary>Not in the file, so a "twitchUrl" there stops the build.</summary>
    [JsonIgnore]
    public string? TwitchUrl => Twitch is null ? null : $"https://www.twitch.tv/{Uri.EscapeDataString(Twitch)}";
}

/// <summary>Reads <c>"coop": "Nyakuzaa"</c> as well as <c>"coop": ["franticalle", "Nyakuzaa"]</c>.</summary>
public sealed class OneOrManyConverter : JsonConverter<IReadOnlyList<string>>
{
    public override IReadOnlyList<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? [reader.GetString()!]
            : JsonSerializer.Deserialize<List<string>>(ref reader, options)!;

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
