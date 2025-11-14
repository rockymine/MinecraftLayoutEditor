using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class EditingModeSwitch
{
    [Parameter]
    public EditorMode EditorMode { get; set; }
    [Parameter]
    public EventCallback<EditorMode> OnChangeMode { get; set; }
    private async Task ChangeMode(EditorMode newMode)
    {
        if (EditorMode != newMode)
            await OnChangeMode.InvokeAsync(newMode);
    }
}