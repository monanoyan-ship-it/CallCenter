using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CallCenter.Tests.Factories;

public class SlnClientFactoryTests : IDisposable
{
    private readonly AppDbContext _db;

    public SlnClientFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData("Test Musteri", "123", "test@example.com", null, "Gecerli bir telefon girin")]
    [InlineData("Test Musteri", "+905551234567", "hatali-email", null, "Gecerli bir e-posta girin")]
    [InlineData("Test Musteri", "+905551234567", "test@example.com", 101, "Beyaz orani 0 ile 100 arasinda olmali")]
    public async Task CreateClientAsync_RejectsInvalidContactAndHealthFields(
        string fullName,
        string phone,
        string email,
        int? whiteRatioPercent,
        string expectedError)
    {
        var factory = CreateFactory();
        var dto = new SlnClientCreateDto
        {
            FullName = fullName,
            Phone = phone,
            Email = email,
            WhiteRatioPercent = whiteRatioPercent
        };

        var act = () => factory.CreateClientAsync(dto, customerId: 1);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(expectedError);
        (await _db.SlnClients.CountAsync()).Should().Be(0);
    }

    private SlnClientFactory CreateFactory()
        => new(
            new SlnClientEntityService(_db),
            new SlnFormulaEntityService(_db),
            new SlnTreatmentRecordEntityService(_db),
            new SlnClientPhotoEntityService(_db),
            new SlnAppointmentEntityService(_db),
            new SlnInvoiceEntityService(_db),
            new SlnServiceSessionPlanEntityService(_db),
            Substitute.For<ISlnServiceSessionFactory>(),
            new UnitOfWork(_db),
            NullLogger<SlnClientFactory>.Instance);
}
