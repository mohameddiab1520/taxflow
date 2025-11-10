using System.Globalization;
using System.Windows;

namespace TaxFlow.Desktop.Services;

/// <summary>
/// Localization service implementation
/// </summary>
public class LocalizationService : ILocalizationService
{
    private string _currentLanguage;
    private readonly Dictionary<string, string> _rtlLanguages = new() { { "ar", "Arabic" } };

    public event Action<string>? LanguageChanged;

    public string CurrentLanguage => _currentLanguage;

    public bool IsRightToLeft => _rtlLanguages.ContainsKey(_currentLanguage);

    public LocalizationService()
    {
        // Load saved language preference (default to English)
        _currentLanguage = LoadLanguagePreference();
        ApplyLanguage(_currentLanguage);
    }

    public void SetLanguage(string languageCode)
    {
        if (_currentLanguage == languageCode)
            return;

        _currentLanguage = languageCode;
        ApplyLanguage(languageCode);
        SaveLanguagePreference(languageCode);
        LanguageChanged?.Invoke(languageCode);
    }

    public string GetString(string key)
    {
        try
        {
            // Try to get the string from resources
            var resourceDictionary = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains($"Strings.{_currentLanguage}") == true);

            if (resourceDictionary != null && resourceDictionary.Contains(key))
            {
                return resourceDictionary[key]?.ToString() ?? key;
            }

            return key;
        }
        catch
        {
            return key;
        }
    }

    private void ApplyLanguage(string languageCode)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                // Set culture
                var culture = new CultureInfo(languageCode);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                // Set flow direction
                var flowDirection = IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                FrameworkElement.FlowDirectionProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(flowDirection));

                // Load language resource dictionary
                var langDict = new ResourceDictionary
                {
                    Source = new Uri($"Resources/Strings/Strings.{languageCode}.xaml", UriKind.Relative)
                };

                // Remove old language dictionaries
                var oldDictionaries = Application.Current.Resources.MergedDictionaries
                    .Where(d => d.Source?.OriginalString.Contains("Strings.") == true)
                    .ToList();

                foreach (var dict in oldDictionaries)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(dict);
                }

                // Add new language dictionary
                Application.Current.Resources.MergedDictionaries.Add(langDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying language: {ex.Message}");
            }
        });
    }

    private string LoadLanguagePreference()
    {
        try
        {
            var savedLanguage = Properties.Settings.Default.Language;
            return string.IsNullOrEmpty(savedLanguage) ? "en" : savedLanguage;
        }
        catch
        {
            return "en";
        }
    }

    private void SaveLanguagePreference(string languageCode)
    {
        try
        {
            Properties.Settings.Default.Language = languageCode;
            Properties.Settings.Default.Save();
        }
        catch
        {
            // Ignore save errors
        }
    }
}
