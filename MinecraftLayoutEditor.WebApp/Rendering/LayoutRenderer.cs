using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using MinecraftLayoutEditor.WebApp.Extensions;
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

    public LayoutRenderer()
    {
        UpdateTRS(new Vector2(25, 25), 20f);
    }

    public void UpdateTRS(Vector2 translation, float scale)
    {
        Console.WriteLine($"scale={scale}, trans=({translation.X}, {translation.Y})");

        WorldToScreen = Matrix4x4.CreateTranslation(translation.X, translation.Y, 0) * Matrix4x4.CreateScale(scale);
        Matrix4x4.Invert(WorldToScreen, out ScreenToWorld);
        Scale = scale;
        CameraPosition = translation;
    }

    public async Task RenderAsync(IContext2DWithoutGetters ctx, Logic.Layout layout,
        Node? hoveredNode, Node? selectedNode, RenderingOptions options, RenderTrigger trigger)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var uniqueEdges = GetUniqueEdges(layout.Graph.Nodes);

        await ctx.ClearRectAsync(0, 0, CanvasWidth, CanvasHeight);

        Console.WriteLine($"Render trigger: {trigger}");

        var gridStopwatch = Stopwatch.StartNew();
        await RenderGrid(ctx, layout, options);
        gridStopwatch.Stop();
        Console.WriteLine($"Grid: {gridStopwatch.ElapsedMilliseconds}ms (dimension: {layout.Width} x {layout.Height})");

        var mirrorStopwatch = Stopwatch.StartNew();
        await RenderMirrorAxis(ctx, layout, options);
        mirrorStopwatch.Stop();
        Console.WriteLine($"Mirror: {mirrorStopwatch.ElapsedMilliseconds}ms");

        var edgesStopwatch = Stopwatch.StartNew();
        await RenderEdges(ctx, uniqueEdges, options, layout.LaneWidth);
        edgesStopwatch.Stop();
        Console.WriteLine($"Edges: {edgesStopwatch.ElapsedMilliseconds}ms (count: {uniqueEdges.Count}, bounding box: {options.ShowBoundingBoxEnabled}, bounding box: {options.ShowBlocksEnabled}, width: {layout.LaneWidth})");

        var nodesStopwatch = Stopwatch.StartNew();
        await RenderNodes(ctx, layout.Graph.Nodes, hoveredNode, selectedNode, options);
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

    private async Task RenderGrid(IContext2DWithoutGetters ctx, Logic.Layout layout, RenderingOptions options)
    {
        // Render grid cells
        var gridLineStyle = GetGridLineStyle(options);
        await _gridRenderer.RenderAsync(ctx, options.GridSpacing, gridLineStyle.LineWidth, 
            gridLineStyle.StrokeStyle, layout, this);

        // Render grid box
        await ctx.DrawRect(WorldToScreenPos(new Vector2(-layout.Width / 2f, -layout.Height / 2f)),
            WorldToScreenScale(layout.Width), WorldToScreenScale(layout.Height), options.GridBorderLineWidth,
            gridLineStyle.StrokeStyle, []);
    }

    private async Task RenderMirrorAxis(IContext2DWithoutGetters ctx, Logic.Layout layout, RenderingOptions options)
    {
        if (layout.Symmetry == null || !layout.MirrorEnabled)
            return;

        // Render mirror line
        var mirrorLineStyle = GetMirrorLineStyle(options);
        await ctx.DrawLine(WorldToScreenPos(layout.Symmetry.GetStartPointWorld(layout)),
            WorldToScreenPos(layout.Symmetry.GetEndPointWorld(layout)), mirrorLineStyle.LineWidth,
            mirrorLineStyle.StrokeStyle, mirrorLineStyle.LineDash);

        // Render rotation point
        if (layout.Symmetry.RotationDeg == 180)
        {
            var mirrorPointStyle = GetMirrorPointStyle(options);
            await ctx.DrawCircle(WorldToScreenPos(Vector2.Zero), WorldToScreenScale(mirrorPointStyle.Radius),
                WorldToScreenScale(mirrorPointStyle.Radius), mirrorPointStyle.LineWidth, mirrorPointStyle.FillStyle,
                mirrorPointStyle.StrokeStyle, FillRule.NonZero);
        }
    }

    private async Task RenderNodes(IContext2DWithoutGetters ctx, IReadOnlyList<Node> nodes, 
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        foreach (var n in nodes)
        {
            var style = GetNodeStyle(n, hoveredNode, selectedNode, options);
            await RenderNodeShape(ctx, n.Position, style);
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

    private async Task RenderNodeShape(IContext2DWithoutGetters ctx, Vector2 position, RenderStyle style)
    {
        var screenPos = WorldToScreenPos(position);

        switch (style.Shape.ToLower())
        {
            case "circle":
                await RenderCircleNode(ctx, screenPos, style);
                break;
            case "square":
                await RenderSquareNode(ctx, screenPos, style);
                break;
            case "diamond":
                await RenderDiamondNode(ctx, screenPos, style);
                break;
            default:
                await RenderCircleNode(ctx, screenPos, style);
                break;
        }
    }

    private async Task RenderCircleNode(IContext2DWithoutGetters ctx, Vector2 screenPos, RenderStyle style)
    {
        await ctx.DrawCircle(screenPos, WorldToScreenScale(style.Radius), WorldToScreenScale(style.Radius),
            style.LineWidth, style.FillStyle, style.StrokeStyle, FillRule.NonZero);
    }

    private async Task RenderSquareNode(IContext2DWithoutGetters ctx, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 4);
        var topLeft = screenPos - new Vector2(WorldToScreenScale(size), WorldToScreenScale(size));

        await ctx.DrawRect(topLeft, WorldToScreenScale(size * 2), WorldToScreenScale(size * 2),
            style.LineWidth, style.StrokeStyle, style.LineDash, style.FillStyle);
    }

    private async Task RenderDiamondNode(IContext2DWithoutGetters ctx, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 2);

        await ctx.DrawDiamond(screenPos, WorldToScreenScale(size), WorldToScreenScale(size),
            style.LineWidth, style.StrokeStyle, style.LineDash, style.FillStyle);
    }

    private async Task RenderEdges(IContext2DWithoutGetters ctx, HashSet<Edge> edges, RenderingOptions options, float laneWidth)
    {
        if (edges.Count == 0)
            return;
        
        foreach (var e in edges)
        {
            // Render path bounding box preview
            if (options.ShowBoundingBoxEnabled)
                await RenderEdgeBoundingBox(ctx, e.Node1.Position, e.Node2.Position, options, laneWidth);

            // Render schematic preview
            if (options.ShowBlocksEnabled)
                await RenderEdgeSchematicBlocks(ctx, e, options);

            var style = options.GetStyle(e.Type.ToString().ToLower());

            await ctx.DrawLine(WorldToScreenPos(e.Node1.Position), WorldToScreenPos(e.Node2.Position), 
                style.LineWidth, style.StrokeStyle, style.LineDash);
        }
    }

    private async Task RenderEdgeSchematicBlocks(IContext2DWithoutGetters ctx, Edge edge, 
        RenderingOptions options)
    {
        Console.WriteLine($"Edge blocks count: { edge.EdgeBlocks.Count}");
        
        foreach (var block in edge.EdgeBlocks)
        {
            await ctx.DrawRect(WorldToScreenPos(block), WorldToScreenScale(1), WorldToScreenScale(1),
                1, "black", [], options.CellFillStyle);
        }
    }

    private async Task RenderEdgeBoundingBox(IContext2DWithoutGetters ctx, Vector2 pos1, Vector2 pos2, 
        RenderingOptions options, float laneWidth)
    {
        var corners = Rectangle.FindRectCorners(pos1, pos2, laneWidth);

        await ctx.DrawRect(WorldToScreenPos(corners[0]), WorldToScreenPos(corners[2]),
            WorldToScreenPos(corners[3]), WorldToScreenPos(corners[1]), 0.5f, options.BoundingBoxLineStroke, []);
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
