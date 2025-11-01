using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class NodeInformation
{
    [Parameter]
    public Node? SelectedNode { get; set; }
    [Parameter] 
    public EventCallback OnDeleteNode { get; set; }

    private Task DeleteNode() => OnDeleteNode.InvokeAsync();
}