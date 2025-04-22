using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;

namespace agents.plugins;

public class InvoicePlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null // Don't use camelCase policy to match the mixed-case in the JSON
    };

    public InvoicePlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5136";
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

    [KernelFunction("approve_invoice")]
    [Description("Approve an invoice for payment by providing the invoice number and approver name")]
    public async Task<string> ApproveInvoiceAsync(
        [Description("The invoice number to approve")] string invoiceNumber,
        [Description("The name of the person approving the invoice")] string approverName)
    {
        try
        {
            string url = $"{_baseUrl}/api/Invoices/{invoiceNumber}/approve";

            // Create approval request with approver name
            var approvalRequest = new { ApproverName = approverName };
            var content = new StringContent(
                JsonSerializer.Serialize(approvalRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return $"Invoice #{invoiceNumber} has been successfully approved by {approverName}.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"Invoice #{invoiceNumber} not found in the database.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Error approving invoice: {response.StatusCode} - {errorContent}";
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
}

