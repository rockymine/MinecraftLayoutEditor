using BlazorDownloadFile;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Schematics;
using MinecraftLayoutEditor.WebApp.Rendering;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home
{
    private Canvas Canvas;
    private readonly Logic.Layout _layout = LayoutFactory.Empty(40, 40);
    private readonly LayoutRenderer _renderer = new();
    private readonly RenderingOptions _renderingOptions = new();
    private Node? HoveredNode;
    private Node? SelectedNode;

    [Inject] public required IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) 
            return;

        await Render();
    }

    private async Task OnSettingsChanged()
    {
        await Render(); 
    }

    private async Task OnClearLayout()
    {
        SelectedNode = null;
        HoveredNode = null;

        _layout.Graph.Nodes.Clear();

        await Render();
    }

    private async Task OnSchematicCreate()
    {
        var schematic = SchematicMaker.FromLayout(_layout, height: 5, scale: 1);
        var fileName = $"{schematic.Name}.schematic";

        await BlazorDownloadFileService.DownloadFile(fileName, schematic.Save(),
            "application/octet-stream");
    }

    private async Task OnMouseUp(MouseEventArgs e)
    {
        Vector2 clickedAt = _renderer.ScreenToWorldPos(new Vector2((float)e.OffsetX, 
            (float)e.OffsetY));

        // Add nodes when the user left clicks the canvas and no node is selected
        if (e.Button == 0 && HoveredNode == null && _layout.Contains(clickedAt))
        {
            _layout.AddNode(clickedAt);
            await Render();
        }
        // Select nodes with right click
        else if (e.Button == 0 && HoveredNode != null)
        {
            if (SelectedNode == null)
            {
                SelectedNode = HoveredNode;
            } 
            else
            {
                // Add or delete edge when second node is clicked
                _layout.Graph.AddOrRemoveEdge(HoveredNode, SelectedNode);
                SelectedNode = null;
            }

            await Render();
        }
        // Delete node when the user right clicks on it
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
            await _renderer.RenderAsync(ctx, _layout, HoveredNode, SelectedNode, _renderingOptions);
        }
    }
}