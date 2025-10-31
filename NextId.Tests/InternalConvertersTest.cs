namespace NextId.Tests;

public class InternalConvertersTest
{
    [Fact]
    public void ConvertToNumber_ConvertBack_GivesSameValue()
    {
        for (int i = 0; i < 100; i++)
        {
            byte[] data = Guid.NewGuid().ToByteArray();
            ulong value = BitConverter.ToUInt64(data, 0);

            string encoded = InternalConverters.EncodeToString(value);
            ulong decoded = InternalConverters.Decode(encoded);
            
            decoded.Should().Be(value);

            ulong data2 = (ulong)ThreadSafeRandom.NextInt64();

            encoded = InternalConverters.EncodeToString(data2);
            decoded = InternalConverters.Decode(encoded);

            decoded.Should().Be(data2);
        }
    }

    [Fact]
    public void EncodedDataAreSortable()
    {
        for (int i = 0; i < 100; i++)
        {
            ulong data1 = (ulong)ThreadSafeRandom.NextInt64() / 2;
            ulong data2 = (ulong)(data1 * 1.171M);

            data2.Should().BeGreaterThan(data1);

            string value1 = InternalConverters.EncodeToString(data1);
            string value2 = InternalConverters.EncodeToString(data2);
            value1.Length.Should().Be(value2.Length);

            string[] array = [value1, value2];

            array.OrderBy(x => x).Should().BeEquivalentTo(array);
        }
    }

    [Fact]
    public void EncodeToNumber_Decode_GivesSameValue()
    {
        for (int i = 0; i < 100; i++)
        {
            byte[] data = Guid.NewGuid().ToByteArray();
            ulong value = BitConverter.ToUInt64(data, 0);

            string encoded = InternalConverters.EncodeToNumberString(value);
            ulong decoded = InternalConverters.Decode(encoded);

            decoded.Should().Be(value);

            ulong data2 = (ulong)ThreadSafeRandom.NextInt64();

            encoded = InternalConverters.EncodeToNumberString(data2);
            decoded = InternalConverters.Decode(encoded);

            decoded.Should().Be(data2);
        }
    }

    [Fact]
    public void EncodeChecksum_Gives3Digits()
    {
        int i = 0;
        while (i < InternalConverters.Max3Digits)
        {
            string checksum = InternalConverters.EncodeChecksum(i);
            checksum.Length.Should().Be(3);

            i += ThreadSafeRandom.Next(127) + 1;
        }
    }
}