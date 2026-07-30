using MinecraftLayoutEditor.Logic;
using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class NodeRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        foreach (var node in context.Map.Graph.Nodes)
        {
            var style = GetNodeStyle(node, context);
            RenderNodeShape(context, node.Position, style);
        }
    }

    private static RenderStyle GetNodeStyle(Node node, RenderContext context)
    {
        var style = context.Options.GetNodeStyle(node.Type);

        if (node == context.HoveredNode)
        {
            return style with { StrokeStyle = context.Options.HoveredNodeStroke };
        }

        if (node == context.SelectedNode)
        {
            return style with { StrokeStyle = context.Options.SelectedNodeStroke };
        }

        return style;
    }

    private void RenderNodeShape(RenderContext context, Vector2 position, RenderStyle style)
    {
        var screenPos = position;

        switch (style.Shape)
        {
            case NodeShape.Square:
                RenderSquareNode(context, screenPos, style);
                break;
            case NodeShape.Diamond:
                RenderDiamondNode(context, screenPos, style);
                break;
            default:
                RenderCircleNode(context, screenPos, style);
                break;
        }
    }

    private void RenderCircleNode(RenderContext context, Vector2 screenPos, RenderStyle style)
    {
        var circleFillPaint = context.Cache.GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth, context.Viewport.Scale);
        var circleStrokePaint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth, context.Viewport.Scale);

        context.Surface.Canvas.DrawCircle(screenPos.X, screenPos.Y, style.Radius, circleFillPaint);
        context.Surface.Canvas.DrawCircle(screenPos.X, screenPos.Y, style.Radius, circleStrokePaint);
    }

    private void RenderSquareNode(RenderContext context, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 4);
        var topLeft = screenPos - new Vector2(size, size);

        var squareFillPaint = context.Cache.GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth, context.Viewport.Scale);
        var squareStrokePaint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth, context.Viewport.Scale);

        context.Surface.Canvas.DrawRect(topLeft.X, topLeft.Y, size * 2, size * 2, squareFillPaint);
        context.Surface.Canvas.DrawRect(topLeft.X, topLeft.Y, size * 2, size * 2, squareStrokePaint);
    }

    private void RenderDiamondNode(RenderContext context, Vector2 screenPos, RenderStyle style)
    {
        var size = style.Radius * (float)Math.Sqrt(Math.PI / 2);

        var left = new Vector2(screenPos.X - size, screenPos.Y);
        var top = new Vector2(screenPos.X, screenPos.Y - size);
        var right = new Vector2(screenPos.X + size, screenPos.Y);
        var bottom = new Vector2(screenPos.X, screenPos.Y + size);

        var diamondFillPaint = context.Cache.GetPaint(style.FillStyle, SKPaintStyle.Fill, style.LineWidth, context.Viewport.Scale);
        var diamondStrokePaint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth, context.Viewport.Scale);

        SKPath diamond = new();
        diamond.MoveTo(left.X, left.Y);
        diamond.LineTo(top.X, top.Y);
        diamond.LineTo(right.X, right.Y);
        diamond.LineTo(bottom.X, bottom.Y);
        diamond.LineTo(left.X, left.Y);
        diamond.Close();

        context.Surface.Canvas.DrawPath(diamond, diamondFillPaint);
        context.Surface.Canvas.DrawPath(diamond, diamondStrokePaint);
    }
}
