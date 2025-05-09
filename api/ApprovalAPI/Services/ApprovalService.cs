using System.Text.Json;
using ApprovalAPI.Models;
using ApprovalAPI.Repositories;

namespace ApprovalAPI.Services;

public class ApprovalService
{
    private readonly ICosmosDbRepository<ApprovalHistory> _approvalRepository;
    private readonly ILogger<ApprovalService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApprovalService(
        ICosmosDbRepository<ApprovalHistory> approvalRepository,
        ILogger<ApprovalService> logger,
        IConfiguration configuration)
    {
        _approvalRepository = approvalRepository;
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Initialize the repository asynchronously
        _approvalRepository.InitializeAsync().GetAwaiter().GetResult();
    }

    // Update the method signature to no longer require approverName parameter
    public async Task<ApprovalResponse> ApproveInvoiceAsync(string invoiceNumber)
    {
        _logger.LogInformation("Processing approval request for invoice: {InvoiceNumber}", invoiceNumber);

        var response = new ApprovalResponse
        {
            InvoiceNumber = invoiceNumber,
            Success = false
        };

        try
        {
            // Step 1: Get the invoice details from the InvoiceAPI
            string invoiceApiUrl = _configuration["ApiEndpoints:InvoiceApi"] ?? "http://localhost:5136";
            var invoiceUrl = $"{invoiceApiUrl}/api/Invoices/{invoiceNumber}";

            var invoiceResponse = await _httpClient.GetAsync(invoiceUrl);
            if (!invoiceResponse.IsSuccessStatusCode)
            {
                response.Message = $"Invoice {invoiceNumber} not found or cannot be retrieved";
                return response;
            }
            var invoiceContent = await invoiceResponse.Content.ReadAsStringAsync();
            var invoice = JsonSerializer.Deserialize<Invoice>(invoiceContent, _jsonOptions);
            if (invoice == null)
            {
                response.Message = "Error processing invoice data";
                return response;
            }

            // Get the approver name from the invoice
            string approverName = invoice.Approver ?? string.Empty;
            if (string.IsNullOrEmpty(approverName))
            {
                response.Message = "No approver designated for this invoice";
                return response;
            }
            _logger.LogInformation("Using designated approver from invoice: {ApproverName}", approverName);

            // Step 2: Check if invoice is already approved
            if (invoice.Status?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true)
            {
                response.Message = $"Invoice {invoiceNumber} is already approved";
                response.Status = "Approved";
                return response;
            }

            // Step 3: Check if invoice is in pending approval status
            if (invoice.Status == null || !invoice.Status.Equals("Pending Approval", StringComparison.OrdinalIgnoreCase))
            {
                response.Message = $"Invoice {invoiceNumber} is not in 'Pending Approval' status";
                response.Status = invoice.Status;
                return response;
            }

            // Step 4: Check if the approver has sufficient safe limit
            string safeLimitApiUrl = _configuration["ApiEndpoints:SafeLimitApi"] ?? "http://localhost:5192";
            var checkUrl = $"{safeLimitApiUrl}/api/SafeLimits/check";
            try
            {
                var checkRequest = new
                {
                    UserName = approverName,
                    InvoiceAmount = invoice.Total
                };

                var checkResponse = await _httpClient.PostAsJsonAsync(checkUrl, checkRequest);
                if (!checkResponse.IsSuccessStatusCode)
                {
                    response.Message = $"Failed to verify approval limit: {checkResponse.StatusCode}";
                    return response;
                }

                var checkContent = await checkResponse.Content.ReadAsStringAsync();
                var limitResult = JsonSerializer.Deserialize<ApprovalLimitCheckResult>(checkContent, _jsonOptions);

                if (limitResult != null && !limitResult.CanApprove)
                {
                    response.Message = $"Approval limit exceeded. {approverName} is not authorized to approve invoices of ${invoice.Total}";
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking safe limit for user {UserName}", approverName);
                response.Message = $"Error checking approval limit: {ex.Message}";
                return response;
            }

            // Step 5: Validate Purchase Order status (should not be closed)
            string poApiUrl = _configuration["ApiEndpoints:PurchaseOrderApi"] ?? "http://localhost:5294";
            var poUrl = $"{poApiUrl}/api/PurchaseOrders/{invoice.PurchaseOrderNumber}";
            try
            {
                var poResponse = await _httpClient.GetAsync(poUrl);
                if (!poResponse.IsSuccessStatusCode)
                {
                    response.Message = $"Unable to validate purchase order {invoice.PurchaseOrderNumber}";
                    return response;
                }
                var poContent = await poResponse.Content.ReadAsStringAsync();
                var purchaseOrder = JsonSerializer.Deserialize<PurchaseOrder>(poContent, _jsonOptions);
                if (purchaseOrder?.Status?.Equals("Closed", StringComparison.OrdinalIgnoreCase) == true)
                {
                    response.Message = $"Cannot approve invoice {invoiceNumber} because purchase order {invoice.PurchaseOrderNumber} is closed";
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating purchase order {PONumber}", invoice.PurchaseOrderNumber);
                response.Message = $"Error validating purchase order: {ex.Message}";
                return response;
            }

            // Step 6: Validate that all goods for the invoice items are received
            string gsrApiUrl = _configuration["ApiEndpoints:GoodsReceivedApi"] ?? "http://localhost:5284";

            // If AutoCore is true, skip the goods received validation
            if (invoice.AutoCore == true)
            {
                _logger.LogInformation("Invoice {InvoiceNumber} has AutoCore enabled, skipping goods received validation", invoiceNumber);
            }
            else
            {
                foreach (var item in invoice.LineItems)
                {
                    var gsrUrl = $"{gsrApiUrl}/api/GoodsReceived/po/{invoice.PurchaseOrderNumber}?itemId={item.ItemId}";
                    try
                    {
                        var gsrResponse = await _httpClient.GetAsync(gsrUrl);
                        // If the goods received data is not found or there's an error, reject the approval
                        if (!gsrResponse.IsSuccessStatusCode)
                        {
                            response.Message = $"Cannot approve invoice {invoiceNumber}. Goods received data for item {item.ItemId} not found.";
                            return response;
                        }
                        var gsrContent = await gsrResponse.Content.ReadAsStringAsync();
                        var goodsItems = JsonSerializer.Deserialize<List<GoodsReceivedItem>>(gsrContent, _jsonOptions);
                        // Check if any goods for this item are not received
                        if (goodsItems == null || !goodsItems.Any() || goodsItems.Any(g => g.Status != "Received"))
                        {
                            response.Message = $"Cannot approve invoice {invoiceNumber}. Goods for item {item.ItemId} are not marked as received.";
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error validating goods received for item {ItemId}", item.ItemId);
                        response.Message = $"Error validating goods received: {ex.Message}";
                        return response;
                    }
                }
            }

            // Step 7: All validation checks passed, update the invoice status by calling the InvoiceAPI
            var updateUrl = $"{invoiceApiUrl}/api/Invoices/{invoiceNumber}/status";
            string newStatus = "Approved";

            var updateResponse = await _httpClient.PutAsJsonAsync(updateUrl, newStatus);

            if (!updateResponse.IsSuccessStatusCode)
            {
                response.Message = $"Failed to update invoice status: {updateResponse.StatusCode}";
                return response;
            }

            // Record the approval in our history
            await RecordApprovalAsync(invoiceNumber, approverName, "Approved");

            // Return success response
            response.Success = true;
            response.Status = "Approved";
            response.Message = $"Invoice {invoiceNumber} has been successfully approved by {approverName}";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval for invoice {InvoiceNumber}", invoiceNumber);
            response.Message = $"An error occurred while processing the approval: {ex.Message}";
            return response;
        }
    }

    public async Task<List<ApprovalHistory>> GetApprovalHistoryAsync(string? invoiceNumber = null)
    {
        try
        {
            if (string.IsNullOrEmpty(invoiceNumber))
            {
                // Get all approval records
                var allHistory = await _approvalRepository.GetAllAsync();
                return allHistory.ToList();
            }
            else
            {
                // Query with a filter for a specific invoice number
                string query = $"SELECT * FROM c WHERE c.invoiceNumber = '{invoiceNumber}'";
                var filteredHistory = await _approvalRepository.GetAllAsync(query);
                return filteredHistory.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval history: {Message}", ex.Message);
            // Return empty list in case of error
            return new List<ApprovalHistory>();
        }
    }

    private async Task RecordApprovalAsync(string invoiceNumber, string approverName, string action, string? comments = null)
    {
        try
        {
            var approvalHistory = new ApprovalHistory
            {
                id = Guid.NewGuid().ToString(),
                InvoiceNumber = invoiceNumber,
                ApproverName = approverName,
                Action = action,
                Comments = comments
            };

            await _approvalRepository.CreateAsync(approvalHistory);
            _logger.LogInformation("Approval record created for invoice {InvoiceNumber}", invoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording approval for invoice {InvoiceNumber}: {Message}",
                invoiceNumber, ex.Message);
            throw;
        }
    }
}