using Microsoft.Extensions.Options;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class MapBlocksRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        var blockPaint = context.Cache.GetPaint(context.Options.CellFillStyle, SKPaintStyle.Stroke, 1f, context.Viewport.Scale);
        SKPath blockList = new()
        {
            FillType = SKPathFillType.Winding
        };

        foreach (var block in context.Map.Blocks)
        {
            var centerX = Math.Abs(block.X + 0.5f);
            var centerY = Math.Abs(block.Y + 0.5f);

            if (centerX <= context.LimitX && centerY <= context.LimitY)
            {
                var screenPos = block;
                var size = 1;

                blockList.AddRect(SKRect.Create(screenPos.X, screenPos.Y, size, size));
            }
        }

        blockList.Close();
        context.Surface.Canvas.DrawPath(blockList, blockPaint);
    }
}
