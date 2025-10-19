using Microsoft.Extensions.Localization;

namespace Docker.Dotnet.UI.Services;

public class MyLocalizer : IStringLocalizer
{
    private string CurrentLanguageCode => "en-us"; // TODO: get current language code

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
                return new LocalizedString(name, name + "NOT_FOUND", true);
            
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
                return new LocalizedString(name, name + "NOT_FOUND", true);
            
            var format = data[CurrentLanguageCode]?.ToString() ?? name;
            var value = string.Format(format, arguments);
            return new LocalizedString(name, value, false);

        }
    }
}