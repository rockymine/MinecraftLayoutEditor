using MinecraftLayoutEditor.Logic.Geometry;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class EdgeBoundingBoxRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        if (!context.Options.ShowBoundingBoxEnabled)
            return;
        
        var uniqueEdges = context.Map.Graph.GetUniqueEdges();

        foreach (var uniqueEdge in uniqueEdges)
        {

            var corners = Rectangle.FindRectCorners(uniqueEdge.Node1.Position, uniqueEdge.Node2.Position, context.Map.LaneWidth);

            var corner0 = corners[0];
            var corner1 = corners[1];
            var corner2 = corners[2];
            var corner3 = corners[3];

            var boundingBoxPaint = context.Cache.GetPaint(context.Options.BoundingBoxLineStroke, SKPaintStyle.Stroke, 1f, context.Viewport.Scale);
            context.Surface.Canvas.DrawLine(corner0.X, corner0.Y, corner2.X, corner2.Y, boundingBoxPaint);
            context.Surface.Canvas.DrawLine(corner2.X, corner2.Y, corner3.X, corner3.Y, boundingBoxPaint);
            context.Surface.Canvas.DrawLine(corner3.X, corner3.Y, corner1.X, corner1.Y, boundingBoxPaint);
            context.Surface.Canvas.DrawLine(corner1.X, corner1.Y, corner0.X, corner0.Y, boundingBoxPaint);
        }
    }
}
