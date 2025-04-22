using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;

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

    public PurchaseOrderPlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5294";
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

    [KernelFunction("get_purchase_orders")]
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
}