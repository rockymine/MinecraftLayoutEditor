using Microsoft.AspNetCore.Components;
using static MinecraftLayoutEditor.Logic.Layout;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class LayoutSettings
{
    [Parameter]
    public MirrorMode MirrorMode { get; set; }

    [Parameter]
    public EventCallback<MirrorMode> MirrorModeChanged { get; set; }

    private MirrorMode SelectedMirrorMode
    {
        get => MirrorMode;
        set
        {
            if (MirrorMode == value) return;
            MirrorMode = value;
            MirrorModeChanged.InvokeAsync(value);
        }
    }
}