using Microsoft.AspNetCore.Components;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class MirrorControls
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }

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

    public async Task OnMirrorEnabledChanged()
    {
        await MirrorEnabledChanged.InvokeAsync(MirrorEnabled);
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
}