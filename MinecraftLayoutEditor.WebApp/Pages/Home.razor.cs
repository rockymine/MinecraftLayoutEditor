using BlazorDownloadFile;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.History;
using MinecraftLayoutEditor.Schematics;
using MinecraftLayoutEditor.WebApp.Rendering;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home : ComponentBase
{
    private Canvas Canvas;
    private readonly Logic.Layout _layout = LayoutFactory.Empty(40, 40);
    private readonly LayoutRenderer _renderer = new();
    private readonly RenderingOptions _renderingOptions = new();
    private Node? HoveredNode;
    private Node? SelectedNode;
    private HistoryStack? _historyStack;

    [Inject] public required IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) 
            return;

        await Render();
    }

    protected override void OnInitialized()
    {
        _historyStack = new HistoryStack(_layout.Graph);
    }
    
    private async Task OnSettingsChanged()
    {
        await Render(); 
    }

    private async Task OnClearLayout()
    {
        SelectedNode = null;
        HoveredNode = null;

        _layout.Graph.Clear();
        _historyStack = new HistoryStack(_layout.Graph);

        await Render();
    }

    private async Task OnUndo()
    {
        _historyStack?.Undo();
        SelectedNode = null;
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
            var node = _layout.AddNode(clickedAt);

            if (node != null)
                _historyStack?.Add(HistoryAction.AddNode(node));

            await Render();
        }
        // Select nodes with left click
        else if (e.Button == 0 && HoveredNode != null)
        {
            if (SelectedNode == null)
            {
                SelectedNode = HoveredNode;
            }
            // Deselect SelectedNode with left click
            else if (SelectedNode == HoveredNode)
            {
                SelectedNode = null;
            }
            else
            {
                // Add or delete edge when second node is clicked
                var edge = _layout.Graph.AddOrRemoveEdge(HoveredNode, SelectedNode);

                if (edge != null)
                    _historyStack?.Add(HistoryAction.AddOrRemoveEdge(edge.Node1, edge.Node2));

                SelectedNode = null;
            }

            await Render();
        }
        // Handle right click
        else if (e.Button == 2)
        {
            Vector2 cursorPosition = _renderer.ScreenToWorldPos(new Vector2((float)e.OffsetX, (float)e.OffsetY));
            Node? closestNode = _layout.Graph.GetClosestNode(cursorPosition);
            var threshhold = 2f;

            // Delete node when the user right clicks on it
            if (HoveredNode != null)
            {
                _layout.Graph.DeleteNode(HoveredNode);
                _historyStack?.Add(HistoryAction.RemoveNode(HoveredNode));

                if (SelectedNode == HoveredNode)
                    SelectedNode = null;

                HoveredNode = null;
                await Render();
            }
            // Unselect Node
            else if (SelectedNode != null && closestNode != null && Vector2.Distance(cursorPosition, closestNode.Position) >= threshhold)
            {
                SelectedNode = null;
                await Render();
            }
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

    public async Task OnKeyUp(KeyboardEventArgs e)
    {
        if (SelectedNode == null)
            return;
        
        if (e.Key == "ArrowUp")
        {
            _layout.MoveNode(SelectedNode, new Vector2(0, -1));
        }
        else if (e.Key == "ArrowDown")
        {
            _layout.MoveNode(SelectedNode, new Vector2(0, 1));
        }
        else if (e.Key == "ArrowLeft")
        {
            _layout.MoveNode(SelectedNode, new Vector2(-1, 0));
        }
        else if (e.Key == "ArrowRight")
        {
            _layout.MoveNode(SelectedNode, new Vector2(1, 0));
        }

        await Render();
    }
}