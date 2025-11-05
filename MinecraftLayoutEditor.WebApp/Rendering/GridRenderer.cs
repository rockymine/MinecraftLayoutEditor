using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class GridRenderer
{
    public void Render(SKSurface surface, int chunkSize, float gridLineWidth, SKColor gridStrokeStyle,
        Logic.Layout layout, LayoutRenderer renderer)
    {
        var paint = renderer.GetPaint(gridStrokeStyle, SKPaintStyle.Stroke, gridLineWidth);
        using var gridPath = new SKPath();

        float left = -layout.Width / 2f;
        float right = layout.Width / 2f;
        float bottom = -layout.Height / 2f;
        float top = layout.Height / 2f;

        const float epsilon = 0.001f;  // Tolerance for float comparison

        // --- Vertical chunk lines ---
        float firstX = MathF.Floor(left / chunkSize) * chunkSize;
        for (float x = firstX; x < right + epsilon; x += chunkSize)
        {
            // Only draw if line intersects layout bounds
            if (x >= left - epsilon && x <= right + epsilon)
            {
                gridPath.MoveTo(x, bottom);
                gridPath.LineTo(x, top);
            }
        }

        // --- Horizontal chunk lines ---
        float firstY = MathF.Floor(bottom / chunkSize) * chunkSize;
        for (float y = firstY; y < top + epsilon; y += chunkSize)
        {
            // Only draw if line intersects layout bounds
            if (y >= bottom - epsilon && y <= top + epsilon)
            {
                gridPath.MoveTo(left, y);
                gridPath.LineTo(right, y);
            }
        }

        surface.Canvas.DrawPath(gridPath, paint);
    }
}
