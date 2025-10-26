using Excubo.Blazor.Canvas.Contexts;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class GridRenderer
{
    public async Task RenderAsync(Context2D ctx, int gridSpacing, float gridLineWidth, string gridStrokeStyle, 
        Logic.Layout layout, LayoutRenderer renderer)
    {
        await ctx.SaveAsync();
        await ctx.BeginPathAsync();

        var gridOrigin = new Vector2(-layout.Width / 2f, -layout.Height / 2f);

        // Add all vertical lines to the path
        for (float x = gridOrigin.X + gridSpacing; x < layout.Width / 2f; x += gridSpacing)
        {
            var pos1 = renderer.WorldToScreenPos(new Vector2(x, -layout.Height / 2f));
            var pos2 = renderer.WorldToScreenPos(new Vector2(x, layout.Height / 2f));

            await ctx.MoveToAsync(pos1.X, pos1.Y);
            await ctx.LineToAsync(pos2.X, pos2.Y);
        }

        // Add all horizontal lines to the same path
        for (float y = gridOrigin.Y; y < layout.Height / 2f; y += gridSpacing)
        {
            var pos1 = renderer.WorldToScreenPos(new Vector2(-layout.Width / 2f, y));
            var pos2 = renderer.WorldToScreenPos(new Vector2(layout.Width / 2f, y));

            await ctx.MoveToAsync(pos1.X, pos1.Y);
            await ctx.LineToAsync(pos2.X, pos2.Y);
        }

        await ctx.ClosePathAsync();

        await ctx.LineWidthAsync(gridLineWidth);
        await ctx.StrokeStyleAsync(gridStrokeStyle);
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }
}
