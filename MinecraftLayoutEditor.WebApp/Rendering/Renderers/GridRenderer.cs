using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class GridRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        // Render grid cells
        var gridLineStyle = context.Options.GetStyle("gridLineStyle");
        var paint = context.Cache.GetPaint(gridLineStyle.StrokeStyle, SKPaintStyle.Stroke, gridLineStyle.LineWidth, context.Scale);
        using var gridPath = new SKPath();

        float left = -context.Map.Width / 2f;
        float right = context.Map.Width / 2f;
        float bottom = -context.Map.Height / 2f;
        float top = context.Map.Height / 2f;

        const float epsilon = 0.001f;
        var chunkSize = 16;

        // Vertical lines
        float firstX = MathF.Floor(left / chunkSize) * chunkSize;
        for (float x = firstX; x < right + epsilon; x += chunkSize)
        {
            if (x >= left - epsilon && x <= right + epsilon)
            {
                gridPath.MoveTo(x, bottom);
                gridPath.LineTo(x, top);
            }
        }

        // Horizontal lines
        float firstY = MathF.Floor(bottom / chunkSize) * chunkSize;
        for (float y = firstY; y < top + epsilon; y += chunkSize)
        {
            if (y >= bottom - epsilon && y <= top + epsilon)
            {
                gridPath.MoveTo(left, y);
                gridPath.LineTo(right, y);
            }
        }

        context.Surface.Canvas.DrawPath(gridPath, paint);

        // Render grid box
        var gridBoxPaint = context.Cache.GetPaint(gridLineStyle.StrokeStyle, SKPaintStyle.Stroke, context.Options.GridBorderLineWidth, context.Scale);
        var origin = new Vector2(-context.Map.Width / 2f, -context.Map.Height / 2f);
        context.Surface.Canvas.DrawRect(origin.X, origin.Y, context.Map.Width,
            context.Map.Height, gridBoxPaint);
    }
}
