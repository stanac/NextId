using NextId.Serialization.Json;

// ReSharper disable once CheckNamespace
namespace System.Text.Json;

public static class Extensions
{
    public static JsonSerializerOptions AddIdentifierConverters(this JsonSerializerOptions options, bool serializeIdsAsNumberValues)
    {
        options.Converters.Add(new IdentifierJsonConverterFactory(serializeIdsAsNumberValues));

        return options;
    }
}