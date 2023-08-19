using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextId.Serialization.Json.Tests;

public class SerializationTests
{
    private readonly JsonSerializerOptions _options;

    public SerializationTests()
    {
        _options = new JsonSerializerOptions();
        _options.AddIdentifierConverters();
    }

    [Fact]
    public void Test1()
    {
        User user1 = User.NewRandomUser();
        string json = JsonSerializer.Serialize(user1, _options);
        User user2 = JsonSerializer.Deserialize<User>(json, _options)!;

        user1.Id.Should().Be(user2.Id);
    }
}