using Excubo.Blazor.Canvas.Contexts;
using Excubo.Blazor.Canvas;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Extensions;

public static class Context2DExtensions
{
    public static async Task DrawLine(this Context2D ctx, Vector2 pos1, Vector2 pos2, 
        double lineWidth, string strokeStyle, double[] defaultDash)
    {
        await ctx.SaveAsync();

        await ctx.BeginPathAsync();
        await ctx.MoveToAsync(pos1.X, pos1.Y);
        await ctx.LineToAsync(pos2.X, pos2.Y);

        await ctx.SetLineDashAsync(defaultDash);
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }

    public static async Task DrawCircle(this Context2D ctx, Vector2 pos, double radiusX, 
        double radiusY, double lineWidth, string fillStyle, string StrokeStyle, FillRule fillRule)
    {
        await ctx.SaveAsync();

        await ctx.BeginPathAsync();
        await ctx.EllipseAsync(pos.X, pos.Y, radiusX, radiusY, 0, 0, 2 * Math.PI);

        // Fill
        await ctx.FillStyleAsync(fillStyle);
        await ctx.FillAsync(fillRule);

        //Stroke
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync("black");
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }
}
