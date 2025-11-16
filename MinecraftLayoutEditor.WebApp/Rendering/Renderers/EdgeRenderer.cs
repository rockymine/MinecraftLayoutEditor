using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class EdgeRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        var uniqueEdges = context.Map.Graph.GetUniqueEdges();

        foreach (var edge in uniqueEdges)
        {
            var style = context.Options.GetStyle(edge.Type.ToString().ToLower());
            var p0 = edge.Node1.Position;
            var p1 = edge.Node2.Position;
            var edgePaint = context.Cache.GetPaint(style.StrokeStyle, SKPaintStyle.Stroke, style.LineWidth, context.Viewport.Scale);

            context.Surface.Canvas.DrawLine(p0, p1, edgePaint);
        }
    }
}
