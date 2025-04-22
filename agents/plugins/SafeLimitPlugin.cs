using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;

namespace agents.plugins;

public class SafeLimitPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public SafeLimitPlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5310";
    }

    [KernelFunction("get_user_limit")]
    [Description("Get the approval limit for a specific user by providing their name")]
    public async Task<string> GetUserLimitAsync([Description("The name of the user to check the limit for")] string userName)
    {
        try
        {
            string url = $"{_baseUrl}/api/SafeLimits/name/{Uri.EscapeDataString(userName)}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userLimit = JsonSerializer.Deserialize<SafeLimit>(content, _jsonOptions);

                if (userLimit != null)
                {
                    return $"User: {userLimit.UserName}\n" +
                           $"Role: {userLimit.Role}\n" +
                           $"Approval Limit: ${userLimit.ApprovalLimit:F2} {userLimit.Currency}\n" +
                           $"User ID: {userLimit.UserId}";
                }
                return content;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"User '{userName}' not found in the system.";
            }
            else
            {
                return $"Error retrieving user limit: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Safe Limit API: {ex.Message}";
        }
    }

    [KernelFunction("request_limit_increase")]
    [Description("Request an increase to a user's approval limit")]
    public async Task<string> RequestLimitIncreaseAsync(
        [Description("The user ID for the limit increase")] string userId,
        [Description("The new requested limit amount")] decimal newLimit,
        [Description("Justification for the limit increase")] string justification)
    {
        try
        {
            string url = $"{_baseUrl}/api/SafeLimits/increase";

            var request = new
            {
                UserId = userId,
                NewLimit = newLimit,
                Justification = justification
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var updatedLimit = JsonSerializer.Deserialize<SafeLimit>(content, _jsonOptions);

                if (updatedLimit != null)
                {
                    return $"Limit increase approved for {updatedLimit.UserName}.\n" +
                           $"New approval limit: ${updatedLimit.ApprovalLimit:F2} {updatedLimit.Currency}\n" +
                           $"Justification: {justification}";
                }
                return content;
            }
            else
            {
                return $"Error requesting limit increase: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Safe Limit API: {ex.Message}";
        }
    }
}