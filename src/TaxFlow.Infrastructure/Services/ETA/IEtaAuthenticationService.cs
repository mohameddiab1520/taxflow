namespace TaxFlow.Infrastructure.Services.ETA;

/// <summary>
/// ETA authentication service interface for OAuth 2.0
/// </summary>
public interface IEtaAuthenticationService
{
    /// <summary>
    /// Authenticates with ETA and retrieves an access token
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current token is valid
    /// </summary>
    Task<bool> IsTokenValidAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token
    /// </summary>
    Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out from ETA
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
