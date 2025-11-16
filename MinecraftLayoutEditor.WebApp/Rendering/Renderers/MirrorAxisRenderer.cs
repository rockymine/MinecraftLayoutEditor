using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class MirrorAxisRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        if (context.Map.Symmetry == null || !context.Map.MirrorEnabled)
            return;

        // Render mirror line
        var mirrorLineStyle = context.Options.GetStyle("mirrorLineStyle");
        var mirrorLinePaint = context.Cache.GetPaint(mirrorLineStyle.StrokeStyle, SKPaintStyle.Stroke, mirrorLineStyle.LineWidth, context.Viewport.Scale);
        var start = context.Map.Symmetry.GetStartPointWorld(context.Map);
        var end = context.Map.Symmetry.GetEndPointWorld(context.Map);
        context.Surface.Canvas.DrawLine(start.X, start.Y, end.X, end.Y, mirrorLinePaint);

        // Render rotation point
        if (context.Map.Symmetry.RotationDeg == 180)
        {
            var mirrorPointStyle = context.Options.GetStyle("mirrorPointStyle");
            var mirrorPointPaint = context.Cache.GetPaint(mirrorPointStyle.FillStyle, SKPaintStyle.Fill, mirrorPointStyle.LineWidth, context.Viewport.Scale);
            var center = Vector2.Zero;
            var radius = mirrorPointStyle.Radius;
            context.Surface.Canvas.DrawCircle(center.X, center.Y, radius, mirrorPointPaint);
        }
    }
}
