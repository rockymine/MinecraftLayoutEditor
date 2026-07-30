using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

/// <summary>
/// Reuses the SKPaint objects the renderers ask for.
///
/// The canvas draws in world units, so a stroke that should be one screen pixel wide
/// has to be divided by the current zoom. Including that divided width in the cache
/// key mints a new native paint for every zoom level ever visited and never releases
/// it. The key is the paint's stable identity instead, and the width - and the dash
/// pattern, which is in screen units for the same reason - are assigned on the way out.
/// </summary>
public class PaintCache : IDisposable
{
    private readonly Dictionary<(SKColor, SKPaintStyle, float, double[]?), SKPaint> _paintCache = [];

    // Dash patterns are compared by array identity: the styles that own them are built
    // once, so each distinct pattern is one array instance for the life of the app.
    private readonly Dictionary<double[], DashEffect> _dashEffects = [];

    public SKPaint GetPaint(SKColor color, SKPaintStyle style, float lineWidth, float scale,
        double[]? lineDash = null)
    {
        var dashPattern = (lineDash != null && lineDash.Length > 0) ? lineDash : null;
        var key = (color, style, lineWidth, dashPattern);

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

        if (dashPattern != null)
            paint.PathEffect = DashEffectFor(dashPattern, scale);

        return paint;
    }

    /// <summary>
    /// The dash effect for a pattern at the current zoom. Dash lengths are screen
    /// distances, like stroke widths, so the effect has to be rebuilt when the zoom
    /// changes - but only one is ever alive per pattern, and the previous one is
    /// released rather than left to accumulate.
    /// </summary>
    private SKPathEffect DashEffectFor(double[] dashPattern, float scale)
    {
        if (_dashEffects.TryGetValue(dashPattern, out var cached) && cached.Scale == scale)
            return cached.Effect;

        cached?.Effect.Dispose();

        var effect = SKPathEffect.CreateDash(ToScreenIntervals(dashPattern, scale), 0);
        _dashEffects[dashPattern] = new DashEffect(effect, scale);
        return effect;
    }

    /// <summary>
    /// Converts a dash pattern to the world-unit intervals Skia needs. Skia requires an
    /// even number of intervals - alternating on and off - so an odd-length pattern is
    /// repeated once, which is the same reading the web platform gives it: [5] means
    /// five on, five off.
    /// </summary>
    private static float[] ToScreenIntervals(double[] dashPattern, float scale)
    {
        var count = dashPattern.Length % 2 == 0 ? dashPattern.Length : dashPattern.Length * 2;
        var intervals = new float[count];
        var worldUnitsPerScreenPixel = 1f / Math.Max(scale, 0.001f);

        for (int index = 0; index < count; index++)
            intervals[index] = (float)dashPattern[index % dashPattern.Length] * worldUnitsPerScreenPixel;

        return intervals;
    }

    public void Dispose()
    {
        foreach (var paint in _paintCache.Values)
            paint.Dispose();

        foreach (var dashEffect in _dashEffects.Values)
            dashEffect.Effect.Dispose();

        _paintCache.Clear();
        _dashEffects.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed record DashEffect(SKPathEffect Effect, float Scale);
}
