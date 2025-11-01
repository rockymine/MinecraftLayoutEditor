using Microsoft.AspNetCore.Components;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class VisualizationControls
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }

    [Parameter]
    public bool ShowBlocksEnabled { get; set; }
    [Parameter]
    public EventCallback<bool> ShowBlocksEnabledChanged { get; set; }

    [Parameter]
    public bool ShowBoundingBoxEnabled { get; set; }
    [Parameter]
    public EventCallback<bool> ShowBoundingBoxEnabledChanged { get; set; }

    public async Task OnShowBlocksEnabledChanged()
    {
        await ShowBlocksEnabledChanged.InvokeAsync(ShowBlocksEnabled);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnShowBoundingBoxEnabledChanged()
    {
        await ShowBoundingBoxEnabledChanged.InvokeAsync(ShowBoundingBoxEnabled);
        await SettingsChanged.InvokeAsync();
    }
}