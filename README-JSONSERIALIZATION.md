This library adds support for serialization of [NextId identifiers](https://github.com/stanac/nextid)
using `System.Text.Json` serializer.

Add package reference
```
dotnet add package NextId.Serialization.Json
```

Use it as in test:

```csharp
public class SerializationTests
{
    private readonly JsonSerializerOptions _options;

    public SerializationTests()
    {
        _options = new JsonSerializerOptions();
        _options.AddIdentifierConverters(serializeIdsAsNumberValues: false);
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
```