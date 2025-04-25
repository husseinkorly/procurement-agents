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

    // Generate a truly random invoice number
    private string GenerateRandomInvoiceNumber()
    {
        // Use a combination of date, random number and guid for uniqueness
        var random = new Random();
        var randomPart = random.Next(1000, 9999).ToString();
        var uniqueId = Guid.NewGuid().ToString("N")[..8].ToUpper(); // Get first 8 chars and make uppercase
        return $"INV-{DateTime.Now:yyyyMMdd}-{randomPart}-{uniqueId}";
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

    // Create a new invoice
    public async Task<Invoice?> CreateInvoiceAsync(Invoice invoice)
    {
        try
        {
            // Check if an invoice with the same number already exists
            if (!string.IsNullOrEmpty(invoice.InvoiceNumber) &&
                _invoices.Any(i => i.InvoiceNumber == invoice.InvoiceNumber))
            {
                // Instead of returning null, throw a specific exception
                throw new InvalidOperationException($"Invoice with number {invoice.InvoiceNumber} already exists");
            }

            // Generate a unique invoice number if one wasn't provided
            if (string.IsNullOrEmpty(invoice.InvoiceNumber))
            {
                invoice.InvoiceNumber = GenerateRandomInvoiceNumber();
            }

            // Set default values if not provided
            if (string.IsNullOrEmpty(invoice.Status))
            {
                invoice.Status = "Pending Approval";
            }

            if (string.IsNullOrEmpty(invoice.InvoiceDate))
            {
                invoice.InvoiceDate = DateTime.Now.ToString("yyyy-MM-dd");
            }

            if (string.IsNullOrEmpty(invoice.DueDate))
            {
                // Set due date to 30 days from invoice date by default
                invoice.DueDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
            }

            // Calculate totals if they're not set
            if (invoice.LineItems != null && invoice.LineItems.Any())
            {
                decimal subtotal = 0;
                foreach (var item in invoice.LineItems)
                {
                    // Calculate item total price if not already set
                    if (item.TotalPrice == 0 && item.Quantity > 0 && item.UnitPrice > 0)
                    {
                        item.TotalPrice = item.Quantity * item.UnitPrice;
                    }
                    subtotal += item.TotalPrice;
                }

                if (invoice.Subtotal == 0)
                {
                    invoice.Subtotal = subtotal;
                }
                // If total is not provided, calculate it from subtotal + tax + shipping
                if (invoice.Total == 0)
                {
                    invoice.Total = invoice.Subtotal + invoice.Tax + invoice.Shipping;
                }
            }

            // Add the invoice to our collection
            _invoices.Add(invoice);

            // Save the updated invoice list
            await SaveInvoicesToFile();

            return invoice;
        }
        catch (Exception ex)
        {
            // Log the exception - in a real app you'd inject a logger here
            Console.WriteLine($"Error in CreateInvoiceAsync: {ex.Message}");
            // Rethrow to let the controller handle it properly
            throw;
        }
    }

    // Update invoice status
    public async Task<Invoice?> UpdateInvoiceStatusAsync(string invoiceNumber, string status, string updatedBy)
    {
        var invoice = GetInvoiceByNumber(invoiceNumber);

        if (invoice == null)
        {
            return null;
        }

        // Track previous status for validation or logging if needed
        string previousStatus = invoice.Status ?? "Unknown";

        // Update invoice status
        invoice.Status = status;

        // Save the updated invoice back to the file
        await SaveInvoicesToFile();

        return invoice;
    }
}