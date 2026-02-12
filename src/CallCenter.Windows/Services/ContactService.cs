using System.IO;
using System.Text.Json;
using CallCenter.Windows.Models;

namespace CallCenter.Windows.Services;

/// <summary>
/// Yerel rehber servisi — JSON dosyasinda saklanir.
/// MicroSIP'teki contacts.xml'in karsiligi.
/// </summary>
public class ContactService
{
    private readonly string _filePath;
    private List<Contact> _contacts = new();

    public ContactService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CallCenter");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "contacts.json");
        Load();
    }

    public List<Contact> GetAll() => _contacts.OrderBy(c => c.Name).ToList();

    public List<Contact> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return GetAll();
        var q = query.ToLowerInvariant();
        return _contacts
            .Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || c.Number.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || (c.Company?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(c => c.Name)
            .ToList();
    }

    public List<Contact> GetFavorites() => _contacts.Where(c => c.IsFavorite).OrderBy(c => c.Name).ToList();

    public Contact? GetById(string id) => _contacts.FirstOrDefault(c => c.Id == id);

    public Contact? GetByNumber(string number) => _contacts.FirstOrDefault(c => c.Number == number);

    public void Add(Contact contact)
    {
        _contacts.Add(contact);
        Save();
    }

    public void Update(Contact contact)
    {
        var idx = _contacts.FindIndex(c => c.Id == contact.Id);
        if (idx >= 0)
        {
            _contacts[idx] = contact;
            Save();
        }
    }

    public void Delete(string id)
    {
        _contacts.RemoveAll(c => c.Id == id);
        Save();
    }

    public void ToggleFavorite(string id)
    {
        var contact = GetById(id);
        if (contact != null)
        {
            contact.IsFavorite = !contact.IsFavorite;
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _contacts = JsonSerializer.Deserialize<List<Contact>>(json) ?? new();
            }
        }
        catch
        {
            _contacts = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_contacts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Contacts] Kayit hatasi: {ex.Message}");
        }
    }
}
