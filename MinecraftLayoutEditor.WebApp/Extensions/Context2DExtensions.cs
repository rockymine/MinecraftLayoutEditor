using Excubo.Blazor.Canvas.Contexts;
using Excubo.Blazor.Canvas;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Extensions;

public static class Context2DExtensions
{
    public static async Task DrawLine(this Context2D ctx, Vector2 pos1, Vector2 pos2, 
        float lineWidth, string strokeStyle, double[] defaultDash)
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

    public static async Task DrawRect(this Context2D ctx, Vector2 origin, float width, float height, 
        float lineWidth, string strokeStyle, double[] defaultDash)
    {
        await ctx.SaveAsync();

        await ctx.DrawLine(origin, new Vector2(origin.X, origin.Y + height), 
            lineWidth, strokeStyle, defaultDash);

        await ctx.DrawLine(new Vector2(origin.X, origin.Y + height), 
            new Vector2(origin.X + width, origin.Y + height),
            lineWidth, strokeStyle, defaultDash);

        await ctx.DrawLine(new Vector2(origin.X + width, origin.Y + height), 
            new Vector2(origin.X + width, origin.Y),
            lineWidth, strokeStyle, defaultDash);

        await ctx.DrawLine(new Vector2(origin.X + width, origin.Y), origin,
            lineWidth, strokeStyle, defaultDash);
    }

    public static async Task DrawCircle(this Context2D ctx, Vector2 pos, float radiusX, 
        float radiusY, float lineWidth, string fillStyle, string strokeStyle, FillRule fillRule)
    {
        await ctx.SaveAsync();

        await ctx.BeginPathAsync();
        await ctx.EllipseAsync(pos.X, pos.Y, radiusX, radiusY, 0, 0, 2 * Math.PI);

        // Fill
        await ctx.FillStyleAsync(fillStyle);
        await ctx.FillAsync(fillRule);

        //Stroke
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }
}
