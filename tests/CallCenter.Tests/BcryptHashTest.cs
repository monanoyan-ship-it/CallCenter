namespace CallCenter.Tests;

public class BcryptHashTest
{
    [Fact]
    public void VerifySeedHash()
    {
        var hash = "$2a$11$4NK5QRHYyKGuXY/Wr41bGOgqCOD1PDK.c1473NdyCowy2.HJswS72";
        var result = BCrypt.Net.BCrypt.Verify("1123Azs+-", hash);
        Assert.True(result, $"Seed sifre hash ile eslesmiyor! Yeni hash: {BCrypt.Net.BCrypt.HashPassword("1123Azs+-")}");
    }
}
