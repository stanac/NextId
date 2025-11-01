using System.Text.Json;

namespace NextId.Serialization.Json.Tests;

public class SerializationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SerializeDeserialize_GivesEquivalentObject(bool serializeIdsAsNumberValues)
    {
        JsonSerializerOptions options = new();
        options.AddIdentifierConverters(serializeIdsAsNumberValues);

        User user1 = User.NewRandomUser();
        string json = JsonSerializer.Serialize(user1, options);
        User user2 = JsonSerializer.Deserialize<User>(json, options)!;

        user1.Id.Should().Be(user2.Id);
        user2.Should().BeEquivalentTo(user1);
    }
}