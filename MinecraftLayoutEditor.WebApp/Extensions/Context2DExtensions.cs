using Excubo.Blazor.Canvas.Contexts;
using Excubo.Blazor.Canvas;
using System.Numerics;
using MinecraftLayoutEditor.WebApp.Rendering;

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

    private static (int gx, int gz) ToGridCell(Vector2 screenPos, Vector2 originPx, float cellSizePx)
    {
        var gx = (int)Math.Floor((screenPos.X - originPx.X) / cellSizePx);
        var gz = (int)Math.Floor((screenPos.Y - originPx.Y) / cellSizePx);
        return (gx, gz);
    }

    public static async Task FillCellsAlongBresenhamLine(this Context2D ctx, Vector2 screenPos1, Vector2 screenPos2, Vector2 originPx, float cellSizePx, string fillStyle)
    {
        await ctx.SaveAsync();
        await ctx.FillStyleAsync(fillStyle);

        // 1) Map endpoints from pixels -> grid cell indices
        var (gx0, gz0) = ToGridCell(screenPos1, originPx, cellSizePx);
        var (gx1, gz1) = ToGridCell(screenPos2, originPx, cellSizePx);

        // 2) Run Bresenham on CELL coordinates (not pixels!)
        foreach (var (gx, gz) in GridRenderer.BresenhamLine(gx0, gz0, gx1, gz1))
        {
            // 3) Map cell -> pixel rect and fill
            double px = originPx.X + gx * cellSizePx;
            double py = originPx.Y + gz * cellSizePx;
            await ctx.FillRectAsync(px, py, cellSizePx, cellSizePx);
        }

        await ctx.RestoreAsync(); // you were missing this
    }
}
