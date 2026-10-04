using System.Globalization;
using Echappee.Numbers;
using Xunit;

public class BigAmountTests
{
    static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    [Theory]
    [InlineData(0, "0")]
    [InlineData(5, "5")]
    [InlineData(999, "999")]
    [InlineData(1260, "1,26K")]
    [InlineData(22500, "22,5K")]
    [InlineData(176000, "176K")]
    [InlineData(3400000, "3,4M")]
    [InlineData(2.5e9, "2,5B")]
    public void Formats_compactly(double v, string expected) =>
        Assert.Equal(expected, new BigAmount(v).ToString(Fr));

    [Fact]
    public void Adds_across_magnitudes()
    {
        var a = new BigAmount(1e6) + new BigAmount(2.5e5);
        Assert.Equal(1.25e6, a.ToDouble(), 3);
        Assert.Equal(new BigAmount(1e30), new BigAmount(1e30) + new BigAmount(1));
    }

    [Fact]
    public void Subtraction_never_goes_negative()
    {
        Assert.True((new BigAmount(10) - new BigAmount(50)).IsZero);
        Assert.Equal(40, (new BigAmount(50) - new BigAmount(10)).ToDouble(), 6);
    }

    [Fact]
    public void Multiplies_and_compares()
    {
        Assert.Equal(6e12, (new BigAmount(2e6) * new BigAmount(3e6)).ToDouble(), 0);
        Assert.True(new BigAmount(1e100) > new BigAmount(9e99));
        Assert.True(BigAmount.Zero < BigAmount.One);
    }

    [Fact]
    public void Scale_matches_pow_and_survives_huge_levels()
    {
        Assert.Equal(100 * System.Math.Pow(1.15, 10), BigAmount.Scale(100, 1.15, 10).ToDouble(), 3);
        var huge = BigAmount.Scale(100, 1.15, 5000); // dépasse double
        Assert.True(huge > new BigAmount(1e300));
    }

    [Fact]
    public void Letter_suffixes_after_Dc()
    {
        Assert.EndsWith("aa", new BigAmount(1e36).ToString(Fr));
    }

    [Fact]
    public void Save_string_roundtrips()
    {
        var a = BigAmount.FromParts(3.14159, 77);
        Assert.Equal(a, BigAmount.FromSaveString(a.ToSaveString()));
        Assert.True(BigAmount.FromSaveString("0").IsZero);
    }
}
