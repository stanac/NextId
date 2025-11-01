namespace NextId.Tests;

public class UserIdTests
{
    [Fact]
    public void ToString_ReturnsId()
    {
        const string idPrefix = "user-";
        UserId id = UserId.NewId();

        id.ToString().Count(c => c == '-').Should().Be(1);
        id.Value.Should().StartWith(idPrefix);

        id.Value.Should().HaveLength(idPrefix.Length + 27);
        id.NumberValue.Should().HaveLength(idPrefix.Length + 46);
    }

    [Fact]
    public void Value_IsValid_ReturnsTrue()
    {
        int max = 100;

        for (int i = 0; i < max; i++)
        {
            Thread.Sleep(ThreadSafeRandom.Next(13) + 1);

            UserId id = UserId.NewId();
            UserId.IsValid(id.Value).Should().BeTrue();
        }
    }

    [Fact]
    public void NumberValue_IsValid_ReturnsTrue()
    {
        int max = 100;
        
        for (int i = 0; i < max; i++)
        {
            Thread.Sleep(ThreadSafeRandom.Next(13) + 1);

            UserId id = UserId.NewId();
            UserId.IsValid(id.NumberValue).Should().BeTrue();
        }
    }

    [Fact]
    public void Parse_GivesExpectedValue()
    {
        int max = 100;
        
        for (int i = 0; i < max; i++)
        {
            Thread.Sleep(ThreadSafeRandom.Next(13) + 1);

            UserId id1 = UserId.NewId();
            string value = id1.ToString();

            UserId id2 = UserId.Parse(value);

            id2.Should().Be(id1);
        }
    }

    [Fact]
    public void Parse_ValidValue_ReturnsExpectedId()
    {
        string idValue = "user-222v7NmdXTMm2zRJdGqknKKvHYN";

        UserId id = UserId.Parse(idValue);
        id.Value.Should().Be(idValue);

        id.NumberValue.Length.Should().BeGreaterThan(idValue.Length);

        id.ToString().Should().Be(idValue);
    }

    [Fact]
    public void Parse_ValidNumberValue_ReturnsExpectedId()
    {
        string idValue = "user-5062152812410341583132584488951123813900033868";

        UserId id = UserId.Parse(idValue);
        id.NumberValue.Should().Be(idValue);

        id.NumberValue.Length.Should().BeGreaterThan(id.Value.Length);
    }

    [Fact]
    public void ParseNumberValue_GivesExpectedValue()
    {
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(ThreadSafeRandom.Next(13) + 1);

            UserId id1 = UserId.NewId();
            UserId id2 = UserId.Parse(id1.NumberValue);
            id2.NumberValue.Should().Be(id1.NumberValue);

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

        string time1 = id1.Value.Split('-')[1].Substring(0, 12);
        string time2 = id2.Value.Split('-')[1].Substring(0, 12);

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
        string value = "use-222v7HvDxSCh3aCFR6m982ZcjVT";

        Action a = () => UserId.Parse(value);
        a.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_ChecksumWrong_ThrowsException()
    {
        string value = "user-222v7HvDxSCh3aCFR6m982ZcjVE";

        Action a = () => UserId.Parse(value);
        a.Should().Throw<FormatException>();
    }

    [Fact]
    public void IsValid_ValidValue_ReturnsTrue()
    {
        string value = "user-222v7NmdXTMm2zRJdGqknKKvHYN";
        UserId.IsValid(value).Should().BeTrue();
    }
    
    [Fact]
    public void IsValid_ValidNumberValue_ReturnsTrue()
    {
        string value = "user-5062152812410341583132584488951123813900033868";
        UserId.IsValid(value).Should().BeTrue();
    }

    [Fact]
    public void IsValid_NotValidValue_ReturnsFalse()
    {
        string value = "user-222f7HvDxSCh3aCFR6m982ZcjVT";
        UserId.IsValid(value).Should().BeFalse();
    }

    [Fact]
    public void StaticFieldTest()
    {
        for (int i = 0; i < 100; i++)
        {
            UserId userId1 = UserId.NewId();
            CustomerId customerId1 = CustomerId.NewId();

            UserId.IsValid(userId1.Value).Should().BeTrue();
            UserId.IsValid(userId1.NumberValue).Should().BeTrue();
            
            UserId userId2 = UserId.Parse(userId1.Value);
            userId2.Equals(userId1).Should().BeTrue();

            UserId.IsValid(customerId1.Value).Should().BeFalse();
            UserId.IsValid(customerId1.NumberValue).Should().BeFalse();

            CustomerId customerId2 = CustomerId.Parse(customerId1.NumberValue);
            (customerId2 == customerId1).Should().BeTrue();
        }
    }

    [Fact]
    public void IdsAreUnique()
    {
        const int count = 100;
        HashSet<string> ids = new();
        HashSet<string> numberIds = new();

        for (int i = 0; i < count; i++)
        {
            ids.Add(UserId.NewId().Value);
            numberIds.Add(UserId.NewId().NumberValue);
        }

        ids.Should().HaveCount(count);
        numberIds.Should().HaveCount(count);
    }

    [Fact]
    public async Task ThreadSafetyCheckTest()
    {
        HashSet<string> values = new();

        const int count = 1000;

    }
}