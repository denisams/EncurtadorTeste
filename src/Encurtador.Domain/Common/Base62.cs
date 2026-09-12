using System.Text;

namespace Encurtador.Domain.Common;

/// <summary>
/// Encodes monotonically increasing integers into short, URL-safe Base62 codes.
/// Used to turn a Redis-generated sequence number into a compact short code
/// without needing a lookup at generation time.
/// </summary>
public static class Base62
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Encode(long value)
    {
        if (value == 0)
        {
            return Alphabet[0].ToString();
        }

        var sb = new StringBuilder();
        var remaining = value;

        while (remaining > 0)
        {
            var digit = (int)(remaining % Alphabet.Length);
            sb.Insert(0, Alphabet[digit]);
            remaining /= Alphabet.Length;
        }

        return sb.ToString();
    }

    public static long Decode(string code)
    {
        long value = 0;

        foreach (var c in code)
        {
            var digit = Alphabet.IndexOf(c);
            if (digit < 0)
            {
                throw new FormatException($"Invalid Base62 character '{c}' in code '{code}'.");
            }

            value = (value * Alphabet.Length) + digit;
        }

        return value;
    }
}
