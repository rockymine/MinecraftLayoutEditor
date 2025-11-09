using Microsoft.AspNetCore.Components;
using System.Reflection.Metadata;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class ViewControls
{
    [Parameter]
    public bool MirrorEnabled { get; set; }
    [Parameter]
    public EventCallback OnFitMap { get; set; }
    [Parameter]
    public EventCallback OnFitTeam { get; set; }

    private Task FitMap() => OnFitMap.InvokeAsync();
    private Task FitTeam() => OnFitTeam.InvokeAsync();
}