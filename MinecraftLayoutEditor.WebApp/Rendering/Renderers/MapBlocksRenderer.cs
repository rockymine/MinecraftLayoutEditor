using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class MapBlocksRenderer : IRenderable, IDisposable
{
    private readonly BlockGeometry _geometry = new();

    public void Render(RenderContext context)
    {
        var blockPaint = context.Cache.GetPaint(context.Options.CellFillStyle,
            SKPaintStyle.Stroke, 1f, context.Viewport.Scale);

        _geometry.Draw(
            context.Surface!.Canvas,
            blockPaint,
            context.Map.Blocks,
            context.Map.BlocksRevision,
            context.Viewport.VisibleWorldRect(),
            context.LimitX,
            context.LimitY);
    }

    public void Dispose()
    {
        _geometry.Dispose();
        GC.SuppressFinalize(this);
    }
}
