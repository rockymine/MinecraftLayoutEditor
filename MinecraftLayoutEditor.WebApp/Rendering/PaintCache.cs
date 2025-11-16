using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class PaintCache
{
    private readonly Dictionary<(SKColor, SKPaintStyle, float), SKPaint> _paintCache = [];

    public SKPaint GetPaint(SKColor color, SKPaintStyle style, float lineWidth, float scale)
    {
        var adjustedWidth = (style == SKPaintStyle.Stroke) ? lineWidth / Math.Max(scale, 0.001f) : lineWidth;

        var key = (color, style, adjustedWidth);
        if (!_paintCache.TryGetValue(key, out var paint))
        {
            paint = new SKPaint
            {
                Color = color,
                Style = style,
                StrokeWidth = adjustedWidth,
                IsAntialias = false
            };
            _paintCache.Add(key, paint);
        }
        return paint;
    }
}
