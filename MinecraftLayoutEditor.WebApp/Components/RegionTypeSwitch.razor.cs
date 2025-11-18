using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class RegionTypeSwitch
{
    [Parameter]
    public RegionType RegionType { get; set; }

    [Parameter]
    public EventCallback<RegionType> RegionTypeChanged { get; set; }

    private async Task OnSelectionChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<RegionType>(e.Value?.ToString(), out var newType))
        {
            if (RegionType != newType)
            {
                RegionType = newType;  // Update the local property
                await RegionTypeChanged.InvokeAsync(newType);  // Trigger the callback
            }
        }
    }
}