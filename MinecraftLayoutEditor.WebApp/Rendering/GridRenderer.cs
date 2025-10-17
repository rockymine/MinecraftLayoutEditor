using Excubo.Blazor.Canvas.Contexts;
using MinecraftLayoutEditor.WebApp.Extensions;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class GridRenderer
{
    public async Task RenderAsync(Context2D ctx, int gridSpacing, float gridLineWidth, string gridStrokeStyle, Logic.Layout layout, LayoutRenderer renderer)
    {
        await ctx.SaveAsync();
        await ctx.BeginPathAsync();

        var gridOrigin = new Vector2(-layout.Width / 2f, -layout.Height / 2f);

        // vertical lines
        for (float x = gridOrigin.X + gridSpacing; x < layout.Width / 2f; x += gridSpacing)
        {
            var pos1 = new Vector2(x, -layout.Height / 2f);
            var pos2 = new Vector2(x, layout.Height / 2f);

            await ctx.DrawLine(renderer.WorldToScreenPos(pos1), renderer.WorldToScreenPos(pos2), 
                gridLineWidth, gridStrokeStyle, []);
        }

        // horizontal lines
        for (float y = gridOrigin.Y; y < layout.Height / 2f; y += gridSpacing)
        {
            var pos1 = new Vector2(-layout.Width / 2f, y);
            var pos2 = new Vector2(layout.Width / 2f, y);

            await ctx.DrawLine(renderer.WorldToScreenPos(pos1), renderer.WorldToScreenPos(pos2), 
                gridLineWidth, gridStrokeStyle, []);
        }
    }
}
