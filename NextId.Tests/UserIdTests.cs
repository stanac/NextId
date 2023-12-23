using System.Text;

namespace NextId.Tests;

public class UserIdTests
{
    [Fact]
    public void ToString_ReturnsId()
    {
        UserId id = UserId.NewId();

        id.ToString().Count(c => c == '-').Should().Be(1);
        id.Value.Should().StartWith("user-");
    }

    [Fact]
    public void Parse_GivesExpectedValue()
    {
        int max = 1000;
        ThreadSafeRandom rand = new();

        for (int i = 0; i < max; i++)
        {
            Thread.Sleep(rand.Next(13));

            UserId id1 = UserId.NewId();
            string value = id1.ToString();

            UserId id2 = UserId.Parse(value);

            id2.Should().Be(id1);
        }
    }

    [Fact]
    public void Parse_ValidValue_ReturnsExpectedId()
    {
        string idValue = "user-4B4XH7BnCp68CCY8mzVbNT5X";

        UserId id = UserId.Parse(idValue);
        id.Value.Should().Be(idValue);
        id.NumberValue.Length.Should().BeGreaterThan(idValue.Length);
        id.ToString().Should().Be(idValue);

        var id2 = UserId.Parse(idValue);
    }

    [Fact]
    public void ParseNumberValue_GivesExpectedValue()
    {
        ThreadSafeRandom rand = new();

        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(rand.Next(13));

            UserId id1 = UserId.NewId();
            UserId id2 = UserId.Parse(id1.NumberValue);

            id2.Should().Be(id1);
        }
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
        (id1 == id2).Should().BeFalse();
    }

    [Fact]
    public void TwoIds_WithSameDate_HaveSameTimeComponent()
    {
        DateTimeOffset now = DateTimeOffset.Now;

        UserId id1 = new UserId(now);
        UserId id2 = new UserId(now);

        string time1 = id1.Value.Split('-')[1].Substring(0, 10);
        string time2 = id2.Value.Split('-')[1].Substring(0, 10);

        time2.Should().Be(time1);

        id1.TimeComponent.Should().Be(id2.TimeComponent);
    }

    [Fact]
    public void Parse_ValueTooLarge_ThrowsException()
    {
        string value = "user-" + new string('a', 50);

        Action a = () => UserId.Parse(value);
        a.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_PrefixWrong_ThrowsException()
    {
        string value = "users-4B4XH7BnCp68CCY8mzVbNT5X";

        Action a = () => UserId.Parse(value);
        a.Should().Throw<FormatException>();
    }
    
    [Fact]
    public void Parse_ChecksumWrong_ThrowsException()
    {
        string value = "user-4B4XH7BnCp68CCY8mzVbNT5Y";

        Action a = () => UserId.Parse(value);
        a.Should().Throw<FormatException>();
    }

    [Fact]
    public void IsValid_ValidValue_ReturnsTrue()
    {
        string value = "user-4B4XH7BnCp68CCY8mzVbNT5X";
        UserId.IsValid(value).Should().BeTrue();
    }

    [Fact]
    public void IsValid_NotValidValue_ReturnsFalse()
    {
        string value = "user-3B4XH7BnCp68CCY8mzVbNT5X";
        UserId.IsValid(value).Should().BeFalse();
    }
}