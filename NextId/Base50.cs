using System.Text;

namespace NextId;

internal static class Base50
{
    public const string Charset = "23456789BCDFGHJKLMNPQRSTVWXYZabcdfghjkmnpqrstvwxyz";

    public static string ToString(long value, int pad = 0)
    {
        string result = "";
        int targetBase = Charset.Length;

        do
        {
            result = Charset[(int)(value % targetBase)] + result;
            value /= targetBase;
        } while (value > 0);

        if (pad > result.Length)
        {
            result = result.PadLeft(pad, Charset[0]);
        }

        return result;
    }

    public static long ToLong(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        value = new string(value.Reverse().ToArray());

        long result = 0;
        double baseValue = Charset.Length;

        for (long index = 0; index < value.Length; index++)
        {
            long charValue = Charset.IndexOf(value[(int)index]);
            result += charValue * (long)Math.Pow(baseValue, index);
        }

        return result;
    }

    public static string GetNumberValue(string value)
    {
        char[] chars = new char[value.Length * 2];

        for (int i = 0; i < value.Length; i++)
        {
            int index = Charset.IndexOf(value[i]);
            string indexStr = index.ToString("00");
            chars[i * 2] = indexStr[0];
            chars[i * 2 + 1] = indexStr[1];
        }

        return new(chars);
    }

    public static string GetStringValue(string numberValue)
    {
        char[] chars = new char[numberValue.Length / 2];

        for (int i = 0; i < numberValue.Length / 2; i++)
        {
            int firstIndex = i * 2;
            char[] indexChars = { numberValue[firstIndex], numberValue[firstIndex + 1] };
            int index = int.Parse(new string(indexChars));
            chars[i] = Charset[index];
        }

        return new string(chars);
    }
}