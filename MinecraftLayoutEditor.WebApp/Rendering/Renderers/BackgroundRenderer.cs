using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class BackgroundRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        var mapRect = SKRect.Create(-context.Map.Width / 2f, -context.Map.Height / 2f, context.Map.Width, context.Map.Height);
        var backdropPaint = context.Cache.GetPaint(SKColors.Pink, SKPaintStyle.Fill, 1f, context.Viewport.Scale);
        context.Surface.Canvas.DrawRect(mapRect, backdropPaint);
    }
}
