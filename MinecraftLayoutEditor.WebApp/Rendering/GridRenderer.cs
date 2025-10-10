using Excubo.Blazor.Canvas.Contexts;
using MinecraftLayoutEditor.WebApp.Extensions;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class GridRenderer
{
    public async Task RenderAsync(Context2D ctx, int gridSpacing, Logic.Layout layout, LayoutRenderer renderer)
    {
        var gridStroke = "black";
        var gridLineWidth = 0.5f;
        
        await ctx.SaveAsync();
        await ctx.BeginPathAsync();

        var gridOrigin = new Vector2(-layout.Width / 2, -layout.Height / 2);

        // vertical lines
        for (float x = gridOrigin.X + gridSpacing; x < layout.Width / 2; x += gridSpacing)
        {
            var pos1 = new Vector2(x, -layout.Height / 2);
            var pos2 = new Vector2(x, layout.Height / 2);

            await ctx.DrawLine(renderer.WorldToScreenPos(pos1), renderer.WorldToScreenPos(pos2), 
                gridLineWidth, gridStroke, []);
        }

        // horizontal lines
        for (float y = gridOrigin.Y; y < layout.Height / 2; y += gridSpacing)
        {
            var pos1 = new Vector2(-layout.Width / 2, y);
            var pos2 = new Vector2(layout.Width / 2, y);

            await ctx.DrawLine(renderer.WorldToScreenPos(pos1), renderer.WorldToScreenPos(pos2), 
                gridLineWidth, gridStroke, []);
        }
    }
}
