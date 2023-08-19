using NextId.Tests;

namespace NextId.Serialization.Json.Tests;

public class User
{
    public required UserId Id { get; set; }
    public required string Name { get; set; }

    public static User NewRandomUser()
    {
        return new User
        {
            Id = UserId.NewId(),
            Name = Guid.NewGuid().ToString()
        };
    }
}