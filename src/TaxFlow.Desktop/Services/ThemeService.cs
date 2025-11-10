using System.Windows;
using ControlzEx.Theming;

namespace TaxFlow.Desktop.Services;

/// <summary>
/// Theme service implementation using MahApps.Metro
/// </summary>
public class ThemeService : IThemeService
{
    private bool _isDarkTheme;

    public event Action<bool>? ThemeChanged;

    public bool IsDarkTheme => _isDarkTheme;

    public ThemeService()
    {
        // Load saved theme preference (default to light)
        _isDarkTheme = LoadThemePreference();
        ApplyTheme(_isDarkTheme);
    }

    public void SetTheme(bool isDark)
    {
        if (_isDarkTheme == isDark)
            return;

        _isDarkTheme = isDark;
        ApplyTheme(isDark);
        SaveThemePreference(isDark);
        ThemeChanged?.Invoke(isDark);
    }

    private void ApplyTheme(bool isDark)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var theme = ThemeManager.Current.GetTheme(isDark ? "Dark.Steel" : "Light.Steel");

                if (theme != null)
                {
                    ThemeManager.Current.ChangeTheme(Application.Current, theme);
                }
                else
                {
                    // Fallback: Create custom white/black theme
                    var baseTheme = isDark ? "Dark" : "Light";
                    var accentColor = isDark ? "Steel" : "Steel";
                    ThemeManager.Current.ChangeTheme(Application.Current, $"{baseTheme}.{accentColor}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        });
    }

    private bool LoadThemePreference()
    {
        try
        {
            var savedTheme = Properties.Settings.Default.IsDarkTheme;
            return savedTheme;
        }
        catch
        {
            return false; // Default to light theme
        }
    }

    private void SaveThemePreference(bool isDark)
    {
        try
        {
            Properties.Settings.Default.IsDarkTheme = isDark;
            Properties.Settings.Default.Save();
        }
        catch
        {
            // Ignore save errors
        }
    }
}
