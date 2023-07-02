using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextId.Serialization.Json;

public class IdentifierJsonConverter<T> : JsonConverter<T>
    where T: Identifier<T>, IParsable<T>
{
    private static readonly MethodInfo _parse = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] {typeof(string), typeof(IFormatProvider)})
        ?? throw new InvalidOperationException("Failed to find parse method");

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonConverter<string> converter = (JsonConverter<string>)options.GetConverter(typeof(string));
        string? value = converter.Read(ref reader, typeof(string), options);
        
        if (value == null) return null;
        
        return (T?)_parse.Invoke(null, new object?[] {value, null});
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonConverter<string> c = (JsonConverter<string>)options.GetConverter(typeof(string));
        c.Write(writer, value.Value, options);
    }
}