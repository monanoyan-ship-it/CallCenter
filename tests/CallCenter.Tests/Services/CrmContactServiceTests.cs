using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Services;

public class CrmContactServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CrmContactService _sut;

    public CrmContactServiceTests()
    {
        _db = TestDbContextFactory.CreateWithSeedData();
        _sut = new CrmContactService(_db, NullLogger<CrmContactService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    // CreateContactAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task CreateContact_ValidRequest_CreatesAndReturns()
    {
        var req = new CreateContactRequest
        {
            FullName = "Ahmet Yilmaz",
            PhoneNumber = "+905551234567",
            Email = "ahmet@test.com",
            Company = "Test A.S."
        };

        var result = await _sut.CreateContactAsync(req, userId: 101, customerId: 200);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Ahmet Yilmaz");
        result.PhoneNumber.Should().Be("+905551234567");
        result.Email.Should().Be("ahmet@test.com");
        result.Company.Should().Be("Test A.S.");
        result.SourceId.Should().Be(CrmContactSources.Ids.Manual);
    }

    // ═══════════════════════════════════════
    // GetContactsAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetContacts_ReturnsOwnedAndSharedContacts()
    {
        // Arrange
        _db.CrmContacts.AddRange(
            new CrmContact { FullName = "Kisi 1", PhoneNumber = "1111", OwnerUserId = 101, CustomerId = 200 },
            new CrmContact { FullName = "Kisi 2", PhoneNumber = "2222", OwnerUserId = null, CustomerId = 200 }, // paylasilmis
            new CrmContact { FullName = "Kisi 3", PhoneNumber = "3333", OwnerUserId = 102, CustomerId = 200 }  // baskasinin
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetContactsAsync(userId: 101, customerId: 200, search: null);

        // userId=101 -> OwnerUserId=101 veya OwnerUserId=null olan kayitlari gorebilir
        result.Should().HaveCount(2);
        result.Select(c => c.FullName).Should().Contain("Kisi 1");
        result.Select(c => c.FullName).Should().Contain("Kisi 2");
    }

    [Fact]
    public async Task GetContacts_WithSearch_FiltersCorrectly()
    {
        _db.CrmContacts.AddRange(
            new CrmContact { FullName = "Ali Veli", PhoneNumber = "1111", OwnerUserId = 101 },
            new CrmContact { FullName = "Mehmet Can", PhoneNumber = "2222", OwnerUserId = 101 },
            new CrmContact { FullName = "Ayse Demir", PhoneNumber = "3333", OwnerUserId = 101, Company = "Ali Ticaret" }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetContactsAsync(userId: 101, customerId: null, search: "ali");

        // "Ali Veli" (isim) + "Ayse Demir" (sirket=Ali Ticaret)
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetContacts_Pagination_Works()
    {
        for (int i = 1; i <= 10; i++)
        {
            _db.CrmContacts.Add(new CrmContact
            {
                FullName = $"Kisi {i:D2}",
                PhoneNumber = $"555{i:D4}",
                OwnerUserId = 101
            });
        }
        await _db.SaveChangesAsync();

        var page1 = await _sut.GetContactsAsync(101, null, null, page: 1, pageSize: 3);
        var page2 = await _sut.GetContactsAsync(101, null, null, page: 2, pageSize: 3);

        page1.Should().HaveCount(3);
        page2.Should().HaveCount(3);
    }

    // ═══════════════════════════════════════
    // GetContactAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetContact_OwnContact_ReturnsContact()
    {
        var contact = new CrmContact { FullName = "Test", PhoneNumber = "1234", OwnerUserId = 101 };
        _db.CrmContacts.Add(contact);
        await _db.SaveChangesAsync();

        var result = await _sut.GetContactAsync(contact.Id, userId: 101);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Test");
    }

    [Fact]
    public async Task GetContact_OtherUsersContact_ReturnsNull()
    {
        var contact = new CrmContact { FullName = "Gizli", PhoneNumber = "9999", OwnerUserId = 102 };
        _db.CrmContacts.Add(contact);
        await _db.SaveChangesAsync();

        var result = await _sut.GetContactAsync(contact.Id, userId: 101);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContact_SharedContact_ReturnsContact()
    {
        var contact = new CrmContact { FullName = "Ortak", PhoneNumber = "5555", OwnerUserId = null };
        _db.CrmContacts.Add(contact);
        await _db.SaveChangesAsync();

        var result = await _sut.GetContactAsync(contact.Id, userId: 101);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Ortak");
    }

    // ═══════════════════════════════════════
    // ImportFromCsvAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task ImportCsv_ValidData_ImportsCorrectly()
    {
        // "FullName,PhoneNumber,Email" CSV
        var csvContent = "Ad Soyad,Telefon,Email\nAli Veli,+905551111111,ali@test.com\nMehmet Can,+905552222222,mehmet@test.com";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var req = new CsvImportRequest
        {
            CsvContentBase64 = base64,
            HasHeader = true,
            ColumnMapping = new Dictionary<int, string>
            {
                { 0, "FullName" },
                { 1, "PhoneNumber" },
                { 2, "Email" }
            }
        };

        var result = await _sut.ImportFromCsvAsync(req, userId: 101, customerId: 200);

        result.TotalRows.Should().Be(2);
        result.ImportedCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        var contacts = _db.CrmContacts.Where(c => c.OwnerUserId == 101).ToList();
        contacts.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportCsv_EmptyContent_ReturnsZero()
    {
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Header\n"));

        var req = new CsvImportRequest
        {
            CsvContentBase64 = base64,
            HasHeader = true,
            ColumnMapping = new Dictionary<int, string> { { 0, "FullName" } }
        };

        var result = await _sut.ImportFromCsvAsync(req, userId: 101, customerId: null);

        result.TotalRows.Should().Be(0);
        result.ImportedCount.Should().Be(0);
    }
}
