using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextId.Serialization.Json;

public class IdentifierJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsClass
               && !typeToConvert.IsAbstract
               && typeToConvert.BaseType != null
               && typeToConvert.BaseType.IsGenericType
               && typeToConvert.BaseType.GetGenericTypeDefinition() == typeof(Identifier<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(IdentifierJsonConverter<>).MakeGenericType(typeToConvert))!;
    }
}

public class IdentifierJsonConverter<T> : JsonConverter<T>
    where T: Identifier<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}