using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.XML;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class RectangleRegionForm
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }
    [Parameter]
    public RectangleRegion RectangleRegion { get; set; }
    [Parameter]
    public EventCallback<RectangleRegion> RectangleRegionChanged { get; set; }
    [Parameter]
    public EventCallback<Region> OnRegionFocused { get; set; }
    [Parameter]
    public EventCallback OnDelete { get; set; }

    private async Task UpdateMinX(RectangleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        float newX = parsedValue;
        region.Min = new Vector2(newX, region.Min.Y);

        await RectangleRegionChanged.InvokeAsync(RectangleRegion);
        await SettingsChanged.InvokeAsync();
    }

    private async Task UpdateMinY(RectangleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        float newY = parsedValue;
        region.Min = new Vector2(region.Min.X, newY);

        await RectangleRegionChanged.InvokeAsync(RectangleRegion);
        await SettingsChanged.InvokeAsync();
    }

    private async Task UpdateMaxX(RectangleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        float newX = parsedValue;
        region.Max = new Vector2(newX, region.Max.Y);

        await RectangleRegionChanged.InvokeAsync(RectangleRegion);
        await SettingsChanged.InvokeAsync();
    }

    private async Task UpdateMaxY(RectangleRegion region, ChangeEventArgs e)
    {
        string? rawValue = e.Value?.ToString();

        if (!float.TryParse(rawValue, out float parsedValue))
            return;

        float newY = parsedValue;
        region.Max = new Vector2(region.Max.X, newY);

        await RectangleRegionChanged.InvokeAsync(RectangleRegion);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnNameChanged()
    {
        await RectangleRegionChanged.InvokeAsync(RectangleRegion);
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
            await OnRegionFocused.InvokeAsync(RectangleRegion);
    }
}