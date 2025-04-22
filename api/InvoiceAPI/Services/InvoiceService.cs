using System.Text.Json;
using InvoiceAPI.Models;

namespace InvoiceAPI.Services;

public class InvoiceService
{
    private readonly string _invoicesFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null // Don't use camelCase policy to match the mixed-case in the JSON
    };
    private List<Invoice> _invoices = [];

    public InvoiceService(IConfiguration configuration)
    {
        // Set the path to the invoices database
        _invoicesFilePath = configuration["DataFilePath"] ?? 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "invoices.json");
        
        // Load invoices immediately
        LoadInvoicesFromFile().GetAwaiter().GetResult();
    }

    // Helper method to read all invoices from the JSON file
    private async Task<List<Invoice>> LoadInvoicesFromFile()
    {
        if (!File.Exists(_invoicesFilePath))
        {
            var directory = Path.GetDirectoryName(_invoicesFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create an empty database if the file doesn't exist
            await SaveInvoicesToFile(new List<Invoice>());
            return new List<Invoice>();
        }

        string json = await File.ReadAllTextAsync(_invoicesFilePath);
        
        // Handle empty file case
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Invoice>();
        }
        
        var database = JsonSerializer.Deserialize<InvoiceDatabase>(json, _jsonOptions);
        _invoices = database?.Invoices ?? new List<Invoice>();
        return _invoices;
    }

    // Helper method to save all invoices to the JSON file
    private async Task SaveInvoicesToFile(List<Invoice>? invoices = null)
    {
        var database = new InvoiceDatabase { Invoices = invoices ?? _invoices };
        string json = JsonSerializer.Serialize(database, _jsonOptions);
        await File.WriteAllTextAsync(_invoicesFilePath, json);
    }

    // Get all invoices
    public List<Invoice> GetAllInvoices()
    {
        return _invoices;
    }

    // Get invoices with specific status
    public List<Invoice> GetInvoicesByStatus(string status)
    {
        return _invoices.Where(i => i.Status != null && 
            i.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // Get a specific invoice by invoice number
    public Invoice? GetInvoiceByNumber(string invoiceNumber)
    {
        return _invoices.FirstOrDefault(i => i.InvoiceNumber != null &&
            i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
    }

    // Approve an invoice
    public async Task<Invoice?> ApproveInvoiceAsync(string invoiceNumber)
    {
        var invoice = GetInvoiceByNumber(invoiceNumber);
        
        if (invoice == null)
        {
            return null;
        }

        if (invoice.Status != null && (invoice.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
            invoice.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase)))
        {
            return invoice; // Already approved or paid
        }

        // Update invoice status
        invoice.Status = "Approved";

        // Save the updated invoice back to the file
        await SaveInvoicesToFile();

        return invoice;
    }
}