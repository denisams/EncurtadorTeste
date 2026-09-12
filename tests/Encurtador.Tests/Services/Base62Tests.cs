using Encurtador.Domain.Common;

namespace Encurtador.Tests.Services;

public class Base62Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(61)]
    [InlineData(62)]
    [InlineData(123456789)]
    [InlineData(long.MaxValue)]
    public void EncodeThenDecode_RoundTrips(long value)
    {
        var code = Base62.Encode(value);
        var decoded = Base62.Decode(code);

        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Encode_ProducesShorterCodesForLargerNumbers_ThanDecimal()
    {
        var code = Base62.Encode(56800235584); // 62^6

        Assert.True(code.Length <= 7);
    }
}
