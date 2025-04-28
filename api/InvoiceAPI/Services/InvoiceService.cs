using InvoiceAPI.Models;
using InvoiceAPI.Repositories;

namespace InvoiceAPI.Services;

public class InvoiceService
{
    private readonly ICosmosDbRepository<Invoice> _invoiceRepository;
    private readonly ILogger<InvoiceService> _logger;
    private List<Invoice> _invoices = []; // In-memory cache

    public InvoiceService(
        ICosmosDbRepository<Invoice> invoiceRepository,
        ILogger<InvoiceService> logger,
        IConfiguration configuration)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;

        // Initialize and load invoices asynchronously
        InitializeRepositoryAndLoadInvoices().GetAwaiter().GetResult();
    }

    // Initialize the repository and load invoices
    private async Task InitializeRepositoryAndLoadInvoices()
    {
        try
        {
            // Initialize the repository (create database and container if they don't exist)
            await _invoiceRepository.InitializeAsync();

            // Load all invoices from Cosmos DB to in-memory cache
            _invoices = (await _invoiceRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Loaded {Count} invoices from Cosmos DB", _invoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing repository and loading invoices");
            _invoices = [];
        }
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

    // Create a new invoice or update a draft
    public async Task<Invoice?> CreateInvoiceAsync(Invoice invoice)
    {
        try
        {
            // Check if an invoice with the same number already exists
            var existingInvoice = _invoices.FirstOrDefault(i =>
                i.InvoiceNumber != null &&
                i.InvoiceNumber.Equals(invoice.InvoiceNumber, StringComparison.OrdinalIgnoreCase));

            if (existingInvoice != null)
            {
                // If existing invoice is not a draft, reject the operation
                if (existingInvoice.Status != "Draft")
                {
                    throw new InvalidOperationException($"Invoice with number {invoice.InvoiceNumber} already exists.");
                }
                // If it's a draft, we'll update it with new data instead
                _logger.LogInformation("Updating existing draft invoice: {InvoiceNumber}", invoice.InvoiceNumber);
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

            // setting MicrosoftInvoiceNumber to random 10 digits if not already set
            if (string.IsNullOrEmpty(invoice.MicrosoftInvoiceNumber))
            {
                invoice.MicrosoftInvoiceNumber = "57" + new Random().Next(10000000, 99999999).ToString();
            }

            Invoice resultInvoice;

            // Update or create in Cosmos DB based on whether it's an existing draft
            if (existingInvoice != null)
            {
                // Update the existing draft invoice
                resultInvoice = await _invoiceRepository.UpdateAsync(invoice, invoice.InvoiceNumber!, invoice.InvoiceNumber!);

                // Update in-memory cache
                var index = _invoices.FindIndex(i => i.InvoiceNumber == invoice.InvoiceNumber);
                if (index >= 0)
                {
                    _invoices[index] = resultInvoice;
                }
            }
            else
            {
                invoice.id = Guid.NewGuid().ToString();
                // Create new invoice in Cosmos DB
                resultInvoice = await _invoiceRepository.CreateAsync(invoice);
                // Add to in-memory cache
                _invoices.Add(resultInvoice);
            }

            return resultInvoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateInvoiceAsync: {Message}", ex.Message);
            throw;
        }
    }

    // Update invoice status
    public async Task<Invoice?> UpdateInvoiceStatusAsync(string invoiceNumber, string status)
    {
        try
        {
            var invoice = GetInvoiceByNumber(invoiceNumber);

            if (invoice == null)
            {
                return null;
            }
            // Update invoice status
            invoice.Status = status;

            // Update in Cosmos DB - use InvoiceNumber as both id and partition key
            var updatedInvoice = await _invoiceRepository.UpdateAsync(invoice, invoiceNumber, invoiceNumber);

            // Update in-memory cache
            var index = _invoices.FindIndex(i => i.InvoiceNumber == invoiceNumber);
            if (index >= 0)
            {
                _invoices[index] = updatedInvoice;
            }

            return updatedInvoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice status: {Message}", ex.Message);
            throw;
        }
    }
}