using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class GridRenderer
{  
    public void Render(SKSurface surface, int gridSpacing, float gridLineWidth, SKColor gridStrokeStyle, 
        Logic.Layout layout, LayoutRenderer renderer)
    {
        var gridOrigin = new Vector2(-layout.Width / 2f, -layout.Height / 2f);
        var paint = renderer.GetPaint(gridStrokeStyle, SKPaintStyle.Stroke, gridLineWidth);

        // Add all vertical lines to the path
        for (float x = gridOrigin.X + gridSpacing; x < layout.Width / 2f; x += gridSpacing)
        {
            var pos1 = renderer.WorldToScreenPos(new Vector2(x, -layout.Height / 2f));
            var pos2 = renderer.WorldToScreenPos(new Vector2(x, layout.Height / 2f));

            surface.Canvas.DrawLine(pos1.X, pos1.Y, pos2.X, pos2.Y, paint);
        }

        // Add all horizontal lines to the same path
        for (float y = gridOrigin.Y; y < layout.Height / 2f; y += gridSpacing)
        {
            var pos1 = renderer.WorldToScreenPos(new Vector2(-layout.Width / 2f, y));
            var pos2 = renderer.WorldToScreenPos(new Vector2(layout.Width / 2f, y));

            surface.Canvas.DrawLine(pos1.X, pos1.Y, pos2.X, pos2.Y, paint);
        }
    }
}
