using NextId.Serialization.Json;

// ReSharper disable once CheckNamespace
namespace System.Text.Json;

public static class Extensions
{
    public static JsonSerializerOptions AddIdentifierConverters(this JsonSerializerOptions options)
    {
        options.Converters.Add(new IdentifierJsonConverterFactory());

        return options;
    }
}