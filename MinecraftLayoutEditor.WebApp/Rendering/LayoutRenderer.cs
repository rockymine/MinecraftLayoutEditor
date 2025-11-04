using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using SkiaSharp;
using System.Diagnostics;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class LayoutRenderer
{
    public float CanvasWidth { get; private set; } = 1000f;
    public float CanvasHeight { get; private set; } = 1000f;

    private readonly GridRenderer _gridRenderer = new();
    public float Scale { get; private set; } = 1f;
    public Vector2 CameraPosition { get; private set; }
    private Matrix4x4 WorldToScreen;
    private Matrix4x4 ScreenToWorld;
    private readonly Dictionary<(SKColor, SKPaintStyle, float), SKPaint> _paintCache = [];
    
    public LayoutRenderer()
    {
        UpdateTRS(new Vector2(25, 25), 20f);
    }

    public SKPaint GetPaint(SKColor color, SKPaintStyle style, float lineWidth)
    {
        if (!_paintCache.TryGetValue((color, style, lineWidth), out var paint))
        {
            paint = new SKPaint
            {
                Color = color,
                Style = style,
                StrokeWidth = lineWidth,
                IsAntialias = false
            };

            _paintCache.Add((color, style, lineWidth), paint);
        }

        return paint;
    }

    public void UpdateTRS(Vector2 translation, float scale)
    {
        WorldToScreen = Matrix4x4.CreateTranslation(translation.X, translation.Y, 0) * Matrix4x4.CreateScale(scale);
        Matrix4x4.Invert(WorldToScreen, out ScreenToWorld);
        Scale = scale;
        CameraPosition = translation;
    }

    public void Render(SKSurface surface, Logic.Layout layout,
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var uniqueEdges = GetUniqueEdges(layout.Graph.Nodes);

        var gridStopwatch = Stopwatch.StartNew();
        RenderGrid(surface, layout, options);
        gridStopwatch.Stop();
        Console.WriteLine($"Grid: {gridStopwatch.ElapsedMilliseconds}ms (dimension: {layout.Width} x {layout.Height})");

        var mirrorStopwatch = Stopwatch.StartNew();
        RenderMirrorAxis(surface, layout, options);
        mirrorStopwatch.Stop();
        Console.WriteLine($"Mirror: {mirrorStopwatch.ElapsedMilliseconds}ms");

        var edgesStopwatch = Stopwatch.StartNew();
        RenderEdges(surface, uniqueEdges, options, layout.LaneWidth);
        edgesStopwatch.Stop();
        Console.WriteLine($"Edges: {edgesStopwatch.ElapsedMilliseconds}ms (count: {uniqueEdges.Count}, bounding box: {options.ShowBoundingBoxEnabled}, bounding box: {options.ShowBlocksEnabled}, width: {layout.LaneWidth})");

        var nodesStopwatch = Stopwatch.StartNew();
        RenderNodes(surface, layout.Graph.Nodes, hoveredNode, selectedNode, options);
        nodesStopwatch.Stop();
        Console.WriteLine($"Nodes: {nodesStopwatch.ElapsedMilliseconds}ms (count: {layout.Graph.Nodes.Count})");

        totalStopwatch.Stop();
        Console.WriteLine($"Total Render: {totalStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("---");
    }

    private static HashSet<Edge> GetUniqueEdges(IReadOnlyList<Node> nodes)
    {
        var uniqueEdges = new HashSet<Edge>();
        foreach (var n in nodes)
        {
            foreach (var e in n.Edges)
            {
                uniqueEdges.Add(e);
            }
        }

        return uniqueEdges;
    }

    private void RenderGrid(SKSurface surface, Logic.Layout layout, RenderingOptions options)
    {
        // Render grid cells
        var gridLineStyle = GetGridLineStyle(options);
        _gridRenderer.Render(surface, options.GridSpacing, gridLineStyle.LineWidth, 
            gridLineStyle.StrokeStyle, layout, this);

        // Render grid box
        var gridBoxPaint = GetPaint(gridLineStyle.StrokeStyle, SKPaintStyle.Stroke, options.GridBorderLineWidth);
        var origin = WorldToScreenPos(new Vector2(-layout.Width / 2f, -layout.Height / 2f));
        surface.Canvas.DrawRect(origin.X, origin.Y, WorldToScreenScale(layout.Width), 
            WorldToScreenScale(layout.Height), gridBoxPaint);
    }

    private void RenderMirrorAxis(SKSurface surface, Logic.Layout layout, RenderingOptions options)
    {
        if (layout.Symmetry == null || !layout.MirrorEnabled)
            return;

        // Render mirror line
        var mirrorLineStyle = GetMirrorLineStyle(options);
        var mirrorLinePaint = GetPaint(mirrorLineStyle.StrokeStyle, SKPaintStyle.Stroke, mirrorLineStyle.LineWidth);
        var start = WorldToScreenPos(layout.Symmetry.GetStartPointWorld(layout));
        var end = WorldToScreenPos(layout.Symmetry.GetEndPointWorld(layout));
        surface.Canvas.DrawLine(start.X, start.Y, end.X, end.Y, mirrorLinePaint);

        // Render rotation point
        if (layout.Symmetry.RotationDeg == 180)
        {
            var mirrorPointStyle = GetMirrorPointStyle(options);
            var mirrorPointPaint = GetPaint(mirrorPointStyle.FillStyle, SKPaintStyle.Fill, mirrorPointStyle.LineWidth);
            var center = WorldToScreenPos(Vector2.Zero);
            var radius = WorldToScreenScale(mirrorPointStyle.Radius);
            surface.Canvas.DrawCircle(center.X, center.Y, radius, mirrorPointPaint);
        }
    }

    private void RenderNodes(SKSurface surface, IReadOnlyList<Node> nodes, 
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        foreach (var n in nodes)
        {
            var style = GetNodeStyle(n, hoveredNode, selectedNode, options);
            RenderNodeShape(surface, n.Position, style);
        }
    }

    private static RenderStyle GetNodeStyle(Node node, Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        var style = options.GetStyle(node.Type.ToString().ToLower());

        if (node == hoveredNode)
        {
            return style with { StrokeStyle = options.HoveredNodeStroke };
        }

        if (node == selectedNode)
        {
            return style with { StrokeStyle = options.SelectedNodeStroke };
        }

        return style;
    }

    private void RenderNodeShape(SKSurface surface, Vector2 position, RenderStyle style)
    {
        var screenPos = WorldToScreenPos(position);

        switch (style.Shape.ToLower())
        {
            case "circle":
                RenderCircleNode(surface, screenPos, style);
                break;
            case "square":
                RenderSquareNode(surface, screenPos, style);
                break;
            case "diamond":
                RenderDiamondNode(surface, screenPos, style);
                break;
            default:
                RenderCircleNode(surface, screenPos, style);
                break;
        }
    }

    private void RenderCircleNode(SKSurface surface, Vector2 screenPos, RenderStyle style)
    {
        var circleFillPaint = GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth);
        var circleStrokePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

        surface.Canvas.DrawCircle(screenPos.X, screenPos.Y, WorldToScreenScale(style.Radius), circleFillPaint);
        surface.Canvas.DrawCircle(screenPos.X, screenPos.Y, WorldToScreenScale(style.Radius), circleStrokePaint);
    }

    private void RenderSquareNode(SKSurface surface, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 4);
        var topLeft = screenPos - new Vector2(WorldToScreenScale(size), WorldToScreenScale(size));

        size = WorldToScreenScale(size * 2);

        var squareFillPaint = GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth);
        var squareStrokePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

        surface.Canvas.DrawRect(topLeft.X, topLeft.Y, size, size, squareFillPaint);
        surface.Canvas.DrawRect(topLeft.X, topLeft.Y, size, size, squareStrokePaint);
    }

    private void RenderDiamondNode(SKSurface surface, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 2);

        var left = new Vector2(screenPos.X - WorldToScreenScale(size), screenPos.Y);
        var top = new Vector2(screenPos.X, screenPos.Y - WorldToScreenScale(size));
        var right = new Vector2(screenPos.X + WorldToScreenScale(size), screenPos.Y);
        var bottom = new Vector2(screenPos.X, screenPos.Y + WorldToScreenScale(size));

        var diamondFillPaint = GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth);
        var diamondStrokePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

        SKPath diamond = new SKPath();
        diamond.MoveTo(left.X, left.Y);
        diamond.LineTo(top.X, top.Y);
        diamond.LineTo(right.X, right.Y);
        diamond.LineTo(bottom.X, bottom.Y);
        diamond.LineTo(left.X, left.Y);
        diamond.Close();

        surface.Canvas.DrawPath(diamond, diamondFillPaint);
        surface.Canvas.DrawPath(diamond, diamondStrokePaint);
    }

    private void RenderEdges(SKSurface surface, HashSet<Edge> edges, RenderingOptions options, float laneWidth)
    {
        if (edges.Count == 0)
            return;
        
        foreach (var e in edges)
        {
            // Render path bounding box preview
            if (options.ShowBoundingBoxEnabled)
                RenderEdgeBoundingBox(surface, e.Node1.Position, e.Node2.Position, options, laneWidth);

            // Render schematic preview
            if (options.ShowBlocksEnabled)
                RenderEdgeSchematicBlocks(surface, e, options);

            var style = options.GetStyle(e.Type.ToString().ToLower());
            var from = WorldToScreenPos(e.Node1.Position);
            var to = WorldToScreenPos(e.Node2.Position);
            var edgePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

            surface.Canvas.DrawLine(from, to, edgePaint);
        }
    }

    private void RenderEdgeSchematicBlocks(SKSurface surface, Edge edge,
        RenderingOptions options)
    {
        var edgeBlockPaint = GetPaint(options.CellFillStyle, SKPaintStyle.Fill, 1f);
        SKPath blockList = new()
        {
            FillType = SKPathFillType.Winding
        };

        foreach (var block in edge.EdgeBlocks)
        {
            var screenPos = WorldToScreenPos(block);
            var size = WorldToScreenScale(1);

            blockList.AddRect(SKRect.Create(screenPos.X, screenPos.Y, size, size));
        }

        blockList.Close();
        surface.Canvas.DrawPath(blockList, edgeBlockPaint);
    }

    private void RenderEdgeBoundingBox(SKSurface surface, Vector2 pos1, Vector2 pos2,
        RenderingOptions options, float laneWidth)
    {
        var corners = Rectangle.FindRectCorners(pos1, pos2, laneWidth);

        var corner0 = WorldToScreenPos(corners[0]);
        var corner1 = WorldToScreenPos(corners[1]);
        var corner2 = WorldToScreenPos(corners[2]);
        var corner3 = WorldToScreenPos(corners[3]);

        var boundingBoxPaint = GetPaint(options.BoundingBoxLineStroke, SKPaintStyle.Stroke, 1f);
        surface.Canvas.DrawLine(corner0.X, corner0.Y, corner2.X, corner2.Y, boundingBoxPaint);
        surface.Canvas.DrawLine(corner2.X, corner2.Y, corner3.X, corner3.Y, boundingBoxPaint);
        surface.Canvas.DrawLine(corner3.X, corner3.Y, corner1.X, corner1.Y, boundingBoxPaint);
        surface.Canvas.DrawLine(corner1.X, corner1.Y, corner0.X, corner0.Y, boundingBoxPaint);
    }

    public Vector2 WorldToScreenPos(Vector2 worldPos)
    {
        var v3 = Vector3.Transform(new Vector3(worldPos.X, worldPos.Y, 0), WorldToScreen);

        return new Vector2(v3.X, v3.Y);
    }

    public Vector2 ScreenToWorldPos(Vector2 screenPos)
    {
        var v3 = Vector3.Transform(new Vector3(screenPos.X, screenPos.Y, 0), ScreenToWorld);

        return new Vector2(v3.X, v3.Y);
    }

    public float WorldToScreenScale(float worldLength)
    {
        return worldLength * Scale;
    }

    private static RenderStyle GetMirrorLineStyle(RenderingOptions options)
    {
        return options.GetStyle("mirrorLineStyle");
    }

    private static RenderStyle GetMirrorPointStyle(RenderingOptions options)
    {
        return options.GetStyle("mirrorPointStyle");
    }

    private static RenderStyle GetGridLineStyle(RenderingOptions options)
    {
        return options.GetStyle("gridLineStyle");
    }
}
