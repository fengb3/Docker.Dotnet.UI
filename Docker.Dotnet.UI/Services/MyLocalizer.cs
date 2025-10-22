using Microsoft.Extensions.Localization;

namespace Docker.Dotnet.UI.Services;

public class MyLocalizer(UserPreferencesService preferencesService) : IStringLocalizer
{
    private string? _cachedLanguage;
    
    /// <summary>
    /// Event triggered when language is changed. Components can subscribe to this to refresh UI.
    /// </summary>
    public event Action? LanguageChanged;

    private string CurrentLanguageCode
    {
        get
        {
            // Use cached value if available to avoid repeated async calls
            if (_cachedLanguage != null)
                return _cachedLanguage;

            // Synchronously get from service (will use default if not yet loaded)
            // The language will be properly set when the app initializes
            return "en-us";
        }
    }

    // public async Task<string> GetCurrentLanguageAsync()
    // {
    //     if (_cachedLanguage == null)
    //     {
    //         _cachedLanguage = preferencesService.Language;
    //     }
    //     return _cachedLanguage;
    // }

    public async Task SetLanguageAsync(string language)
    {
        _cachedLanguage = language;
        await preferencesService.Set(p => p.Language).To(language);
        
        // Notify all subscribers that language has changed
        LanguageChanged?.Invoke();
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var localizedStrings = ImmutableTables.Localization.Items.Values.Select(data =>
            new LocalizedString(data.ResourceKey, data[CurrentLanguageCode].ToString() ?? data.ResourceKey, false));

        return localizedStrings;
    }

    /// <inheritdoc/>
    public LocalizedString this[string name]
    {
        get
        {
            if (!ImmutableTables.Localization.Items.TryGetValue(name, out var data))
                return new LocalizedString(name, $"[LOCALIZATION KEY '{name}' NOT FOUND]", true);
            
            var value = data[CurrentLanguageCode]?.ToString() ?? name;
            return new LocalizedString(name, value, false);

        }
    }

    /// <inheritdoc/>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (!ImmutableTables.Localization.Items.TryGetValue(name, out var data))
                return new LocalizedString(name, $"[LOCALIZATION KEY '{name}' NOT FOUND]", true);
            
            var format = data[CurrentLanguageCode]?.ToString() ?? name;
            var value = string.Format(format, arguments);
            return new LocalizedString(name, value, false);
        }
    }
}