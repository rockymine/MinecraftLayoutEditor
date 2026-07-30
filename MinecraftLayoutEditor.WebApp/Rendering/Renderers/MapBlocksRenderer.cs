using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class MapBlocksRenderer : IRenderable, IDisposable
{
    private readonly BlockGeometry _geometry = new();

    /// <summary>Rectangles the cell set collapsed to after merging.</summary>
    public int MergedRectangles => _geometry.MergedRectangles;

    public void Render(RenderContext context)
    {
        // Cells are filled, not outlined. An outline stroked at one screen pixel is
        // wider than the cell itself once the map is zoomed out, so each cell painted
        // over its neighbours and the layer read as a hatch rather than as ground.
        var blockPaint = context.Cache.GetPaint(context.Options.CellFillStyle,
            SKPaintStyle.Fill, 1f, context.Viewport.Scale);

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
