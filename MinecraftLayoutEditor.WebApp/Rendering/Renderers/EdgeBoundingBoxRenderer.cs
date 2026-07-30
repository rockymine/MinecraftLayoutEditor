using MinecraftLayoutEditor.Logic.Geometry;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class EdgeBoundingBoxRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        if (!context.Options.ShowBoundingBoxEnabled)
            return;

        var boundingBoxPaint = context.Cache.GetPaint(context.Options.BoundingBoxLineStroke,
            SKPaintStyle.Stroke, 1f, context.Viewport.Scale);

        foreach (var edge in context.Map.Graph.Edges)
        {
            var corners = Rectangle.FindRectCorners(
                edge.Node1.Position, edge.Node2.Position, context.Map.LaneWidth);

            DrawLine(context, corners.NearLeft, corners.FarLeft, boundingBoxPaint);
            DrawLine(context, corners.FarLeft, corners.FarRight, boundingBoxPaint);
            DrawLine(context, corners.FarRight, corners.NearRight, boundingBoxPaint);
            DrawLine(context, corners.NearRight, corners.NearLeft, boundingBoxPaint);
        }
    }

    private static void DrawLine(RenderContext context, System.Numerics.Vector2 from,
        System.Numerics.Vector2 to, SKPaint paint)
    {
        context.Surface!.Canvas.DrawLine(from.X, from.Y, to.X, to.Y, paint);
    }
}
