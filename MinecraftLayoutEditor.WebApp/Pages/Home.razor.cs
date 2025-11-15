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
using MinecraftLayoutEditor.XML;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home : ComponentBase
{
    private EditorMode _currentMode = EditorMode.Layout;
    private SKGLView Canvas;
    private readonly Map _map = MapFactory.Empty(192,96,10,10);
    private MapRenderer? _renderer;
    private readonly RenderingOptions _renderingOptions = new();
    private MapElement? _uploadedMap;
    private ElementReference _canvasContainer;
    private float CanvasWidth => _renderer?.CanvasWidth ?? throw new UnreachableException();
    private float CanvasHeight => _renderer?.CanvasHeight ?? throw new UnreachableException();

    private float MaxZoom => CalculateMaxZoom();
    private float MinZoom => CalculateMinZoom();

    private string CursorClass => (PanStartPosition != null) ? "grab" : "default";
    private HistoryStack? _historyStack;
    private Vector2? PanStartPosition;

    [Inject]
    public required IBlazorDownloadFileService BlazorDownloadFileService { get; init; }
    [Inject]
    public required IJSRuntime JSRuntime { get; init; }


    private RenderContext? _renderContext;
    private readonly PaintCache _paintCache = new();

    private void OnPaintSurface(SKPaintGLSurfaceEventArgs args)
    {
        args.Surface.Canvas.Clear(SKColors.LightGray);

        if (_renderContext == null || _renderer == null)
            return;

        _renderContext.Update(args.Surface);
        _renderer.Render();

        //_renderer.Render(args.Surface, _map, HoveredNode, SelectedNode, _renderingOptions, _uploadedMap);
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
        await Render(RenderTrigger.Initial);
    }

    private async Task ResizeCanvas()
    {
        var size = await JSRuntime.InvokeAsync<Size>("getElementClientSize", _canvasContainer);
        _renderer.Resize(size.Width, size.Height);
        // keep the same world-center after resize
        var center = new Vector2(_renderer.CanvasWidth / 2f, _renderer.CanvasHeight / 2f);

        _renderer.UpdateTRS(center);
    }

    protected override void OnInitialized()
    {
        _historyStack = new HistoryStack();

        _renderContext = new RenderContext(
            _map, _renderingOptions, 
            _uploadedMap, _paintCache);

        _renderer = new MapRenderer(_renderContext);

        _renderer.renderables.Add(new BackgroundRenderer());
        _renderer.renderables.Add(new GridRenderer());
    }

    private async Task OnSettingsChanged()
    {
        await Render(RenderTrigger.SettingsChanged);
    }

    private void OnChangeEditingMode(EditorMode newMode)
    {
        _currentMode = newMode;
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

    private async Task OnSchematicCreate()
    {
        var schematic = SchematicMaker.FromMap(_map);
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
        await _canvasContainer.FocusAsync();
        
        if (e.Button == 1)
        {
            PanStartPosition = new Vector2((float)e.OffsetX, (float)e.OffsetY);
        }
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

    private async Task OnMouseMove(MouseEventArgs e)
    {
        Vector2 cursorPosition = _renderer.ScreenToWorldPos(new Vector2((float)e.OffsetX, (float)e.OffsetY));
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
        if (PanStartPosition == null)
            return;

        var panEndPosition = new Vector2(mouseX, mouseY);
        var deltaPan = PanStartPosition.Value - panEndPosition;
        PanStartPosition = panEndPosition;

        _renderer.UpdateTRS(_renderer.CameraPosition - deltaPan);
        await Render(RenderTrigger.Pan);
    }

    private async Task Render(RenderTrigger trigger)
    {
        Canvas.Invalidate();
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
        if (deltaY == 0)
            return;

        var relativeCursorPos = new Vector2((float)offsetX, (float)offsetY);
        var worldPosBeforeZoom = _renderer.ScreenToWorldPos(relativeCursorPos);

        float newScale;
        if (deltaY < 0)
        {
            newScale = _renderContext.Scale * 1.6f;
        }
        else
        {
            newScale = _renderContext.Scale / 1.6f;
        }

        newScale = float.Clamp(newScale, MinZoom, MaxZoom);

        if (Math.Abs(newScale - _renderContext.Scale) < 0.001f)
            return;

        // Calculate new translation to keep cursor world pos at cursor screen pos
        var newTranslation = relativeCursorPos - worldPosBeforeZoom * newScale;

        _renderContext.Scale = newScale;
        _renderer.UpdateTRS(newTranslation);

        await Render(RenderTrigger.Zoom);
    }

    public async Task OnFitMap()
    {
        if (_map.Width <= 0 || _map.Height <= 0)
            return;

        var canvasCenter = new Vector2(_renderer.CanvasWidth / 2f, _renderer.CanvasHeight / 2f);
        var newTranslation = canvasCenter;

        _renderContext.Scale = MinZoom;
        _renderer.UpdateTRS(newTranslation);
        await Render(RenderTrigger.ViewFit);
    }

    public async Task OnFitTeam()
    {
        if (_map.Width <= 0 || _map.Height <= 0 || _map.Symmetry == null)
            return;

        var horizontal = _map.Symmetry.IsHorizontal;

        float halfW = _map.Width / 2f;
        float halfH = _map.Height / 2f;

        Vector2 mapHalfCenter;
        float scaleX, scaleY;

        // Horizontal mirror line
        if (horizontal)
        {
            mapHalfCenter = new Vector2(0, -halfH / 2f);
            scaleX = _renderer.CanvasWidth / _map.Width;
            scaleY = _renderer.CanvasHeight / halfH;
        }
        // Vertical mirror line
        else
        {
            mapHalfCenter = new Vector2(halfW / 2f, 0);
            scaleX = _renderer.CanvasWidth / halfW;
            scaleY = _renderer.CanvasHeight / _map.Height;
        }

        var newScale = float.Min(scaleX, scaleY) * 0.98f;
        
        var canvasCenter = new Vector2(_renderer.CanvasWidth / 2f, _renderer.CanvasHeight / 2f);
        var newTranslation = canvasCenter - mapHalfCenter * newScale;

        _renderContext.Scale = newScale;
        _renderer.UpdateTRS(newTranslation);
        await Render(RenderTrigger.ViewFit);
    }

    private float CalculateMaxZoom()
    {
        if (_map.Width <= 0 || _map.Height <= 0)
            return 1f;

        float scaleX = _renderer.CanvasWidth / 16f;
        float scaleY = _renderer.CanvasHeight / 16f;

        return float.Min(scaleX, scaleY) * 0.98f;
    }

    private float CalculateMinZoom()
    {
        if (_map.Width <= 0 || _map.Height <= 0)
            return 1f;

        float scaleX = _renderer.CanvasWidth / _map.Width;
        float scaleY = _renderer.CanvasHeight / _map.Height;

        return float.Min(scaleX, scaleY) * 0.98f;
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
                _uploadedMap = map;
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
