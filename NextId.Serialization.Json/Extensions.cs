using NextId.Serialization.Json;

// ReSharper disable once CheckNamespace
namespace System.Text.Json;

public static class Extensions
{
    [Obsolete("Use override with SerializationProperty.")]
    public static JsonSerializerOptions AddIdentifierConverters(this JsonSerializerOptions options, bool serializeIdsAsNumberValues)
        => options.AddIdentifierConverters(serializeIdsAsNumberValues
            ? SerializationProperty.NumberValue
            : SerializationProperty.Value);

    public static JsonSerializerOptions AddIdentifierConverters(this JsonSerializerOptions options, SerializationProperty serializationProperty)
    {
        options.Converters.Add(new IdentifierJsonConverterFactory(serializationProperty));
        return options;
    }
}