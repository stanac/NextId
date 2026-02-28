using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextId.Serialization.Json;

public class IdentifierJsonConverter<T> : JsonConverter<T>
    where T: Identifier<T>, IParsable<T>
{
    private readonly SerializationProperty _propertyToSerialize;
    
    public IdentifierJsonConverter()
        : this(SerializationProperty.Value)
    {
    }

    [Obsolete("Use any other constructor")]
    public IdentifierJsonConverter(bool serializeIdsAsNumberValues)
        : this(serializeIdsAsNumberValues ? SerializationProperty.NumberValue : SerializationProperty.Value)
    {
    }

    public IdentifierJsonConverter(SerializationProperty propertyToSerialize)
    {
        _propertyToSerialize = propertyToSerialize;
    }

    private static readonly MethodInfo Parse = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] {typeof(string), typeof(IFormatProvider)})
        ?? throw new InvalidOperationException("Failed to find parse method");

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonConverter<string> converter = (JsonConverter<string>)options.GetConverter(typeof(string));
        string? value = converter.Read(ref reader, typeof(string), options);
        
        if (value == null) return null;
        
        return (T?)Parse.Invoke(null, [value, null]);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonConverter<string> c = (JsonConverter<string>)options.GetConverter(typeof(string));
        
        string idValue = _propertyToSerialize switch
        {
            SerializationProperty.Value => value.Value,
            SerializationProperty.NumberValue => value.NumberValue,
            SerializationProperty.ValueNoPrefix => value.ValueNoPrefix,
            SerializationProperty.NumberValueNoPrefix => value.NumberValueNoPrefix,
            _ => throw new ArgumentOutOfRangeException()
        };

        c.Write(writer, idValue, options);
    }
}