using CallCenter.Shared.Helpers;

namespace CallCenter.Tests.Helpers;

public class PhoneHelperTests
{
    [Theory]
    [InlineData("0506 071 67 28", "+905060716728")]
    [InlineData("5060716728", "+905060716728")]
    [InlineData("905060716728", "+905060716728")]
    [InlineData("+90 (506) 071 67 28", "+905060716728")]
    [InlineData("00905060716728", "+905060716728")]
    public void Normalize_TurkishPhoneFormats_ReturnsCanonicalE164(string input, string expected)
    {
        PhoneHelper.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void GetLookupVariants_IncludesLegacyAndCanonicalTurkishFormats()
    {
        var variants = PhoneHelper.GetLookupVariants("+905060716728");

        variants.Should().Contain(new[]
        {
            "+905060716728",
            "905060716728",
            "5060716728",
            "05060716728"
        });
    }
}
