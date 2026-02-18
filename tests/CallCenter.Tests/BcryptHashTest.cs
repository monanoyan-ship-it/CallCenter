namespace CallCenter.Tests;

public class BcryptHashTest
{
    [Fact]
    public void VerifySeedHash()
    {
        var hash = "$2a$11$4NK5QRHYyKGuXY/Wr41bGOgqCOD1PDK.c1473NdyCowy2.HJswS72";
        var result = BCrypt.Net.BCrypt.Verify("admin123", hash);
        Assert.True(result, $"admin123 hash ile eslesmiyor! Yeni hash: {BCrypt.Net.BCrypt.HashPassword("admin123")}");
    }
}
