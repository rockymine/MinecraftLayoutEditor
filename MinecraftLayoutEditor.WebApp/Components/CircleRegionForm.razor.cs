using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.XML;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class CircleRegionForm
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }
    [Parameter]
    public CircleRegion CircleRegion { get; set; }
    [Parameter]
    public EventCallback<CircleRegion> CircleRegionChanged { get; set; }
    [Parameter]
    public EventCallback<Region> OnRegionFocused { get; set; }
    [Parameter]
    public EventCallback OnDelete { get; set; }

    private async Task UpdateCenterX(CircleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        float newX = parsedValue;
        region.Center = new Vector2(newX, region.Center.Y);

        await CircleRegionChanged.InvokeAsync(CircleRegion);
        await SettingsChanged.InvokeAsync();
    }

    private async Task UpdateCenterY(CircleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        float newY = parsedValue;
        region.Center = new Vector2(region.Center.X, newY);

        await CircleRegionChanged.InvokeAsync(CircleRegion);
        await SettingsChanged.InvokeAsync();
    }

    private async Task UpdateRadius(CircleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        region.Radius = (int)parsedValue;

        await CircleRegionChanged.InvokeAsync(CircleRegion);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnNameChanged()
    {
        await CircleRegionChanged.InvokeAsync(CircleRegion);
        await SettingsChanged.InvokeAsync();
    }

    private async Task DeleteRegion()
    {
        if (OnDelete.HasDelegate)
        {
            await OnDelete.InvokeAsync();
        }
    }

    private async Task HandleFocus()
    {
        if (OnRegionFocused.HasDelegate)
            await OnRegionFocused.InvokeAsync(CircleRegion);
    }
}