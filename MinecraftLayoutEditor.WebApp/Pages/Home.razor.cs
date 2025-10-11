using BlazorDownloadFile;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Schematics;
using MinecraftLayoutEditor.WebApp.Extensions;
using MinecraftLayoutEditor.WebApp.Rendering;
using System.Numerics;
using System.Threading.Tasks;
using static MinecraftLayoutEditor.Logic.Layout;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home
{
    private Canvas Canvas;
    private readonly Logic.Layout _layout = LayoutFactory.Empty(30, 20);
    private readonly LayoutRenderer _renderer = new();
    private readonly LayoutRenderOptions _options = new();
    private Node? HoveredNode;
    private Node? SelectedNode;

    [Inject] public required IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) 
            return;

        await Render();
    }

    private async Task OnSchematicCreate()
    {
        var schematic = SchematicMaker.FromLayout(_layout);
        var fileName = $"{schematic.Name}.schematic";

        await BlazorDownloadFileService.DownloadFile(fileName, schematic.Save(),
            "application/octet-stream");
    }

    private async Task OnMouseUp(MouseEventArgs e)
    {
        Vector2 clickedAt = _renderer.ScreenToWorldPos(new Vector2((float)e.OffsetX, 
            (float)e.OffsetY));

        if (e.Button == 0 && HoveredNode == null && _layout.Contains(clickedAt))
        {
            _layout.AddNode(clickedAt);
            await Render();
        }
        else if (e.Button == 0 && HoveredNode != null)
        {
            if (SelectedNode == null)
            {
                SelectedNode = HoveredNode;
            } 
            else
            {
                _layout.Graph.AddOrRemoveEdge(HoveredNode, SelectedNode);
                SelectedNode = null;
            }

            await Render();
        }
        else if (e.Button == 2 && HoveredNode != null)
        {
            _layout.Graph.DeleteNode(HoveredNode);
            HoveredNode = null;
            await Render();
        }            
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        Vector2 cursorPosition = _renderer.ScreenToWorldPos(new Vector2((float)e.OffsetX, (float)e.OffsetY));
        Node? closestNode = _layout.Graph.GetClosestNode(cursorPosition);

        if (closestNode == null)
            return;

        var threshhold = 0.4f;
        var distanceToClosestNode = Vector2.Distance(cursorPosition, closestNode.Position);

        var prevHovered = HoveredNode;
        HoveredNode = distanceToClosestNode <= threshhold ? closestNode : null;

        if (prevHovered != HoveredNode)
            await Render();
    }

    private async Task Render()
    {
        await using (var ctx = await Canvas.GetContext2DAsync())
        {
            await _renderer.RenderAsync(ctx, _layout, _options, HoveredNode, SelectedNode);
        }
    }
}