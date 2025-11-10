namespace TaxFlow.Desktop.Services;

/// <summary>
/// Localization service interface for managing application language
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current language code
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets whether the current language is RTL (Right-to-Left)
    /// </summary>
    bool IsRightToLeft { get; }

    /// <summary>
    /// Sets the application language
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "en", "ar")</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Gets a localized string by key
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Event raised when language changes
    /// </summary>
    event Action<string>? LanguageChanged;
}
