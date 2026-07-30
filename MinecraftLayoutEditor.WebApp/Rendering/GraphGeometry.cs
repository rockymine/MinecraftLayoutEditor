using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

/// <summary>
/// Cached draw batches for the layout graph.
///
/// Every SkiaSharp call made from WebAssembly crosses into the native Skia library,
/// and that crossing costs far more than the drawing behind it. A frame that issues
/// one DrawLine per edge and two shapes per node therefore scales with the number of
/// native calls rather than with the pixels covered.
///
/// Edges and nodes are collected into one path per type, so a frame issues a fixed
/// handful of calls whatever the size of the graph. The paths depend only on the
/// graph, so they are rebuilt when <see cref="Graph.Revision"/> changes and reused
/// otherwise - hovering, panning and zooming leave them alone.
/// </summary>
public class GraphGeometry : IDisposable
{
    private readonly Dictionary<Edge.EdgeType, SKPath> _edgePaths = [];
    private readonly Dictionary<Node.NodeType, SKPath> _nodePaths = [];
    private int _builtAtRevision = -1;

    private SKPath? _laneOutlinePath;
    private int _laneOutlineRevision = -1;
    private int _laneOutlineWidth = -1;

    public void DrawEdges(RenderContext context)
    {
        EnsureBuilt(context);

        foreach (var (edgeType, path) in _edgePaths)
        {
            var style = context.Options.GetEdgeStyle(edgeType);
            var paint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke,
                style.LineWidth, context.Viewport.Scale, style.LineDash);

            context.Surface!.Canvas.DrawPath(path, paint);
        }
    }

    public void DrawNodes(RenderContext context)
    {
        EnsureBuilt(context);

        foreach (var (nodeType, path) in _nodePaths)
        {
            var style = context.Options.GetNodeStyle(nodeType);
            DrawFilledAndStroked(context, path, style);
        }

        // The hovered and selected nodes carry a different stroke colour. Rebuilding a
        // batch whenever the pointer moves would defeat the caching, so they are simply
        // drawn again on top of the batch that already contains them. Antialiasing is
        // off and the geometry is identical, so the redraw covers what is underneath.
        DrawHighlight(context, context.SelectedNode, context.Options.SelectedNodeStroke);
        DrawHighlight(context, context.HoveredNode, context.Options.HoveredNodeStroke);
    }

    /// <summary>
    /// The outline of every edge's lane. Keyed on the lane width as well as the graph,
    /// because the width is a map setting the graph revision knows nothing about.
    /// </summary>
    public void DrawLaneOutlines(RenderContext context)
    {
        var graph = context.Map.Graph;
        var laneWidth = context.Map.LaneWidth;

        if (_laneOutlinePath == null
            || _laneOutlineRevision != graph.Revision
            || _laneOutlineWidth != laneWidth)
        {
            _laneOutlinePath?.Dispose();
            _laneOutlinePath = new SKPath();

            foreach (var edge in graph.Edges)
            {
                var corners = Rectangle.FindRectCorners(
                    edge.Node1.Position, edge.Node2.Position, laneWidth);

                _laneOutlinePath.MoveTo(corners.NearLeft.X, corners.NearLeft.Y);
                _laneOutlinePath.LineTo(corners.FarLeft.X, corners.FarLeft.Y);
                _laneOutlinePath.LineTo(corners.FarRight.X, corners.FarRight.Y);
                _laneOutlinePath.LineTo(corners.NearRight.X, corners.NearRight.Y);
                _laneOutlinePath.Close();
            }

            _laneOutlineRevision = graph.Revision;
            _laneOutlineWidth = laneWidth;
        }

        var paint = context.Cache.GetPaint(context.Options.BoundingBoxLineStroke,
            SKPaintStyle.Stroke, 1f, context.Viewport.Scale);

        context.Surface!.Canvas.DrawPath(_laneOutlinePath, paint);
    }

    private static void DrawHighlight(RenderContext context, Node? node, SKColor strokeColor)
    {
        if (node == null)
            return;

        var style = context.Options.GetNodeStyle(node.Type) with { StrokeStyle = strokeColor };

        using var path = new SKPath();
        AddNodeShape(path, node.Position, style);
        DrawFilledAndStroked(context, path, style);
    }

    private static void DrawFilledAndStroked(RenderContext context, SKPath path, RenderStyle style)
    {
        var fillPaint = context.Cache.GetPaint(style.FillStyle, SKPaintStyle.Fill,
            style.LineWidth, context.Viewport.Scale);
        var strokePaint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke,
            style.LineWidth, context.Viewport.Scale);

        context.Surface!.Canvas.DrawPath(path, fillPaint);
        context.Surface!.Canvas.DrawPath(path, strokePaint);
    }

    private void EnsureBuilt(RenderContext context)
    {
        var graph = context.Map.Graph;
        if (graph.Revision == _builtAtRevision)
            return;

        DisposePaths();

        foreach (var node in graph.Nodes)
        {
            if (!_nodePaths.TryGetValue(node.Type, out var path))
            {
                path = new SKPath();
                _nodePaths.Add(node.Type, path);
            }

            AddNodeShape(path, node.Position, context.Options.GetNodeStyle(node.Type));
        }

        foreach (var edge in graph.Edges)
        {
            if (!_edgePaths.TryGetValue(edge.Type, out var path))
            {
                path = new SKPath();
                _edgePaths.Add(edge.Type, path);
            }

            path.MoveTo(edge.Node1.Position.X, edge.Node1.Position.Y);
            path.LineTo(edge.Node2.Position.X, edge.Node2.Position.Y);
        }

        _builtAtRevision = graph.Revision;
    }

    private static void AddNodeShape(SKPath path, Vector2 position, RenderStyle style)
    {
        switch (style.Shape)
        {
            case NodeShape.Square:
                {
                    var halfSize = style.Radius * MathF.Sqrt(MathF.PI / 4);
                    path.AddRect(SKRect.Create(
                        position.X - halfSize, position.Y - halfSize,
                        halfSize * 2, halfSize * 2));
                    break;
                }

            case NodeShape.Diamond:
                {
                    var reach = style.Radius * MathF.Sqrt(MathF.PI / 2);
                    path.MoveTo(position.X - reach, position.Y);
                    path.LineTo(position.X, position.Y - reach);
                    path.LineTo(position.X + reach, position.Y);
                    path.LineTo(position.X, position.Y + reach);
                    path.Close();
                    break;
                }

            default:
                path.AddCircle(position.X, position.Y, style.Radius);
                break;
        }
    }

    private void DisposePaths()
    {
        _laneOutlinePath?.Dispose();
        _laneOutlinePath = null;
        _laneOutlineRevision = -1;

        foreach (var path in _edgePaths.Values)
            path.Dispose();

        foreach (var path in _nodePaths.Values)
            path.Dispose();

        _edgePaths.Clear();
        _nodePaths.Clear();
    }

    public void Dispose()
    {
        DisposePaths();
        GC.SuppressFinalize(this);
    }
}
