namespace NextId.Tests;

public class UserIdTests
{
    [Fact]
    public void ToString_GivesId()
    {
        UserId id = new UserId();

        id.ToString().Count(c => c == '-').Should().Be(4);
    }

    [Fact]
    public void Parse_ValidValue_GivesExpectedId()
    {
        string idValue = "user-37zRpmxZ-0-CDmVuNnk3yBd63VzJvFKmJ-YYs";

        UserId id = UserId.Parse(idValue);
        id.Value.Should().Be(idValue);
        id.ToString().Should().Be(idValue);
    }

    [Fact]
    public void TwoSameValues_AreEqual_ReturnsTrue()
    {
        UserId id1 = UserId.NewId();
        UserId id2 = UserId.Parse(id1.Value);

        id1.Equals(id2).Should().BeTrue();
        Equals(id1, id2).Should().BeTrue();

        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void TwoDifferentValues_AreEqual_ReturnsFalse()
    {
        UserId id1 = UserId.NewId();
        UserId id2 = UserId.NewId();

        id1.Equals(id2).Should().BeFalse();
        Equals(id1, id2).Should().BeFalse();

        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void Parse_ValueTooLarge_ThrowsException()
    {
        string value = new('a', 300);

        Action a = () => UserId.Parse(value);
        a.Should().Throw<ArgumentException>();
    }
}