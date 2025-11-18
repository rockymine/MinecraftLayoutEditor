using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.XML;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class MapRegionsContainer
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }
    [Parameter]
    public RegionsElement RegionsElement { get; set; }

    [Parameter] 
    public EventCallback<RegionsElement> RegionsElementChanged { get; set; }
    [Parameter]
    public EventCallback<Region> RegionFocused { get; set; }

    private async Task OnRegionSettingsChanged()
    {
        await RegionsElementChanged.InvokeAsync(RegionsElement);
        await SettingsChanged.InvokeAsync();
    }

    private async Task DeleteRegionAt(int index)
    {
        if (RegionsElement == null)
            return;

        if (index < 0 || index >= RegionsElement.Items.Count)
            return;

        RegionsElement.Items.RemoveAt(index);
        await RegionsElementChanged.InvokeAsync(RegionsElement);
        await SettingsChanged.InvokeAsync();
    }

    private async Task SetHoveredRegion(Region region)
    {
        if (RegionFocused.HasDelegate)
            await RegionFocused.InvokeAsync(region);
    }
}