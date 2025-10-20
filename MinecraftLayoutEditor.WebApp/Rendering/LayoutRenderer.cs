using Excubo.Blazor.Canvas.Contexts;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.WebApp.Extensions;
using Excubo.Blazor.Canvas;
using System.Numerics;
using MinecraftLayoutEditor.Logic.Geometry;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class LayoutRenderer
{
    private readonly GridRenderer _gridRenderer = new();
    private Vector2 Translation = new(25, 25);
    private float Scale = 20f;

    public async Task RenderAsync(Context2D ctx, Logic.Layout layout,
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        var uniqueEdges = new HashSet<Edge>();
        var nodes = layout.Graph.Nodes;

        foreach (var n in nodes)
        {
            foreach (var e in n.Edges)
            {
                uniqueEdges.Add(e); 
            }
        }

        await ctx.ClearRectAsync(0, 0, 1000, 1000);

        // Render the grid cells
        await _gridRenderer.RenderAsync(ctx, options.GridSpacing, options.GridLineWidth, options.GridLineStroke, layout, this);

        // Render a box around the grid
        await ctx.DrawRect(WorldToScreenPos(new Vector2(-layout.Width / 2f, -layout.Height / 2f)), 
            WorldToScreenScale(layout.Width), WorldToScreenScale(layout.Height), options.GridBorderLineWidth, 
            options.GridLineStroke, []);

        // Render the mirror line and mirror point
        if (layout.Symmetry != null && layout.MirrorEnabled)
        {
            await ctx.DrawLine(WorldToScreenPos(layout.Symmetry.GetStartPointWorld(layout)),
                WorldToScreenPos(layout.Symmetry.GetEndPointWorld(layout)), options.MirrorLineWidth, 
                options.MirrorLineStroke, options.MirrorLineDash);

            if (layout.Symmetry.RotationDeg == 180)
            {
                await ctx.DrawCircle(WorldToScreenPos(Vector2.Zero), options.MirrorPointRadius, 
                    options.MirrorPointRadius, options.MirrorPointLineWidth, options.MirrorPointFill, 
                    options.MirrorPointLineStroke, FillRule.NonZero);
            }
        }

        // Render the graph
        await RenderEdges(ctx, uniqueEdges, layout, options);
        await RenderNodes(ctx, nodes, hoveredNode, selectedNode, options);
    }

    private async Task RenderNodes(Context2D ctx, IReadOnlyList<Node> nodes, 
        Node? hoveredNode, Node? selectedNode, RenderingOptions options)
    {
        foreach (var n in nodes)
        {
            var baseStyle = options.GetStyle(n.Type);
            var finalStroke = baseStyle.StrokeStyle;

            if (n == hoveredNode)
            {
                finalStroke = options.HoveredNodeStroke;
            }
            else if (n == selectedNode)
            {
                finalStroke = options.SelectedNodeStroke;
            }

            if (baseStyle.Shape == "circle")
            {
                await ctx.DrawCircle(WorldToScreenPos(n.Position), baseStyle.Radius, baseStyle.Radius,
                    baseStyle.LineWidth, baseStyle.FillStyle, finalStroke, FillRule.NonZero);
            } 
            else if (baseStyle.Shape == "square")
            {
                var size = baseStyle.Radius * (float)Math.Sqrt(Math.PI / 4);
                var topLeft = WorldToScreenPos(n.Position) - new Vector2(size, size);
                
                await ctx.DrawRect(topLeft, size * 2, size * 2, 
                    baseStyle.LineWidth, finalStroke, baseStyle.LineDash, baseStyle.FillStyle);
            } else if (baseStyle.Shape == "diamond")
            {
                var size = baseStyle.Radius * (float)Math.Sqrt(Math.PI / 2);

                await ctx.DrawDiamond(WorldToScreenPos(n.Position), size, size,
                    baseStyle.LineWidth, finalStroke, baseStyle.LineDash, baseStyle.FillStyle);
            }
        }
    }

    private async Task RenderEdges(Context2D ctx, HashSet<Edge> edges, Logic.Layout layout, RenderingOptions options)
    {
        foreach (var e in edges)
        {
            await RenderBlockEdges(ctx, e.Node1.Position, e.Node2.Position, layout, options);

            var baseStyle = options.GetStyle(e.Type);

            await ctx.DrawLine(WorldToScreenPos(e.Node1.Position), WorldToScreenPos(e.Node2.Position), 
                baseStyle.LineWidth, baseStyle.StrokeStyle, baseStyle.LineDash);
        }
    }

    public async Task RenderBlockEdges(Context2D ctx, Vector2 pos1, Vector2 pos2, Logic.Layout layout, RenderingOptions options)
    {
        // Draw bounding box
        var corners = Rectangle.FindRectCorners(pos1, pos2, 4f);
        await ctx.DrawRect(WorldToScreenPos(corners[0]), WorldToScreenPos(corners[2]),
            WorldToScreenPos(corners[3]), WorldToScreenPos(corners[1]), 0.5f, options.BoundingBoxLineStroke, []);

        if (!options.ShowBlocksEnabled)
            return;

        // Draw blocks
        foreach (var block in Rectangle.DiscretePointsInsideRect(pos1, pos2, 4f))
        {
            await ctx.DrawRect(WorldToScreenPos(block), WorldToScreenScale(1), WorldToScreenScale(1), 
                1, "black", [], options.CellFillStyle);
        }
    }

    public Vector2 WorldToScreenPos(Vector2 worldPos)
    {
        var screenCoord = worldPos + Translation;

        return screenCoord * Scale;
    }

    public Vector2 ScreenToWorldPos(Vector2 screenPos)
    {
        var worldPos = screenPos / Scale;

        return worldPos - Translation;
    }

    public float WorldToScreenScale(float worldLength)
    {
        return worldLength * Scale;
    }
}
