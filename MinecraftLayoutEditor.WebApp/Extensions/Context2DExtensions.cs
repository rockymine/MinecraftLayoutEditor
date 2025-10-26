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
        await ctx.ClosePathAsync();

        await ctx.SetLineDashAsync(defaultDash);
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }

    public static async Task DrawRect(this Context2D ctx, Vector2 origin, float width, float height, 
        float lineWidth, string strokeStyle, double[] lineDash, string? fillStyle = null)
    {
        await ctx.SaveAsync();

        var bottomLeft = new Vector2(origin.X, origin.Y + height);
        var bottomRight = new Vector2(origin.X + width, origin.Y + height);
        var topRight = new Vector2(origin.X + width, origin.Y);

        await ctx.BeginPathAsync();
        await ctx.MoveToAsync(origin.X, origin.Y);
        await ctx.LineToAsync(bottomLeft.X, bottomLeft.Y);
        await ctx.LineToAsync(bottomRight.X, bottomRight.Y);
        await ctx.LineToAsync(topRight.X, topRight.Y);
        await ctx.ClosePathAsync();

        await ctx.SetLineDashAsync(lineDash);
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);

        if (fillStyle != null)
        {
            await ctx.FillStyleAsync(fillStyle);
            await ctx.FillAsync(FillRule.NonZero);
        }

        await ctx.StrokeAsync();
        await ctx.RestoreAsync();
    }

    public static async Task DrawRect(this Context2D ctx, Vector2 pos1, Vector2 pos2, Vector2 pos3, Vector2 pos4,
        float lineWidth, string strokeStyle, double[] lineDash)
    {
        await ctx.SaveAsync();
        await ctx.BeginPathAsync();

        await ctx.MoveToAsync(pos1.X, pos1.Y);
        await ctx.LineToAsync(pos2.X, pos2.Y);
        await ctx.LineToAsync(pos3.X, pos3.Y);
        await ctx.LineToAsync(pos4.X, pos4.Y);
        await ctx.ClosePathAsync();

        await ctx.SetLineDashAsync(lineDash);
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);

        await ctx.StrokeAsync();
        await ctx.RestoreAsync();
    }

    public static async Task DrawDiamond(this Context2D ctx, Vector2 origin, float width, float height,
        float lineWidth, string strokeStyle, double[] lineDash, string? fillStyle = null)
    {
        await ctx.SaveAsync();

        var left = new Vector2(origin.X - width, origin.Y);
        var top = new Vector2(origin.X, origin.Y - height);
        var right = new Vector2(origin.X + width, origin.Y);
        var bottom = new Vector2(origin.X, origin.Y + height);

        await ctx.BeginPathAsync();
        await ctx.MoveToAsync(left.X, left.Y);
        await ctx.LineToAsync(top.X, top.Y);
        await ctx.LineToAsync(right.X, right.Y);
        await ctx.LineToAsync(bottom.X, bottom.Y);
        await ctx.ClosePathAsync();

        await ctx.SetLineDashAsync(lineDash);
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);

        if (fillStyle != null)
        {
            await ctx.FillStyleAsync(fillStyle);
            await ctx.FillAsync(FillRule.NonZero);
        }

        await ctx.StrokeAsync();
        await ctx.RestoreAsync();
    }

    public static async Task DrawCircle(this Context2D ctx, Vector2 pos, float radiusX, 
        float radiusY, float lineWidth, string fillStyle, string strokeStyle, FillRule fillRule, float scale)
    {
        await ctx.SaveAsync();

        await ctx.BeginPathAsync();
        await ctx.EllipseAsync(pos.X, pos.Y, radiusX * scale, radiusY  * scale, 0, 0, 2 * Math.PI);

        // Fill
        await ctx.FillStyleAsync(fillStyle);
        await ctx.FillAsync(fillRule);

        // Stroke
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }

    public static async Task FillPolygonAsync(this Context2D ctx, IEnumerable<Vector2> points, 
        string fillStyle, FillRule fillRule = FillRule.NonZero)
    {
        var pts = points as IList<Vector2> ?? points.ToList();
        if (pts.Count < 3) return; // not a polygon

        await ctx.SaveAsync();
        await ctx.BeginPathAsync();

        await ctx.MoveToAsync(pts[0].X, pts[0].Y);
        for (int i = 1; i < pts.Count; i++)
        {
            await ctx.LineToAsync(pts[i].X, pts[i].Y);
        }
        await ctx.ClosePathAsync();

        await ctx.FillStyleAsync(fillStyle);
        await ctx.FillAsync(fillRule);
        await ctx.RestoreAsync();
    }
}
