using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class MapRenderer
{
    public float CanvasWidth { get; private set; }
    public float CanvasHeight { get; private set; }
    public Vector2 CameraPosition { get; private set; }
    public float Scale { get; private set; } = 1f;

    private SKMatrix SKWorldToScreen;
    private readonly Dictionary<(SKColor, SKPaintStyle, float), SKPaint> _paintCache = [];

    public MapRenderer()
    {
        UpdateTRS(new Vector2(25, 25), 20f);
    }

    public SKPaint GetPaint(SKColor color, SKPaintStyle style, float lineWidth)
    {
        var adjustedWidth = (style == SKPaintStyle.Stroke) ? lineWidth / Math.Max(Scale, 0.001f) : lineWidth;

        var key = (color, style, adjustedWidth);
        if (!_paintCache.TryGetValue(key, out var paint))
        {
            paint = new SKPaint
            {
                Color = color,
                Style = style,
                StrokeWidth = adjustedWidth,
                IsAntialias = false
            };
            _paintCache.Add(key, paint);
        }
        return paint;
    }

    public void Resize(float width, float height)
    {
        CanvasWidth = width;
        CanvasHeight = height;
    }

    public void UpdateTRS(Vector2 translation, float scale)
    {
        Scale = scale;
        CameraPosition = translation;

        SKWorldToScreen = SKMatrix.CreateScale(scale, scale);
        SKWorldToScreen.TransX = translation.X;
        SKWorldToScreen.TransY = translation.Y;
    }

    public void Render(SKSurface surface, Map map,
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        var canvas = surface.Canvas;
        canvas.Save();
        canvas.Concat(in SKWorldToScreen);

        var uniqueEdges = map.Graph.GetUniqueEdges();
        var limitX = (int)(map.Width / 2f);
        var limitY = (int)(map.Height / 2f);

        RenderBackground(surface, map);
        RenderGrid(surface, map, options);
        RenderBlocks(surface, map.Blocks, options, limitX, limitY);
        RenderMirrorAxis(surface, map, options);
        RenderEdges(surface, uniqueEdges, options, map.LaneWidth, limitX, limitY);
        RenderNodes(surface, map.Graph.Nodes, hoveredNode, selectedNode, options);

        canvas.Restore();
    }

    private void RenderBackground(SKSurface surface, Map map)
    {
        var mapRect = SKRect.Create(-map.Width / 2f, -map.Height / 2f, map.Width, map.Height);
        var backdropPaint = GetPaint(SKColors.White, SKPaintStyle.Fill, 1f);
        surface.Canvas.DrawRect(mapRect, backdropPaint);
    }

    private void RenderGrid(SKSurface surface, Map map, RenderingOptions options)
    {
        // Render grid cells
        var gridLineStyle = GetGridLineStyle(options);
        var paint = GetPaint(gridLineStyle.StrokeStyle, SKPaintStyle.Stroke, gridLineStyle.LineWidth);
        using var gridPath = new SKPath();

        float left = -map.Width / 2f;
        float right = map.Width / 2f;
        float bottom = -map.Height / 2f;
        float top = map.Height / 2f;

        const float epsilon = 0.001f;
        var chunkSize = 16;

        // Vertical lines
        float firstX = MathF.Floor(left / chunkSize) * chunkSize;
        for (float x = firstX; x < right + epsilon; x += chunkSize)
        {
            if (x >= left - epsilon && x <= right + epsilon)
            {
                gridPath.MoveTo(x, bottom);
                gridPath.LineTo(x, top);
            }
        }

        // Horizontal lines
        float firstY = MathF.Floor(bottom / chunkSize) * chunkSize;
        for (float y = firstY; y < top + epsilon; y += chunkSize)
        {
            if (y >= bottom - epsilon && y <= top + epsilon)
            {
                gridPath.MoveTo(left, y);
                gridPath.LineTo(right, y);
            }
        }

        surface.Canvas.DrawPath(gridPath, paint);

        // Render grid box
        var gridBoxPaint = GetPaint(gridLineStyle.StrokeStyle, SKPaintStyle.Stroke, options.GridBorderLineWidth);
        var origin = new Vector2(-map.Width / 2f, -map.Height / 2f);
        surface.Canvas.DrawRect(origin.X, origin.Y, map.Width, 
            map.Height, gridBoxPaint);
    }

    private void RenderMirrorAxis(SKSurface surface, Map map, RenderingOptions options)
    {
        if (map.Symmetry == null || !map.MirrorEnabled)
            return;

        // Render mirror line
        var mirrorLineStyle = GetMirrorLineStyle(options);
        var mirrorLinePaint = GetPaint(mirrorLineStyle.StrokeStyle, SKPaintStyle.Stroke, mirrorLineStyle.LineWidth);
        var start = map.Symmetry.GetStartPointWorld(map);
        var end = map.Symmetry.GetEndPointWorld(map);
        surface.Canvas.DrawLine(start.X, start.Y, end.X, end.Y, mirrorLinePaint);

        // Render rotation point
        if (map.Symmetry.RotationDeg == 180)
        {
            var mirrorPointStyle = GetMirrorPointStyle(options);
            var mirrorPointPaint = GetPaint(mirrorPointStyle.FillStyle, SKPaintStyle.Fill, mirrorPointStyle.LineWidth);
            var center = Vector2.Zero;
            var radius = mirrorPointStyle.Radius;
            surface.Canvas.DrawCircle(center.X, center.Y, radius, mirrorPointPaint);
        }
    }

    private void RenderNodes(SKSurface surface, IReadOnlyList<Node> nodes, 
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        foreach (var node in nodes)
        {
            var style = GetNodeStyle(node, hoveredNode, selectedNode, options);
            RenderNodeShape(surface, node.Position, style);
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
        var screenPos = position;

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

        surface.Canvas.DrawCircle(screenPos.X, screenPos.Y, style.Radius, circleFillPaint);
        surface.Canvas.DrawCircle(screenPos.X, screenPos.Y, style.Radius, circleStrokePaint);
    }

    private void RenderSquareNode(SKSurface surface, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 4);
        var topLeft = screenPos - new Vector2(size, size);

        var squareFillPaint = GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth);
        var squareStrokePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

        surface.Canvas.DrawRect(topLeft.X, topLeft.Y, size * 2, size * 2, squareFillPaint);
        surface.Canvas.DrawRect(topLeft.X, topLeft.Y, size * 2, size * 2, squareStrokePaint);
    }

    private void RenderDiamondNode(SKSurface surface, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 2);

        var left = new Vector2(screenPos.X - size, screenPos.Y);
        var top = new Vector2(screenPos.X, screenPos.Y - size);
        var right = new Vector2(screenPos.X + size, screenPos.Y);
        var bottom = new Vector2(screenPos.X, screenPos.Y + size);

        var diamondFillPaint = GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth);
        var diamondStrokePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

        SKPath diamond = new();
        diamond.MoveTo(left.X, left.Y);
        diamond.LineTo(top.X, top.Y);
        diamond.LineTo(right.X, right.Y);
        diamond.LineTo(bottom.X, bottom.Y);
        diamond.LineTo(left.X, left.Y);
        diamond.Close();

        surface.Canvas.DrawPath(diamond, diamondFillPaint);
        surface.Canvas.DrawPath(diamond, diamondStrokePaint);
    }

    private void RenderEdges(SKSurface surface, HashSet<Edge> edges, RenderingOptions options, float laneWidth, float limitX, float limitY)
    {
        if (edges.Count == 0)
            return;
        
        foreach (var edge in edges)
        {
            if (options.ShowBoundingBoxEnabled)
                RenderBoundingBox(surface, edge.Node1.Position, edge.Node2.Position, options, laneWidth);                

            if (options.ShowBlocksEnabled)
                RenderBlocks(surface, edge.EdgeBlocks, options, limitX, limitY);

            RenderEdge(surface, edge, options);
        }
    }

    private void RenderEdge(SKSurface surface, Edge edge, RenderingOptions options)
    {
        var style = options.GetStyle(edge.Type.ToString().ToLower());
        var p0 = edge.Node1.Position;
        var p1 = edge.Node2.Position;
        var edgePaint = GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth);

        surface.Canvas.DrawLine(p0, p1, edgePaint);
    }
    
    private void RenderBlocks(SKSurface surface, List<Vector2> positions, RenderingOptions options, float limitX, float limitY)
    {
        var blockPaint = GetPaint(options.CellFillStyle, SKPaintStyle.Stroke, 1f);
        SKPath blockList = new()
        {
            FillType = SKPathFillType.Winding
        };

        foreach (var block in positions)
        {
            var centerX = Math.Abs(block.X + 0.5f);
            var centerY = Math.Abs(block.Y + 0.5f);
            
            if (centerX <= limitX && centerY <= limitY)
            {
                var screenPos = block;
                var size = 1;

                blockList.AddRect(SKRect.Create(screenPos.X, screenPos.Y, size, size));
            } 
        }

        blockList.Close();
        surface.Canvas.DrawPath(blockList, blockPaint);
    }

    private void RenderBoundingBox(SKSurface surface, Vector2 pos1, Vector2 pos2,
        RenderingOptions options, float laneWidth)
    {
        var corners = Rectangle.FindRectCorners(pos1, pos2, laneWidth);

        var corner0 = corners[0];
        var corner1 = corners[1];
        var corner2 = corners[2];
        var corner3 = corners[3];

        var boundingBoxPaint = GetPaint(options.BoundingBoxLineStroke, SKPaintStyle.Stroke, 1f);
        surface.Canvas.DrawLine(corner0.X, corner0.Y, corner2.X, corner2.Y, boundingBoxPaint);
        surface.Canvas.DrawLine(corner2.X, corner2.Y, corner3.X, corner3.Y, boundingBoxPaint);
        surface.Canvas.DrawLine(corner3.X, corner3.Y, corner1.X, corner1.Y, boundingBoxPaint);
        surface.Canvas.DrawLine(corner1.X, corner1.Y, corner0.X, corner0.Y, boundingBoxPaint);
    }

    public Vector2 ScreenToWorldPos(Vector2 screen)
        => new((screen.X - CameraPosition.X) / Scale, (screen.Y - CameraPosition.Y) / Scale);

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
