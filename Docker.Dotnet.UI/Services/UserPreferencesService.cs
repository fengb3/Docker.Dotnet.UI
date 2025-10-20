using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using AutoRegisterInject;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Docker.Dotnet.UI.Services;

/// <summary>
/// Service for managing user preferences stored in browser's protected local storage.
/// Supports expression-based fluent API: await preferences.Set(p => p.Language).To("en-us")
/// Thread-safe with automatic lazy loading - preferences are loaded on first access.
/// </summary>
[RegisterScoped(typeof(UserPreferencesService))]
public class UserPreferencesService(ProtectedLocalStorage localStorage, ILogger<UserPreferencesService> logger)
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _isLoaded;
    
    // Define your preference properties here - storage keys are auto-generated from property names
    public string Language
    {
        get => GetCachedValue("en-us");
        set => SetCachedValue(value);
    }
    
    public bool IsDarkMode
    {
        get => GetCachedValue(false);
        set => SetCachedValue(value);
    }
    
    /// <summary>
    /// Loads all user preferences from browser storage into the cache.
    /// Called automatically on first property access. Can be called manually for eager loading.
    /// </summary>
    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;
        
        await _loadLock.WaitAsync();
        try
        {
            if (_isLoaded) return; // Double-check after acquiring lock
            
            // Load Language preference
            await LoadPreferenceAsync(nameof(Language), "en-us", async () =>
            {
                var result = await localStorage.GetAsync<string>(GetStorageKey(nameof(Language)));
                return result.Success ? result.Value ?? "en-us" : "en-us";
            });
            
            // Load IsDarkMode preference
            await LoadPreferenceAsync(nameof(IsDarkMode), false, async () =>
            {
                var result = await localStorage.GetAsync<bool>(GetStorageKey(nameof(IsDarkMode)));
                return result.Success && result.Value;
            });
            
            _isLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }
    
    /// <summary>
    /// Helper method to load a single preference with error handling.
    /// </summary>
    private async Task LoadPreferenceAsync<T>(string propertyName, T defaultValue, Func<Task<T>> loader)
    {
        try
        {
            var value = await loader();
            _cache[propertyName] = value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load preference '{Preference}', using default value", propertyName);
            _cache[propertyName] = defaultValue;
        }
    }
    
    /// <summary>
    /// Gets a cached value or returns the default if not found.
    /// Does NOT trigger automatic loading - call EnsureLoadedAsync() manually in OnAfterRenderAsync.
    /// </summary>
    private T GetCachedValue<T>(T defaultValue, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName))
            return defaultValue;
            
        return _cache.TryGetValue(propertyName, out var value) && value is T typedValue 
            ? typedValue 
            : defaultValue;
    }
    
    /// <summary>
    /// Sets a cached value (in-memory only, does not persist).
    /// </summary>
    private void SetCachedValue<T>(T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!string.IsNullOrEmpty(propertyName))
        {
            _cache[propertyName] = value;
        }
    }
    
    /// <summary>
    /// Saves a preference value to both cache and browser storage.
    /// </summary>
    internal async Task SavePreferenceAsync<T>(string propertyName, T value)
    {
        // Ensure loaded before saving (in case this is the first operation)
        await EnsureLoadedAsync();
        
        // Update cache
        _cache[propertyName] = value;
        
        // Persist to storage
        var storageKey = GetStorageKey(propertyName);
        if (value != null)
        {
            await localStorage.SetAsync(storageKey, value);
        }
    }
    
    /// <summary>
    /// Converts property name to storage key (snake_case).
    /// Example: "IsDarkMode" -> "user_is_dark_mode"
    /// </summary>
    private static string GetStorageKey(string propertyName)
    {
        // Convert PascalCase to snake_case
        var snakeCase = string.Concat(
            propertyName.Select((c, i) => i > 0 && char.IsUpper(c) 
                ? "_" + char.ToLower(c) 
                : char.ToLower(c).ToString())
        );
        return $"user_{snakeCase}";
    }
}

public static class UserPreferencesServiceExtensions
{
    /// <summary>
    /// Creates a fluent setter for a user preference property.
    /// Usage: await preferences.Set(p => p.Language).To("en-us")
    /// </summary>
    public static UserPreferenceSetter<T> Set<T>(this UserPreferencesService service, Expression<Func<UserPreferencesService, T>> propertyExpression)
    {
        return new UserPreferenceSetter<T>(service, propertyExpression);
    }

    /// <summary>
    /// Fluent API helper for setting user preferences asynchronously.
    /// </summary>
    public class UserPreferenceSetter<T>
    {
        private readonly UserPreferencesService _service;
        private readonly PropertyInfo _propertyInfo;

        public UserPreferenceSetter(UserPreferencesService service, Expression<Func<UserPreferencesService, T>> propertyExpression)
        {
            _service = service;
            
            // Extract the property info from the expression
            MemberExpression memberExpression = (MemberExpression)(propertyExpression.Body is UnaryExpression unary 
                ? unary.Operand 
                : propertyExpression.Body);
            
            _propertyInfo = (PropertyInfo)memberExpression.Member;
        }

        /// <summary>
        /// Sets the preference value both in cache and persists to browser storage.
        /// </summary>
        public async Task To(T value)
        {
            // Update the property (cache)
            _propertyInfo.SetValue(_service, value);
            
            // Persist to storage
            await _service.SavePreferenceAsync(_propertyInfo.Name, value);
        }
    }
}
