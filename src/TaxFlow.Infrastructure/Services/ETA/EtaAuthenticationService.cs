using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace TaxFlow.Infrastructure.Services.ETA;

/// <summary>
/// ETA authentication service implementation for OAuth 2.0
/// </summary>
public class EtaAuthenticationService : IEtaAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _tokenExpiryTime;

    // ETA endpoints
    private string EtaAuthUrl => _configuration["ETA:AuthUrl"] ?? "https://id.eta.gov.eg/connect/token";
    private string ClientId => _configuration["ETA:ClientId"] ?? "";
    private string ClientSecret => _configuration["ETA:ClientSecret"] ?? "";
    private string TaxpayerPin => _configuration["ETA:TaxpayerPin"] ?? "";
    private string TaxpayerSecret => _configuration["ETA:TaxpayerSecret"] ?? "";

    public EtaAuthenticationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check if we have a valid token
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiryTime)
        {
            return _accessToken;
        }

        // Authenticate with ETA
        return await AuthenticateAsync(cancellationToken);
    }

    public async Task<bool> IsTokenValidAsync(CancellationToken cancellationToken = default)
    {
        return !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiryTime;
    }

    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        // ETA OAuth 2.0 requires re-authentication (no refresh token)
        return await AuthenticateAsync(cancellationToken);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _accessToken = null;
        _tokenExpiryTime = DateTime.MinValue;
        return Task.CompletedTask;
    }

    private async Task<string> AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Prepare OAuth 2.0 request
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "scope", "InvoicingAPI" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, EtaAuthUrl)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            // Add Basic Authentication for taxpayer credentials
            var taxpayerAuth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{TaxpayerPin}:{TaxpayerSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", taxpayerAuth);

            // Send request
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Log.Error("ETA authentication failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new Exception($"ETA authentication failed: {response.StatusCode}");
            }

            // Parse response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var authResponse = JsonSerializer.Deserialize<EtaAuthResponse>(responseContent);

            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            {
                throw new Exception("Invalid authentication response from ETA");
            }

            // Store token
            _accessToken = authResponse.AccessToken;
            _tokenExpiryTime = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn - 60); // 60s buffer

            Log.Information("Successfully authenticated with ETA. Token expires at {ExpiryTime}",
                _tokenExpiryTime);

            return _accessToken;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error authenticating with ETA");
            throw;
        }
    }
}

/// <summary>
/// ETA authentication response model
/// </summary>
internal class EtaAuthResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
}
