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
using System.Drawing;
using System.Numerics;
using MinecraftLayoutEditor.XML;

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
    private Vector2 _pendingFirstRegionPos = default!;

    private float CanvasWidth => _viewport.CanvasWidth;
    private float CanvasHeight => _viewport.CanvasHeight;
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

        Render(RenderTrigger.Initial);
    }

    [JSInvokable]
    public async Task OnBrowserResize()
    {
        await ResizeCanvas();

        if (_viewport.Scale == 1f)
            OnFitMap();

        Render(RenderTrigger.Initial);
    }

    private async Task ResizeCanvas()
    {
        var size = await JSRuntime.InvokeAsync<Size>("getElementClientSize", _canvasContainer);
        _viewport.Resize(size.Width, size.Height);
        _viewport.UpdateTRS(_viewport.Center);
    }

    private void Render(RenderTrigger trigger)
    {
        if (OperatingSystem.IsBrowser())
            _canvas.Invalidate();
    }

    private void OnPaintSurface(SKPaintGLSurfaceEventArgs args)
    {
        args.Surface.Canvas.Clear(SKColors.LightGray);

        if (_renderContext == null || _renderer == null)
            return;

        _renderContext.RegisterSurface(args.Surface);
        _renderer.Render();
    }

    public void OnFitMap()
    {
        if (_map.Width <= 0 || _map.Height <= 0)
            return;

        _viewport.FitToContent(_map.Width, _map.Height);
        Render(RenderTrigger.ViewFit);
    }

    public void OnFitTeam()
    {
        if (_map.Width <= 0 || _map.Height <= 0 || _map.Symmetry == null)
            return;

        _viewport.FitToSection(_map.Width, _map.Height, _map.Symmetry.IsHorizontal);
        Render(RenderTrigger.ViewFit);
    }

    private void OnChangeEditingMode(EditorMode newMode)
    {
        _currentMode = newMode;
    }

    private void OnChangeRegionType(RegionType regionType)
    {
        _renderContext.SelectedRegionType = regionType;
    }

    private void OnRegionFocused(Region region)
    {
        _renderContext.HoveredRegion = region;
        Render(RenderTrigger.RegionHover);
    }

    private void OnSettingsChanged()
    {
        Render(RenderTrigger.SettingsChanged);
    }

    private void OnClearMap()
    {
        _renderContext.SelectedNode = null;
        _renderContext.HoveredNode = null;

        _map.Graph.Clear();
        _historyStack = new HistoryStack();

        Render(RenderTrigger.MapCleared);
    }

    private void OnUndo()
    {
        _historyStack?.Undo();
        _renderContext.SelectedNode = null;
        Render(RenderTrigger.Undo);
    }

    private void OnRedo()
    {
        _historyStack?.Redo();
        _renderContext.SelectedNode = null;
        Render(RenderTrigger.Redo);
    }

    private async Task OnSchematicCreate()
    {
        var schematic = SchematicMaker.FromMap(_map);
        var fileName = $"{schematic.Name}.schematic";

        await BlazorDownloadFileService.DownloadFile(fileName, schematic.Save(),
            "application/octet-stream");
    }

    private void OnDeleteNode()
    {
        if (_renderContext.SelectedNode == null)
            return;
        
        var action = new RemoveNodeAction(
                _map.Graph,
                 _renderContext.SelectedNode
                );

        _historyStack?.ExecuteAction(action);
        Render(RenderTrigger.NodeRemoved);
    }

    private async Task OnMouseDown(MouseEventArgs e)
    {
        await _canvasContainer.FocusAsync();

        if (e.Button == 1)
        {
            _panStartPosition = new Vector2((float)e.OffsetX, (float)e.OffsetY);
        }
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        Vector2 clickedAt = _viewport.ScreenToWorldPos(new Vector2((float)e.OffsetX,
            (float)e.OffsetY));

        if (e.Button == 0)
        {
            HandleLeftClick(clickedAt);
        }
        else if (e.Button == 1)
        {
            _panStartPosition = null;
        }
        else if (e.Button == 2)
        {
            HandleRightClick(clickedAt);
        }
    }

    private void OnMouseMove(MouseEventArgs e)
    {
        Vector2 cursorPosition = _viewport.ScreenToWorldPos(new Vector2((float)e.OffsetX, (float)e.OffsetY));

        if (_currentMode == EditorMode.Layout)
        {
            Node? closestNode = _map.Graph.GetClosestNode(cursorPosition);

            var prevHovered = _renderContext.HoveredNode;

            if (closestNode != null)
            {
                var threshhold = 0.4f;
                var distanceToClosestNode = Vector2.Distance(cursorPosition, closestNode.Position);

                _renderContext.HoveredNode = distanceToClosestNode <= threshhold ? closestNode : null;
            }

            if (prevHovered != _renderContext.HoveredNode)
                Render(RenderTrigger.NodeHover);

        }

        if (_currentMode == EditorMode.XML)
        {
            var previous = _renderContext.HoveredRegion;

            _renderContext.HoveredRegion = _renderContext.GetRegionContaining(cursorPosition);

            if (previous != _renderContext.HoveredRegion)
            {
                Render(RenderTrigger.RegionHover);
            }
        }
    }

    [JSInvokable]
    public void JSOnMouseMove(int mouseX, int mouseY)
    {
        if (_panStartPosition == null)
            return;

        var panEndPosition = new Vector2(mouseX, mouseY);
        var deltaPan = _panStartPosition.Value - panEndPosition;
        _panStartPosition = panEndPosition;

        _viewport.UpdateTRS(_viewport.CameraPosition - deltaPan);
        Render(RenderTrigger.Pan);
    }

    public void OnKeyUp(KeyboardEventArgs e)
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
            Render(RenderTrigger.NodeMoved);
    }

    [JSInvokable]
    public void JSOnWheel(double deltaY, double offsetX, double offsetY)
    {
        var cursorPos = new Vector2((float)offsetX, (float)offsetY);

        if (_viewport.TryZoom(deltaY, cursorPos, _map.Width, _map.Height))
            Render(RenderTrigger.Zoom);
    }

    private void HandleLeftClick(Vector2 worldPos)
    {
        // Add node
        if (_currentMode == EditorMode.Layout)
        {

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
                Render(RenderTrigger.NodeAdded);
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

                Render(RenderTrigger.EdgeRemoved);
            }
        }

        if (_currentMode == EditorMode.XML)
        {
            if (_pendingFirstRegionPos == default)
            {
                // First click — set center for both rectangle and circle
                _pendingFirstRegionPos = new Vector2(MathF.Floor(worldPos.X), MathF.Floor(worldPos.Y));

                // Only Rectangle needs second point, Circle also needs second point for radius
                // So do nothing here except store first point
            }
            else
            {
                // Second click
                var secondPos = new Vector2(MathF.Floor(worldPos.X), MathF.Floor(worldPos.Y));

                _renderContext.EnsureMapElement();

                switch (_renderContext.SelectedRegionType)
                {
                    case RegionType.Rectangle:
                        var actionRect = new AddRectangleRegionAction(
                            _renderContext.MapElement, _pendingFirstRegionPos, secondPos);
                        _historyStack?.ExecuteAction(actionRect);
                        break;

                    case RegionType.Circle:
                        var center = _pendingFirstRegionPos;
                        var radius = (int)Math.Floor(Vector2.Distance(center, secondPos));
                        var actionCircle = new AddCircleRegionAction(_renderContext.MapElement, center, radius);
                        _historyStack?.ExecuteAction(actionCircle);
                        break;
                }

                _pendingFirstRegionPos = default;
                Render(RenderTrigger.RegionAdded);
            }
        }
    }

    private void HandleRightClick(Vector2 worldPos)
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
            Render(RenderTrigger.NodeRemoved);
        }
        // Deselect node
        else if (_renderContext.SelectedNode != null && closestNode != null
            && Vector2.Distance(worldPos, closestNode.Position) >= threshhold)
        {
            _renderContext.SelectedNode = null;
            Render(RenderTrigger.NodeDeselected);
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
                OnClearMap();

                foreach (var b in blocks)
                {
                    var pos = new Vector2(b.X, b.Z);
                    _map.Blocks.Add(pos);
                }

                _map.Width = (blocks.Max(b => Math.Abs(b.X)) * 2) + 64;
                _map.Height = (blocks.Max(b => Math.Abs(b.Z)) * 2) + 64;

                OnFitMap();
                Render(RenderTrigger.WorldImport);
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
