using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class LayoutSettings
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }

    [Parameter]
    public int Width { get; set; }
    [Parameter]
    public EventCallback<int> WidthChanged { get; set; }

    [Parameter]
    public int Height { get; set; }
    [Parameter]
    public EventCallback<int> HeightChanged { get; set; }

    [Parameter]
    public bool MirrorEnabled { get; set; }
    [Parameter]
    public EventCallback<bool> MirrorEnabledChanged { get; set; }

    [Parameter]
    public float RotationDeg { get; set; }
    [Parameter]
    public EventCallback<float> RotationDegChanged { get; set; }

    [Parameter]
    public bool IsHorizontal { get; set; }
    [Parameter] public EventCallback<bool> IsHorizontalChanged { get; set; }

    [Parameter]
    public bool ShowBlocksEnabled { get; set; }
    [Parameter]
    public EventCallback<bool> ShowBlocksEnabledChanged { get; set; }

    [Parameter]
    public bool ShowBoundingBoxEnabled { get; set; }
    [Parameter]
    public EventCallback<bool> ShowBoundingBoxEnabledChanged { get; set; }

    [Parameter]
    public Node.NodeType SelectedNodeType { get; set; }
    [Parameter]
    public EventCallback<Node.NodeType> SelectedNodeTypeChanged { get; set; }

    public async Task OnMirrorEnabledChanged()
    {
        await MirrorEnabledChanged.InvokeAsync(MirrorEnabled);
        await SettingsChanged.InvokeAsync();
    }

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

    public async Task OnRotationDegChanged()
    {
        await RotationDegChanged.InvokeAsync(RotationDeg);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnIsHorizontalChanged()
    {
        await IsHorizontalChanged.InvokeAsync(IsHorizontal);
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