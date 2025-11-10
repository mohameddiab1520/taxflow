namespace TaxFlow.Desktop.Services;

/// <summary>
/// Theme service interface for managing application theme
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets whether dark theme is enabled
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Sets the application theme
    /// </summary>
    /// <param name="isDark">True for dark theme, false for light theme</param>
    void SetTheme(bool isDark);

    /// <summary>
    /// Event raised when theme changes
    /// </summary>
    event Action<bool>? ThemeChanged;
}
