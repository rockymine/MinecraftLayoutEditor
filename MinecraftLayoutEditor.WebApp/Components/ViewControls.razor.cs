using Excubo.Generators.Blazor;
using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;
using System.Reflection.Metadata;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class ViewControls
{
    [Parameter]
    public SymmetryAxis? SymmetryAxis { get; set; }
    [Parameter]
    public EventCallback OnResetView { get; set; }
    [Parameter]
    public EventCallback OnFitLayout { get; set; }
    [Parameter]
    public EventCallback OnFitTeam { get; set; }

    private bool IsMirrorEnabled => SymmetryAxis != null;

    private Task ResetView() => OnResetView.InvokeAsync();
    private Task FitLayout() => OnFitLayout.InvokeAsync();
    private Task FitTeam() => OnFitTeam.InvokeAsync();
}