using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class EdgeRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        foreach (var edge in context.Map.Graph.Edges)
        {
            var style = context.Options.GetEdgeStyle(edge.Type);
            var p0 = edge.Node1.Position;
            var p1 = edge.Node2.Position;
            var edgePaint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth, context.Viewport.Scale);

            context.Surface.Canvas.DrawLine(p0, p1, edgePaint);
        }
    }
}
