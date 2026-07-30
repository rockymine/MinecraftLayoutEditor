using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

/// <summary>
/// Reuses the SKPaint objects the renderers ask for.
///
/// The canvas draws in world units, so a stroke that should be one screen pixel wide
/// has to be divided by the current zoom. Including that divided width in the cache
/// key mints a new native paint for every zoom level ever visited and never releases
/// it. The key is the paint's stable identity instead, and the stroke width is
/// assigned on the way out.
/// </summary>
public class PaintCache : IDisposable
{
    private readonly Dictionary<(SKColor, SKPaintStyle, float), SKPaint> _paintCache = [];

    public SKPaint GetPaint(SKColor color, SKPaintStyle style, float lineWidth, float scale)
    {
        var key = (color, style, lineWidth);

        if (!_paintCache.TryGetValue(key, out var paint))
        {
            paint = new SKPaint
            {
                Color = color,
                Style = style,
                IsAntialias = false
            };
            _paintCache.Add(key, paint);
        }

        paint.StrokeWidth = (style == SKPaintStyle.Stroke)
            ? lineWidth / Math.Max(scale, 0.001f)
            : lineWidth;

        return paint;
    }

    public void Dispose()
    {
        foreach (var paint in _paintCache.Values)
            paint.Dispose();

        _paintCache.Clear();
        GC.SuppressFinalize(this);
    }
}
