using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;
using System.Text;

namespace agents.plugins;

public class InvoicePlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };
    private readonly string _purchaseOrderApiBaseUrl;
    public InvoicePlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5136";
        _purchaseOrderApiBaseUrl = "http://localhost:5294";
    }

    [KernelFunction("get_invoice_details")]
    [Description("Get detailed information about an invoice by providing the invoice number.")]
    public async Task<string> GetInvoiceDetails([Description("The invoice number to retrieve details for")] string invoiceNumber)
    {
        try
        {
            string url = $"{_baseUrl}/api/Invoices/{invoiceNumber}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var invoice = JsonSerializer.Deserialize<Invoice>(content, _jsonOptions);
                
                if (invoice != null)
                {
                    string result = $"Invoice #{invoice.InvoiceNumber}\n";
                    result += $"Purchase Order: {invoice.PurchaseOrderNumber}\n";
                    result += $"Supplier: {invoice.SupplierName} (ID: {invoice.SupplierId})\n";
                    result += $"Invoice Date: {invoice.InvoiceDate}\n";
                    result += $"Due Date: {invoice.DueDate}\n";
                    result += $"Status: {invoice.Status}\n";
                    result += $"Authorized Approver: {invoice.Approver}\n\n";
                    
                    result += "Line Items:\n";
                    if (invoice.LineItems != null)
                    {
                        foreach (var item in invoice.LineItems)
                        {
                            result += $"- {item.Description}: {item.Quantity} x ${item.UnitPrice:F2} = ${item.TotalPrice:F2}\n";
                        }
                    }
                    
                    result += $"\nSubtotal: ${invoice.Subtotal:F2}\n";
                    result += $"Tax: ${invoice.Tax:F2}\n";
                    result += $"Shipping: ${invoice.Shipping:F2}\n";
                    result += $"Total: ${invoice.Total:F2} {invoice.Currency}";
                    
                    return result;
                }
                return content;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"Invoice #{invoiceNumber} not found in the database.";
            }
            else
            {
                return $"Error retrieving invoice data: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Invoice API: {ex.Message}";
        }
    }

    [KernelFunction("list_pending_invoices")]
    [Description("Get a list of all invoices that are pending approval.")]
    public async Task<string> ListPendingInvoices()
    {
        try
        {
            string url = $"{_baseUrl}/api/Invoices/pending";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var pendingInvoices = JsonSerializer.Deserialize<List<Invoice>>(content, _jsonOptions);

                if (pendingInvoices == null || pendingInvoices.Count == 0)
                {
                    return "There are no invoices pending approval.";
                }

                string result = $"Found {pendingInvoices.Count} invoices pending approval:\n\n";

                foreach (var invoice in pendingInvoices)
                {
                    result += $"Invoice #{invoice.InvoiceNumber}\n";
                    result += $"Supplier: {invoice.SupplierName}\n";
                    result += $"Amount: ${invoice.Total:F2}\n";
                    result += $"Due date: {invoice.DueDate}\n";
                    result += $"Authorized Approver: {invoice.Approver}\n\n";
                }

                return result;
            }
            else
            {
                return $"Error retrieving pending invoices: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Invoice API: {ex.Message}";
        }
    }

    [KernelFunction("create_invoice_for_po")]
    [Description("Create an invoice for a given purchase order number.")]
    public async Task<string> CreateInvoiceForPO(
        [Description("The purchase order number to create an invoice for")] string poNumber)
    {
        try
        {
            // First, get the purchase order details
            string poUrl = $"{_purchaseOrderApiBaseUrl}/api/PurchaseOrders/{poNumber}";
            var poResponse = await _httpClient.GetAsync(poUrl);

            if (!poResponse.IsSuccessStatusCode)
            {
                if (poResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return $"Purchase order {poNumber} not found in the database.";
                }
                return $"Error retrieving purchase order data: {poResponse.StatusCode} - {await poResponse.Content.ReadAsStringAsync()}";
            }

            // Deserialize the purchase order
            var poContent = await poResponse.Content.ReadAsStringAsync();
            var purchaseOrder = JsonSerializer.Deserialize<PurchaseOrder>(poContent, _jsonOptions);

            if (purchaseOrder == null)
            {
                return "Error parsing purchase order data.";
            }

            // Validate that purchase order is not closed
            if (purchaseOrder.Status != null && purchaseOrder.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
            {
                return $"Cannot create an invoice for purchase order {poNumber} because it is closed.";
            }

            // Create a new invoice based on the purchase order
            var invoice = new Invoice
            {
                PurchaseOrderNumber = purchaseOrder.PurchaseOrderNumber,
                SupplierName = purchaseOrder.SupplierName,
                SupplierId = purchaseOrder.SupplierId,
                InvoiceDate = DateTime.Now.ToString("yyyy-MM-dd"),
                DueDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"), // Default 30 days payment term
                Currency = "USD",
                Status = "Pending Approval",
                LineItems = purchaseOrder.LineItems?.Select(poItem => new LineItem
                {
                    ItemId = poItem.ItemId,
                    Description = poItem.Description,
                    Quantity = poItem.Quantity,
                    UnitPrice = poItem.UnitPrice,
                    TotalPrice = poItem.TotalPrice
                }).ToList(),
                Subtotal = purchaseOrder.Subtotal,
                Tax = purchaseOrder.Tax,
                Shipping = purchaseOrder.Shipping,
                Total = purchaseOrder.Total
            };

            // Assign an approver based on total amount (this logic could be more complex in real systems)
            if (invoice.Total <= 10000)
            {
                invoice.Approver = "JuniorApprover";
            }
            else if (invoice.Total <= 50000)
            {
                invoice.Approver = "SeniorApprover";
            }
            else
            {
                invoice.Approver = "ExecutiveApprover";
            }

            // Generate a unique invoice number
            invoice.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{poNumber}";

            // Create the invoice via POST to the invoice API
            string createUrl = $"{_baseUrl}/api/Invoices";
            
            // Log what we're sending to help debug
            string jsonPayload = JsonSerializer.Serialize(invoice, _jsonOptions);
            
            var jsonContent = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json");

            // Make the API call
            var createResponse = await _httpClient.PostAsync(createUrl, jsonContent);

            // Check if the request was successful
            if (createResponse.IsSuccessStatusCode)
            {
                var createdContent = await createResponse.Content.ReadAsStringAsync();
                
                // Try to deserialize the response, but if it fails, still return a success message
                try
                {
                    var createdInvoice = JsonSerializer.Deserialize<Invoice>(createdContent, _jsonOptions);
                    
                    if (createdInvoice != null)
                    {
                        return $"Invoice #{createdInvoice.InvoiceNumber} was successfully created for purchase order {poNumber}.\n" +
                               $"Total amount: ${createdInvoice.Total:F2}\n" +
                               $"Status: {createdInvoice.Status}\n" +
                               $"Assigned approver: {createdInvoice.Approver}";
                    }
                }
                catch (JsonException jsonEx)
                {
                    // If we can't deserialize the response but the status was successful,
                    // still report success but include the raw response
                    return $"Invoice for purchase order {poNumber} was created successfully. " +
                           $"Response: {createdContent} (Warning: Could not parse response: {jsonEx.Message})";
                }
                
                // If we get here, the status was success but we couldn't parse the invoice details
                return $"Invoice for purchase order {poNumber} was created successfully. Raw response: {createdContent}";
            }
            else
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                return $"Error creating invoice: HTTP {(int)createResponse.StatusCode} ({createResponse.StatusCode}) - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            // Include more details about the exception
            string exDetails = ex.InnerException != null ? 
                $"{ex.Message} | Inner exception: {ex.InnerException.Message}" : 
                ex.Message;
                
            return $"Error creating invoice for purchase order: {exDetails}";
        }
    }
}

