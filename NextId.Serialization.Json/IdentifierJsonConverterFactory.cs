using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextId.Serialization.Json;

public class IdentifierJsonConverterFactory : JsonConverterFactory
{
    private readonly SerializationProperty _serializationProperty;

    public IdentifierJsonConverterFactory(SerializationProperty serializationProperty)
    {
        _serializationProperty = serializationProperty;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true }
               && typeToConvert.BaseType.GetGenericTypeDefinition() == typeof(Identifier<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance
        (
            typeof(IdentifierJsonConverter<>).MakeGenericType(typeToConvert), _serializationProperty
        )!;
    }
}