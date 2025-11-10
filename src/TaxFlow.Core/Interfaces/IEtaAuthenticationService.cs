namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for ETA OAuth 2.0 authentication service
/// </summary>
public interface IEtaAuthenticationService
{
    /// <summary>
    /// Gets a valid access token for ETA API calls
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current access token is valid
    /// </summary>
    Task<bool> IsTokenValidAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token
    /// </summary>
    Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out and invalidates the current session
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
