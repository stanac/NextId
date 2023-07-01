namespace NextId.Tests;

public class UserIdTests
{
    [Fact]
    public void ToString_GivesId()
    {
        UserId id = new UserId();

        id.ToString().Count(c => c == '-').Should().Be(4);
    }
}