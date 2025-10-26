using BlazorDownloadFile;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.History;
using MinecraftLayoutEditor.Schematics;
using MinecraftLayoutEditor.WebApp.Rendering;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home : ComponentBase
{
    private Canvas? Canvas;
    private readonly Logic.Layout _layout = LayoutFactory.Empty(40, 40);
    private readonly LayoutRenderer _renderer = new();
    private readonly RenderingOptions _renderingOptions = new();
    private Node? HoveredNode;
    private Node? SelectedNode;

    private HistoryStack? _historyStack;
    private Vector2? PanStartPosition;

    [Inject] 
    public required IBlazorDownloadFileService BlazorDownloadFileService { get; init; }
    [Inject]
    public required IJSRuntime JSRuntime { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await JSRuntime.InvokeAsync<object>("init", DotNetObjectReference.Create(this));

        await Render();
    }

    protected override void OnInitialized()
    {
        _historyStack = new HistoryStack();
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
        _historyStack = new HistoryStack();

        await Render();
    }

    private async Task OnUndo()
    {
        _historyStack?.Undo();
        SelectedNode = null;
        await Render();
    }

    private async Task OnRedo()
    {
        _historyStack?.Redo();
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

        if (e.Button == 0)
        {
            await HandleLeftClick(clickedAt);
        }
        else if (e.Button == 1)
        {
            PanStartPosition = null;
        }
        else if (e.Button == 2)
        {
            await HandleRightClick(clickedAt);
        }
    }

    private async Task OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == 1)
            PanStartPosition = new Vector2((float)e.OffsetX, (float)e.OffsetY);
    }

    private async Task HandleLeftClick(Vector2 worldPos)
    {
        // Add node
        if (HoveredNode == null && _layout.Contains(worldPos))
        {
            var pos = new Vector2(float.Floor(worldPos.X) + 0.5f, float.Floor(worldPos.Y) + 0.5f);
            var closestNode = _layout.Graph.GetClosestNode(pos);

            // Check if a node already exists at the given position
            if (closestNode != null && Vector2.DistanceSquared(closestNode.Position, pos) < 1)
                return;

            var action = new AddNodeAction(
                _layout.Graph,
                worldPos,
                _layout.SelectedNodeType,
                _layout.Symmetry,
                _layout.MirrorEnabled
                );

            _historyStack?.ExecuteAction(action);
            await Render();
        }
        // Select node
        else if (HoveredNode != null)
        {
            if (SelectedNode == null)
            {
                SelectedNode = HoveredNode;
            }
            // Deselect node
            else if (SelectedNode == HoveredNode)
            {
                SelectedNode = null;
            }
            else
            {
                // Add or delete edge
                var action = new AddOrRemoveEdgeAction(
                    _layout.Graph,
                    HoveredNode,
                    SelectedNode
                    );

                _historyStack?.ExecuteAction(action);
                SelectedNode = null;
            }

            await Render();
        }
    }

    private async Task HandleRightClick(Vector2 worldPos)
    {
        Node? closestNode = _layout.Graph.GetClosestNode(worldPos);
        var threshhold = 2f;

        // Delete node
        if (HoveredNode != null)
        {
            var action = new RemoveNodeAction(
                _layout.Graph,
                HoveredNode
                );

            _historyStack?.ExecuteAction(action);

            if (SelectedNode == HoveredNode)
                SelectedNode = null;

            HoveredNode = null;
            await Render();
        }
        // Deselect node
        else if (SelectedNode != null && closestNode != null 
            && Vector2.Distance(worldPos, closestNode.Position) >= threshhold)
        {
            SelectedNode = null;
            await Render();
        }
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        Vector2 cursorPosition = _renderer.ScreenToWorldPos(new Vector2((float)e.OffsetX, (float)e.OffsetY));
        Node? closestNode = _layout.Graph.GetClosestNode(cursorPosition);

        var prevHovered = HoveredNode;

        if (closestNode != null)
        {
            var threshhold = 0.4f;
            var distanceToClosestNode = Vector2.Distance(cursorPosition, closestNode.Position);
            
            HoveredNode = distanceToClosestNode <= threshhold ? closestNode : null;
        }

        if (prevHovered != HoveredNode)
            await Render();
    }

    [JSInvokable]
    public async ValueTask JSOnMouseMove(int mouseX, int mouseY)
    {
        if (PanStartPosition == null)
            return;

        var panEndPosition = new Vector2(mouseX, mouseY);
        var deltaPan = (PanStartPosition.Value - panEndPosition) / _renderer.Scale;
        PanStartPosition = panEndPosition;

        _renderer.UpdateTRS(_renderer.CameraPosition - deltaPan, _renderer.Scale);
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

        bool nodeMoved = false;
        
        if (e.Key == "ArrowUp")
        {
            _layout.MoveNode(SelectedNode, new Vector2(0, -1));
            nodeMoved = true;
        }
        else if (e.Key == "ArrowDown")
        {
            _layout.MoveNode(SelectedNode, new Vector2(0, 1));
            nodeMoved = true;
        }
        else if (e.Key == "ArrowLeft")
        {
            _layout.MoveNode(SelectedNode, new Vector2(-1, 0));
            nodeMoved = true;
        }
        else if (e.Key == "ArrowRight")
        {
            _layout.MoveNode(SelectedNode, new Vector2(1, 0));
            nodeMoved = true;
        }

        if (nodeMoved)
            await Render();
    }

    public async Task OnWheel(WheelEventArgs e)
    {
        var scrollY = e.DeltaY;

        if (scrollY == 0)
            return;

        var relativeCursorPos = new Vector2((float)e.OffsetX, (float)e.OffsetY);
        var worldPosBeforeZoom = _renderer.ScreenToWorldPos(relativeCursorPos);

        if (scrollY < 0)
        {
            _renderer.UpdateTRS(_renderer.CameraPosition, _renderer.Scale * 1.6f);
        }
        else
        {
            _renderer.UpdateTRS(_renderer.CameraPosition, _renderer.Scale / 1.6f);
        }

        var worldPosAfterZoom = _renderer.ScreenToWorldPos(relativeCursorPos);
        var worldPosChange = worldPosAfterZoom - worldPosBeforeZoom;

        _renderer.UpdateTRS(_renderer.CameraPosition + worldPosChange, _renderer.Scale);

        await Render();
    }
}
