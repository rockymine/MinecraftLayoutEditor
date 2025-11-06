using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class LayoutSettings
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }

    [Parameter]
    public string Name { get; set; }
    [Parameter]
    public EventCallback<string> NameChanged { get; set; }

    [Parameter]
    public int Width { get; set; }
    [Parameter]
    public EventCallback<int> WidthChanged { get; set; }

    [Parameter]
    public int Height { get; set; }
    [Parameter]
    public EventCallback<int> HeightChanged { get; set; }

    [Parameter]
    public int Thickness { get; set; }
    [Parameter]
    public EventCallback<int> ThicknessChanged { get; set; }

    [Parameter]
    public int LaneWidth { get; set; }
    [Parameter]
    public EventCallback<int> LaneWidthChanged { get; set; }

    [Parameter]
    public Node.NodeType SelectedNodeType { get; set; }
    [Parameter]
    public EventCallback<Node.NodeType> SelectedNodeTypeChanged { get; set; }

    public async Task OnNameChanged()
    {
        await NameChanged.InvokeAsync(Name); 
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnLaneWidthChanged()
    {
        await LaneWidthChanged.InvokeAsync(LaneWidth);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnSelectedNodeTypeChanged()
    {
        await SelectedNodeTypeChanged.InvokeAsync(SelectedNodeType);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnThicknessChanged()
    {
        await ThicknessChanged.InvokeAsync(Thickness);
        await SettingsChanged.InvokeAsync();
    }

    private async Task AdjustWidth(int delta)
    {
        var newWidth = Math.Max(16, Width + delta);
        newWidth = (newWidth / 16) * 16; // Ensure multiple of 16
        if (newWidth != Width)
        {
            Width = newWidth;
            await WidthChanged.InvokeAsync(Width);
            await SettingsChanged.InvokeAsync();
        }
    }

    private async Task AdjustHeight(int delta)
    {
        var newHeight = Math.Max(16, Height + delta);
        newHeight = (newHeight / 16) * 16; // Ensure multiple of 16
        if (newHeight != Height)
        {
            Height = newHeight;
            await HeightChanged.InvokeAsync(Height);
            await SettingsChanged.InvokeAsync();
        }
    }
}