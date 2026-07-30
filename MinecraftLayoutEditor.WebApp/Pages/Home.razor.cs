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

public partial class Home : ComponentBase, IDisposable
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
    private readonly RenderProfiler _profiler = new();
    private readonly GraphGeometry _graphGeometry = new();
    private readonly RenderingOptions _renderingOptions = new();
    private SKGLView _canvas = default!;

    private ElementReference _canvasContainer;

    private EditorMode _currentMode = EditorMode.Layout;
    private Vector2? _panStartPosition;

    private float CanvasWidth => _viewport.CanvasWidth;
    private float CanvasHeight => _viewport.CanvasHeight;
    private string CursorClass => (_panStartPosition != null) ? "grab" : "default";

    protected override void OnInitialized()
    {
        _historyStack = new HistoryStack();
        _viewport = new Viewport();

        _renderContext = new RenderContext(
            _map, _renderingOptions,
            _viewport, _paintCache, _profiler, _graphGeometry);

        _renderer = new MapRenderer(_renderContext);

        // Registration order is paint order: later renderables draw over earlier ones.
        _renderer.renderables.Add(new BackgroundRenderer());
        _renderer.renderables.Add(new GridRenderer());
        _renderer.renderables.Add(new MirrorAxisRenderer());
        _renderer.renderables.Add(new MapBlocksRenderer());
        _renderer.renderables.Add(new RegionRenderer());
        _renderer.renderables.Add(new EdgeBoundingBoxRenderer());
        _renderer.renderables.Add(new EdgeBlocksRenderer());

        // The graph is what the editor edits, so it goes on top of everything, and a
        // node goes on top of the edges meeting at it rather than under them.
        _renderer.renderables.Add(new EdgeRenderer());
        _renderer.renderables.Add(new NodeRenderer());
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _profiler.RecordComponentRender();

        if (!firstRender)
            return;

        await _canvasContainer.FocusAsync();
        await JSRuntime.InvokeAsync<object>("init", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("initResizeObserver", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("initRenderStats", DotNetObjectReference.Create(this));

        Render();
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _paintCache.Dispose();
        _graphGeometry.Dispose();
    }

    [JSInvokable]
    public string RenderProfileJson() => _profiler.ToJson();

    [JSInvokable]
    public string RenderProfileText() => _profiler.ToText();

    [JSInvokable]
    public void ResetRenderProfile() => _profiler.Reset();

    [JSInvokable]
    public int ImportedBlockCount() => _map.Blocks.Count;

    /// <summary>
    /// Fills the map with a grid of connected nodes so the node and edge renderers can
    /// be measured with a realistic graph on the canvas.
    /// </summary>
    [JSInvokable]
    public int LoadBenchmarkGraph(int columns, int rows)
    {
        _renderContext.SelectedNode = null;
        _renderContext.HoveredNode = null;
        _historyStack = new HistoryStack();

        MapFactory.FillWithGrid(_map, columns, rows);

        OnFitMap();
        StateHasChanged();

        return _map.Graph.Nodes.Count;
    }

    [JSInvokable]
    public async Task OnBrowserResize()
    {
        await ResizeCanvas();

        if (_viewport.Scale == 1f)
            OnFitMap();

        Render();
    }

    private async Task ResizeCanvas()
    {
        var size = await JSRuntime.InvokeAsync<Size>("getElementClientSize", _canvasContainer);
        _viewport.Resize(size.Width, size.Height);
        _viewport.UpdateTRS(_viewport.Center);
    }

    /// <summary>
    /// Asks for the canvas to be repainted. Repeated calls before the next animation
    /// frame collapse into a single paint, which SKGLView already handles, so callers
    /// do not need to coordinate.
    /// </summary>
    private void Render()
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
        Render();
    }

    public void OnFitTeam()
    {
        if (_map.Width <= 0 || _map.Height <= 0 || _map.Symmetry == null)
            return;

        _viewport.FitToSection(_map.Width, _map.Height, _map.Symmetry.IsHorizontal);
        Render();
    }

    private void OnChangeEditingMode(EditorMode newMode)
    {
        _currentMode = newMode;
    }

    private void OnSettingsChanged()
    {
        Render();
    }

    private void OnClearMap()
    {
        _renderContext.SelectedNode = null;
        _renderContext.HoveredNode = null;

        _map.Graph.Clear();
        _historyStack = new HistoryStack();

        Render();
    }

    private void OnUndo()
    {
        _historyStack?.Undo();
        _renderContext.SelectedNode = null;
        Render();
    }

    private void OnRedo()
    {
        _historyStack?.Redo();
        _renderContext.SelectedNode = null;
        Render();
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
        Render();
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

    /// <summary>
    /// Pointer movement is handled here rather than through a Blazor @onmousemove
    /// binding. A Blazor event handler re-renders the whole component when it returns,
    /// which re-diffs every sidebar control even though moving the pointer changes
    /// nothing the sidebar shows. A JSInvokable call does not, and panning and hovering
    /// only ever need the canvas repainted.
    /// </summary>
    [JSInvokable]
    public void JSOnMouseMove(int mouseX, int mouseY)
    {
        var screenPosition = new Vector2(mouseX, mouseY);
        var moved = false;

        if (_panStartPosition != null)
        {
            var deltaPan = _panStartPosition.Value - screenPosition;
            _panStartPosition = screenPosition;

            _viewport.UpdateTRS(_viewport.CameraPosition - deltaPan);
            moved = true;
        }

        var hoverStartedAt = Stopwatch.GetTimestamp();
        var hoverChanged = UpdateHoveredNode(_viewport.ScreenToWorldPos(screenPosition));
        _profiler.RecordHoverLookup(
            Stopwatch.GetElapsedTime(hoverStartedAt).TotalMilliseconds);

        if (hoverChanged || moved)
            Render();
    }

    /// <summary>Returns whether the hovered node changed.</summary>
    private bool UpdateHoveredNode(Vector2 cursorPosition)
    {
        const float hoverRadius = 0.4f;

        var previousHovered = _renderContext.HoveredNode;
        _renderContext.HoveredNode = _map.Graph.FindNodeWithin(cursorPosition, hoverRadius);

        return previousHovered != _renderContext.HoveredNode;
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
            Render();
    }

    [JSInvokable]
    public void JSOnWheel(double deltaY, double offsetX, double offsetY)
    {
        var cursorPos = new Vector2((float)offsetX, (float)offsetY);

        if (_viewport.TryZoom(deltaY, cursorPos, _map.Width, _map.Height))
            Render();
    }

    private void HandleLeftClick(Vector2 worldPos)
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
            Render();
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

            Render();
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
            Render();
        }
        // Deselect node
        else if (_renderContext.SelectedNode != null && closestNode != null
            && Vector2.Distance(worldPos, closestNode.Position) >= threshhold)
        {
            _renderContext.SelectedNode = null;
            Render();
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

                _map.SetBlocks(blocks.Select(b => new Vector2(b.X, b.Z)));

                _map.Width = (blocks.Max(b => Math.Abs(b.X)) * 2) + 64;
                _map.Height = (blocks.Max(b => Math.Abs(b.Z)) * 2) + 64;

                OnFitMap();
                Render();
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
