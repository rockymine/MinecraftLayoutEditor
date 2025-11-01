using Microsoft.AspNetCore.Components;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class HistoryControls
{
    [Parameter] 
    public bool CanUndo { get; set; }
    [Parameter] 
    public bool CanRedo { get; set; }
    [Parameter] 
    public bool CanClear { get; set; }

    [Parameter] 
    public EventCallback OnClearLayout { get; set; }
    [Parameter] 
    public EventCallback OnUndo { get; set; }
    [Parameter] 
    public EventCallback OnRedo { get; set; }

    private Task Clear() => OnClearLayout.InvokeAsync();
    private Task Undo() => OnUndo.InvokeAsync();
    private Task Redo() => OnRedo.InvokeAsync();
}