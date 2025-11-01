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
    public Node.NodeType SelectedNodeType { get; set; }
    [Parameter]
    public EventCallback<Node.NodeType> SelectedNodeTypeChanged { get; set; }

    public async Task OnNameChanged()
    {
        await NameChanged.InvokeAsync(Name); 
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnWidthChanged()
    {
        await WidthChanged.InvokeAsync(Width);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnHeightChanged()
    {
        await HeightChanged.InvokeAsync(Height);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnSelectedNodeTypeChanged()
    {
        await SelectedNodeTypeChanged.InvokeAsync(SelectedNodeType);
        await SettingsChanged.InvokeAsync();
    }
}