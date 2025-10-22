using Docker.DotNet;
using Docker.DotNet.Models;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(NetworksPageViewModel))]
public class NetworksPageViewModel(DockerClient dockerClient) : ViewModel
{
    

    public override async Task InitializeAsync()
    {
        await RefreshNetworksAsync();
    }

    private IList<NetworkListItemViewModel>? _allNetworks;
    public IList<NetworkListItemViewModel>? Networks { get; set; }
    
    // Search/Filter state
    public string SearchText { get; set; } = string.Empty;
    
    // Batch operations state
    public HashSet<string> SelectedNetworkIds { get; } = new();
    public bool SelectAll { get; set; }

    // Dialog states
    public bool ShowCreateDialog { get; set; }
    public bool ShowInspectDialog { get; set; }
    public string? SelectedNetworkId { get; set; }
    public string? SelectedNetworkName { get; set; }
    public string? InspectJson { get; set; }

    // Create network properties
    public string NewNetworkName { get; set; } = string.Empty;
    public string NewNetworkDriver { get; set; } = "bridge";
    public bool IsCreating { get; set; }

    public DialogOptions DialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };

    public async Task RefreshNetworksAsync()
    {
        var networks = await dockerClient.Networks.ListNetworksAsync();
        _allNetworks = networks.ToViewModel();
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        if (_allNetworks == null)
        {
            Networks = null;
            return;
        }

        var filtered = _allNetworks.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(n =>
                n.Name.ToLowerInvariant().Contains(searchLower) ||
                n.Driver.ToLowerInvariant().Contains(searchLower) ||
                n.ShortId.ToLowerInvariant().Contains(searchLower)
            );
        }

        Networks = filtered.ToList();
        NotifyStateChanged();
    }

    public async Task DeleteNetworkAsync(string networkId)
    {
        await dockerClient.Networks.DeleteNetworkAsync(networkId);
        await RefreshNetworksAsync();
    }

    public void OpenCreateDialog()
    {
        ShowCreateDialog = true;
        NewNetworkName = string.Empty;
        NewNetworkDriver = "bridge";
        NotifyStateChanged();
    }

    public void CloseCreateDialog()
    {
        ShowCreateDialog = false;
        NotifyStateChanged();
    }

    public async Task CreateNetworkAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNetworkName))
            return;

        IsCreating = true;
        NotifyStateChanged();

        try
        {
            await dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = NewNetworkName,
                Driver = NewNetworkDriver,
            });

            await RefreshNetworksAsync();
            CloseCreateDialog();
        }
        catch (Exception)
        {
            // Handle error - could add error message display
            throw;
        }
        finally
        {
            IsCreating = false;
            NotifyStateChanged();
        }
    }

    public async Task ShowInspectAsync(string networkId, string networkName)
    {
        SelectedNetworkId = networkId;
        SelectedNetworkName = networkName;
        ShowInspectDialog = true;
        NotifyStateChanged();

        try
        {
            var inspect = await dockerClient.Networks.InspectNetworkAsync(networkId);
            InspectJson = System.Text.Json.JsonSerializer.Serialize(
                inspect,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            InspectJson = $"Error inspecting network: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void CloseInspectDialog()
    {
        ShowInspectDialog = false;
        InspectJson = null;
        NotifyStateChanged();
    }

    public void ToggleSelectAll()
    {
        SelectAll = !SelectAll;
        SelectedNetworkIds.Clear();
        
        if (SelectAll && Networks != null)
        {
            foreach (var network in Networks)
            {
                SelectedNetworkIds.Add(network.ID);
            }
        }
        
        NotifyStateChanged();
    }

    public void ToggleNetworkSelection(string networkId)
    {
        if (SelectedNetworkIds.Contains(networkId))
        {
            SelectedNetworkIds.Remove(networkId);
            SelectAll = false;
        }
        else
        {
            SelectedNetworkIds.Add(networkId);
            
            // Check if all networks are now selected
            if (Networks != null && SelectedNetworkIds.Count == Networks.Count)
            {
                SelectAll = true;
            }
        }
        
        NotifyStateChanged();
    }

    public async Task RemoveSelectedNetworksAsync()
    {
        if (SelectedNetworkIds.Count == 0) return;

        var tasks = SelectedNetworkIds
            .Select(id => dockerClient.Networks.DeleteNetworkAsync(id))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            // Ignore errors - some networks may fail to delete (system networks, etc.)
        }
        finally
        {
            SelectedNetworkIds.Clear();
            SelectAll = false;
            await RefreshNetworksAsync();
        }
    }

}

public class NetworkListItemViewModel
{
    private static readonly HashSet<string> SystemNetworks = new() { "bridge", "host", "none" };

    public string Name { get; set; } = string.Empty;
    public string ID { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public bool EnableIPv6 { get; set; }
    public IPAM IPAM { get; set; } = new IPAM();
    public bool Internal { get; set; }
    public bool Attachable { get; set; }
    public bool Ingress { get; set; }
    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public string ShortId => ID?.Length > 12 ? ID.Substring(0, 12) : ID ?? string.Empty;

    public bool IsSystemNetwork => SystemNetworks.Contains(Name);
}
