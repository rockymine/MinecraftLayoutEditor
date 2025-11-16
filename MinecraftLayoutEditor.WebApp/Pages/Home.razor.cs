using BlazorDownloadFile;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.History;
using MinecraftLayoutEditor.Schematics;
using MinecraftLayoutEditor.WebApp.Rendering;
using MinecraftLayoutEditor.WebApp.Rendering.Renderers;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    public required IBlazorDownloadFileService BlazorDownloadFileService { get; init; }
    [Inject]
    public required IJSRuntime JSRuntime { get; init; }

    private readonly Map _map = MapFactory.Empty(192, 96, 10, 10);
    private HistoryStack _historyStack = default!;

    private Viewport _viewport = default!;
    private RenderContext _renderContext = default!;
    private MapRenderer _renderer = default!;
    private readonly PaintCache _paintCache = new();
    private readonly RenderingOptions _renderingOptions = new();
    private SKGLView _canvas = default!;

    private ElementReference _canvasContainer;

    private EditorMode _currentMode = EditorMode.Layout;
    private Vector2? _panStartPosition;

    private float CanvasWidth => _viewport?.CanvasWidth ?? throw new UnreachableException();
    private float CanvasHeight => _viewport?.CanvasHeight ?? throw new UnreachableException();
    private string CursorClass => (_panStartPosition != null) ? "grab" : "default";

    protected override void OnInitialized()
    {
        _historyStack = new HistoryStack();
        _viewport = new Viewport();

        _renderContext = new RenderContext(
            _map, _renderingOptions,
            _viewport, _paintCache);

        _renderer = new MapRenderer(_renderContext);

        _renderer.renderables.Add(new BackgroundRenderer());
        _renderer.renderables.Add(new GridRenderer());
        _renderer.renderables.Add(new MirrorAxisRenderer());
        _renderer.renderables.Add(new NodeRenderer());
        _renderer.renderables.Add(new EdgeRenderer());
        _renderer.renderables.Add(new MapBlocksRenderer());
        _renderer.renderables.Add(new RegionRenderer());
        _renderer.renderables.Add(new EdgeBoundingBoxRenderer());
        _renderer.renderables.Add(new EdgeBlocksRenderer());
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await _canvasContainer.FocusAsync();
        await JSRuntime.InvokeAsync<object>("init", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("initResizeObserver", DotNetObjectReference.Create(this));

        await Render(RenderTrigger.Initial);
    }

    [JSInvokable]
    public async Task OnBrowserResize()
    {
        await ResizeCanvas();

        if (_viewport.Scale == 1f)
            await OnFitMap();

        await Render(RenderTrigger.Initial);
    }

    private async Task ResizeCanvas()
    {
        var size = await JSRuntime.InvokeAsync<Size>("getElementClientSize", _canvasContainer);
        _viewport.Resize(size.Width, size.Height);
        _viewport.UpdateTRS(_viewport.Center);
    }

    private async Task Render(RenderTrigger trigger)
    {
       _canvas.Invalidate();
    }

    private void OnPaintSurface(SKPaintGLSurfaceEventArgs args)
    {
        args.Surface.Canvas.Clear(SKColors.Black);

        if (_renderContext == null || _renderer == null)
            return;

        _renderContext.RegisterSurface(args.Surface);
        _renderer.Render();
    }

    public async Task OnFitMap()
    {
        if (_map.Width <= 0 || _map.Height <= 0)
            return;

        _viewport.FitToContent(_map.Width, _map.Height);
        await Render(RenderTrigger.ViewFit);
    }

    public async Task OnFitTeam()
    {
        if (_map.Width <= 0 || _map.Height <= 0 || _map.Symmetry == null)
            return;

        _viewport.FitToSection(_map.Width, _map.Height, _map.Symmetry.IsHorizontal);
        await Render(RenderTrigger.ViewFit);
    }

    private void OnChangeEditingMode(EditorMode newMode)
    {
        _currentMode = newMode;
    }

    private async Task OnSettingsChanged()
    {
        await Render(RenderTrigger.SettingsChanged);
    }

    private async Task OnClearMap()
    {
        _renderContext.SelectedNode = null;
        _renderContext.HoveredNode = null;

        _map.Graph.Clear();
        _historyStack = new HistoryStack();

        await Render(RenderTrigger.MapCleared);
    }

    private async Task OnUndo()
    {
        _historyStack?.Undo();
        _renderContext.SelectedNode = null;
        await Render(RenderTrigger.Undo);
    }

    private async Task OnRedo()
    {
        _historyStack?.Redo();
        _renderContext.SelectedNode = null;
        await Render(RenderTrigger.Redo);
    }

    private async Task OnSchematicCreate()
    {
        var schematic = SchematicMaker.FromMap(_map);
        var fileName = $"{schematic.Name}.schematic";

        await BlazorDownloadFileService.DownloadFile(fileName, schematic.Save(),
            "application/octet-stream");
    }

    private async Task OnDeleteNode()
    {
        if (_renderContext.SelectedNode == null)
            return;
        
        var action = new RemoveNodeAction(
                _map.Graph,
                 _renderContext.SelectedNode
                );

        _historyStack?.ExecuteAction(action);
        await Render(RenderTrigger.NodeRemoved);
    }

    private async Task OnMouseDown(MouseEventArgs e)
    {
        await _canvasContainer.FocusAsync();

        if (e.Button == 1)
        {
            _panStartPosition = new Vector2((float)e.OffsetX, (float)e.OffsetY);
        }
    }

    private async Task OnMouseUp(MouseEventArgs e)
    {
        Vector2 clickedAt = _viewport.ScreenToWorldPos(new Vector2((float)e.OffsetX,
            (float)e.OffsetY));

        if (e.Button == 0)
        {
            await HandleLeftClick(clickedAt);
        }
        else if (e.Button == 1)
        {
            _panStartPosition = null;
        }
        else if (e.Button == 2)
        {
            await HandleRightClick(clickedAt);
        }
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        Vector2 cursorPosition = _viewport.ScreenToWorldPos(new Vector2((float)e.OffsetX, (float)e.OffsetY));
        Node? closestNode = _map.Graph.GetClosestNode(cursorPosition);

        var prevHovered = _renderContext.HoveredNode;

        if (closestNode != null)
        {
            var threshhold = 0.4f;
            var distanceToClosestNode = Vector2.Distance(cursorPosition, closestNode.Position);

            _renderContext.HoveredNode = distanceToClosestNode <= threshhold ? closestNode : null;
        }

        if (prevHovered != _renderContext.HoveredNode)
            await Render(RenderTrigger.NodeHover);
    }

    [JSInvokable]
    public async ValueTask JSOnMouseMove(int mouseX, int mouseY)
    {
        if (_panStartPosition == null)
            return;

        var panEndPosition = new Vector2(mouseX, mouseY);
        var deltaPan = _panStartPosition.Value - panEndPosition;
        _panStartPosition = panEndPosition;

        _viewport.UpdateTRS(_viewport.CameraPosition - deltaPan);
        await Render(RenderTrigger.Pan);
    }



    public async Task OnKeyUp(KeyboardEventArgs e)
    {
        if (_renderContext.SelectedNode == null)
            return;

        bool nodeMoved = false;

        if (e.Key == "ArrowUp")
        {
            _map.MoveNode(_renderContext.SelectedNode, new Vector2(0, -1));
            nodeMoved = true;
        }
        else if (e.Key == "ArrowDown")
        {
            _map.MoveNode(_renderContext.SelectedNode, new Vector2(0, 1));
            nodeMoved = true;
        }
        else if (e.Key == "ArrowLeft")
        {
            _map.MoveNode(_renderContext.SelectedNode, new Vector2(-1, 0));
            nodeMoved = true;
        }
        else if (e.Key == "ArrowRight")
        {
            _map.MoveNode(_renderContext.SelectedNode, new Vector2(1, 0));
            nodeMoved = true;
        }

        if (nodeMoved)
            await Render(RenderTrigger.NodeMoved);
    }

    [JSInvokable]
    public async ValueTask JSOnWheel(double deltaY, double offsetX, double offsetY)
    {
        var cursorPos = new Vector2((float)offsetX, (float)offsetY);

        if (_viewport.TryZoom(deltaY, cursorPos, _map.Width, _map.Height))
            await Render(RenderTrigger.Zoom);
    }

    private async Task HandleLeftClick(Vector2 worldPos)
    {
        // Add node
        if (_renderContext.HoveredNode == null && _map.Contains(worldPos))
        {
            var pos = new Vector2(float.Floor(worldPos.X) + 0.5f, float.Floor(worldPos.Y) + 0.5f);
            var closestNode = _map.Graph.GetClosestNode(pos);

            // Check if a node already exists at the given position
            if (closestNode != null && Vector2.DistanceSquared(closestNode.Position, pos) < 1)
                return;

            var action = new AddNodeAction(
                _map.Graph,
                worldPos,
                _map.SelectedNodeType,
                _map.Symmetry,
                _map.MirrorEnabled
                );

            _historyStack?.ExecuteAction(action);
            await Render(RenderTrigger.NodeAdded);
        }
        // Select node
        else if (_renderContext.HoveredNode != null)
        {
            if (_renderContext.SelectedNode == null)
            {
                _renderContext.SelectedNode = _renderContext.HoveredNode;
            }
            // Deselect node
            else if (_renderContext.SelectedNode == _renderContext.HoveredNode)
            {
                _renderContext.SelectedNode = null;
            }
            else
            {
                // Add or delete edge
                var action = new AddOrRemoveEdgeAction(
                    _map.Graph,
                     _renderContext.HoveredNode,
                     _renderContext.SelectedNode
                    );

                _historyStack?.ExecuteAction(action);
                _map.CalculateEdgeBlocks();
                _renderContext.SelectedNode = null;
            }

            await Render(RenderTrigger.EdgeRemoved);
        }
    }

    private async Task HandleRightClick(Vector2 worldPos)
    {
        Node? closestNode = _map.Graph.GetClosestNode(worldPos);
        var threshhold = 2f;

        // Delete node
        if (_renderContext.HoveredNode != null)
        {
            var action = new RemoveNodeAction(
                _map.Graph,
                 _renderContext.HoveredNode
                );

            _historyStack?.ExecuteAction(action);
            _map.CalculateEdgeBlocks();

            if (_renderContext.SelectedNode == _renderContext.HoveredNode)
                _renderContext.SelectedNode = null;

            _renderContext.HoveredNode = null;
            await Render(RenderTrigger.NodeRemoved);
        }
        // Deselect node
        else if (_renderContext.SelectedNode != null && closestNode != null
            && Vector2.Distance(worldPos, closestNode.Position) >= threshhold)
        {
            _renderContext.SelectedNode = null;
            await Render(RenderTrigger.NodeDeselected);
        }
    }

    private async Task LoadWorldFiles(InputFileChangeEventArgs e)
    {
        var files = e.GetMultipleFiles().ToArray();
        if (files.Length == 0) 
            return;

        try
        {
            var (blocks, spawn, worldName, map) = await WorldImporter.ImportWorld(files);
            _map.Name = worldName;

            if (blocks.Count > 0)
            {
                await OnClearMap();

                foreach (var b in blocks)
                {
                    var pos = new Vector2(b.X, b.Z);
                    _map.Blocks.Add(pos);
                }

                _map.Width = (blocks.Max(b => Math.Abs(b.X)) * 2) + 64;
                _map.Height = (blocks.Max(b => Math.Abs(b.Z)) * 2) + 64;

                await OnFitMap();
                await Render(RenderTrigger.WorldImport);
            }

            if (map != null)
            {
                _renderContext.RegisterMapElement(map);
                Console.WriteLine($"Map '{map.Name}' with {map.Regions.Items.Count} regions loaded.");
            }
            else
            {
                Console.WriteLine("No map.xml found.");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Import failed: {ex.Message}");
        }
    }
}
