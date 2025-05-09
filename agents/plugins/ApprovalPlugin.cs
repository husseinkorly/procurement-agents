using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;

namespace agents.plugins;

public class ApprovalPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public ApprovalPlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5137";
    }

    [KernelFunction("approve_invoice")]
    [Description("Approve an invoice for payment by providing the invoice number")]
    public async Task<string> ApproveInvoiceAsync(
        [Description("The invoice number to approve")] string invoiceNumber)
    {
        try
        {
            string url = $"{_baseUrl}/api/Approvals/{invoiceNumber}/approve";

            // No need to send approver name, the API will use the one from the invoice
            var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var approvalResponse = JsonSerializer.Deserialize<ApprovalResponse>(responseContent, _jsonOptions);

                if (approvalResponse != null && approvalResponse.Success)
                {
                    return $"Invoice #{invoiceNumber} has been successfully approved using the designated approver.";
                }
                else if (approvalResponse != null)
                {
                    return $"Could not approve invoice: {approvalResponse.Message}";
                }

                return $"Invoice #{invoiceNumber} approval request was submitted, but the result is unclear.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"Invoice #{invoiceNumber} not found in the database.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ApprovalResponse>(errorContent, _jsonOptions);

                if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                {
                    return errorResponse.Message;
                }

                return $"Error approving invoice: Bad request - {errorContent}";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Error approving invoice: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Approval API: {ex.Message}";
        }
    }

    [KernelFunction("get_approval_history")]
    [Description("Get approval history for a specific invoice or all invoices")]
    public async Task<string> GetApprovalHistoryAsync(
        [Description("The invoice number to get approval history for (optional, if not provided returns all history)")] string? invoiceNumber = null)
    {
        try
        {
            // Use a single URL pattern with optional query parameter
            string url = $"{_baseUrl}/api/Approvals/history";

            // Add invoice number as query parameter if provided
            if (!string.IsNullOrEmpty(invoiceNumber))
            {
                url += $"?invoiceNumber={invoiceNumber}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var historyItems = JsonSerializer.Deserialize<List<ApprovalHistory>>(content, _jsonOptions);

                if (historyItems == null || !historyItems.Any())
                {
                    return string.IsNullOrEmpty(invoiceNumber)
                        ? "No approval history found for any invoices."
                        : $"No approval history found for invoice #{invoiceNumber}.";
                }

                string result = string.IsNullOrEmpty(invoiceNumber)
                    ? $"Approval history for all invoices ({historyItems.Count} records):\n\n"
                    : $"Approval history for invoice #{invoiceNumber}:\n\n";

                foreach (var item in historyItems)
                {
                    result += $"Invoice: {item.InvoiceNumber}\n";
                    result += $"Action: {item.Action}\n";
                    result += $"By: {item.ApproverName}\n";
                    result += $"Date: {item.Timestamp:yyyy-MM-dd HH:mm:ss}\n";

                    if (!string.IsNullOrEmpty(item.Comments))
                    {
                        result += $"Comments: {item.Comments}\n";
                    }

                    result += "\n";
                }

                return result;
            }
            else
            {
                return $"Error retrieving approval history: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Approval API: {ex.Message}";
        }
    }
}