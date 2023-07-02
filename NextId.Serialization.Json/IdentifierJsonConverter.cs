using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextId.Serialization.Json;

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