using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;
using System.Text;

namespace agents.plugins;

public class PurchaseOrderPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    private readonly string _invoiceApiBaseUrl;
    public PurchaseOrderPlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5294";
        _invoiceApiBaseUrl = "http://localhost:5136";
    }

    [KernelFunction("get_purchase_order")]
    [Description("Get a purchase order by providing the purchase order number.")]
    public async Task<string> GetPurchaseOrder(
        [Description("The purchase order number")] string poNumber)
    {
        try
        {
            string url = $"{_baseUrl}/api/PurchaseOrders/{poNumber}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"Purchase order {poNumber} not found in the database.";
            }
            else
            {
                return $"Error retrieving purchase order data: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Purchase Order API: {ex.Message}";
        }
    }

    [KernelFunction("get_purchase_orders_ready_for_invoicing")]
    [Description("Get all purchase orders, with optional filtering by status.")]
    public async Task<string> GetPurchaseOrders(
        [Description("Filter by purchase order status (optional, e.g., 'Open', 'Closed')")] string? status = null)
    {
        try
        {
            string url = $"{_baseUrl}/api/PurchaseOrders";

            if (!string.IsNullOrEmpty(status))
            {
                url += $"?status={status}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var purchaseOrders = JsonSerializer.Deserialize<List<PurchaseOrder>>(content, _jsonOptions);

                if (purchaseOrders == null || purchaseOrders.Count == 0)
                {
                    return string.IsNullOrEmpty(status)
                        ? "No purchase orders found in the database."
                        : $"No purchase orders with status '{status}' found in the database.";
                }

                string result = $"Found {purchaseOrders.Count} purchase orders";
                if (!string.IsNullOrEmpty(status))
                {
                    result += $" with status '{status}'";
                }
                result += ":\n\n";

                foreach (var po in purchaseOrders)
                {
                    result += $"PO #: {po.PurchaseOrderNumber}\n";
                    result += $"Supplier: {po.SupplierName}\n";
                    result += $"Status: {po.Status}\n";
                    result += $"Total: ${po.Total:F2}\n\n";
                }

                return result;
            }
            else
            {
                return $"Error retrieving purchase orders: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Purchase Order API: {ex.Message}";
        }
    }

    [KernelFunction("update_purchase_order_status")]
    [Description("Update the status of a purchase order.")]
    public async Task<string> UpdatePurchaseOrderStatus(
        [Description("The purchase order number")] string poNumber,
        [Description("The new status for the purchase order (e.g., 'Open', 'Closed')")] string status)
    {
        try
        {
            string url = $"{_baseUrl}/api/PurchaseOrders/{poNumber}/status?status={status}";

            var response = await _httpClient.PutAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"Purchase order {poNumber} not found in the database.";
            }
            else
            {
                return $"Error updating purchase order status: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Purchase Order API: {ex.Message}";
        }
    }

    [KernelFunction("get_purchase_orders_with_draft_invoices")]
    [Description("Get all purchase orders that have an invoice with draft status. They are ready for review")]
    public async Task<string> GetPurchaseOrdersWithDraftInvoices()
    {
        try
        {
            // First get all purchase orders
            string poUrl = $"{_baseUrl}/api/PurchaseOrders?status=Open";
            var poResponse = await _httpClient.GetAsync(poUrl);

            if (!poResponse.IsSuccessStatusCode)
            {
                return $"Error retrieving purchase orders: {poResponse.StatusCode} - {await poResponse.Content.ReadAsStringAsync()}";
            }
            
            var poContent = await poResponse.Content.ReadAsStringAsync();
            var allPurchaseOrders = JsonSerializer.Deserialize<List<PurchaseOrder>>(poContent, _jsonOptions);
            
            if (allPurchaseOrders == null || allPurchaseOrders.Count == 0)
            {
                return "No purchase orders found in the database.";
            }
            
            // Format the response
            StringBuilder result = new StringBuilder();
            var purchaseOrdersWithDraftInvoices = 0;
            var totalDraftInvoiceCount = 0;
            
            
            // Process each purchase order
            foreach (var po in allPurchaseOrders)
            {
                // Check if this PO has draft invoices using the correct endpoint
                string invoiceUrl = $"{_invoiceApiBaseUrl}/api/Invoices/po/{po.PurchaseOrderNumber}?status=Draft";
                var invoiceResponse = await _httpClient.GetAsync(invoiceUrl);
                
                if (!invoiceResponse.IsSuccessStatusCode)
                {
                    // If it's a 404, this means no invoices found, which is fine - continue to next PO
                    if (invoiceResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    return $"Error retrieving invoices for PO {po.PurchaseOrderNumber}: {invoiceResponse.StatusCode}";
                }

                var invoiceContent = await invoiceResponse.Content.ReadAsStringAsync();
                var draftInvoices = JsonSerializer.Deserialize<List<Invoice>>(invoiceContent, _jsonOptions);

                // Check if there are any draft invoices
                if (draftInvoices != null && draftInvoices.Count > 0)
                {
                    purchaseOrdersWithDraftInvoices++;
                    totalDraftInvoiceCount += draftInvoices.Count;
                    
                    result.AppendLine($"    PO #: {po.PurchaseOrderNumber}");
                    result.AppendLine($"    Supplier: {po.SupplierName}");
                    result.AppendLine($"    Draft Invoices: {draftInvoices.Count}");
                    result.AppendLine($"    Status: {po.Status}");
                    result.AppendLine($"    Total: ${po.Total:F2}");
                    result.AppendLine();
                }
            }
            
            // Check if there are any POs with draft invoices
            if (purchaseOrdersWithDraftInvoices == 0)
            {
                return "No purchase orders with draft invoices found.";
            }
            
            // Display the results
            result.AppendLine($"Found {purchaseOrdersWithDraftInvoices} purchase orders with {totalDraftInvoiceCount} total draft invoices:");
            result.AppendLine();
            
            
            // Add a prompt for the user to select a PO
            result.AppendLine("Which purchase order would you like to view draft invoices for? Please respond with the PO number.");
            result.AppendLine("For example: \"I want to see draft invoices for PO 5100063118\"");
            result.AppendLine();
            result.AppendLine("You can use the get_draft_invoices_by_po function to view and edit draft invoices for a specific PO.");
            
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving purchase orders with draft invoices: {ex.Message}";
        }
    }
}