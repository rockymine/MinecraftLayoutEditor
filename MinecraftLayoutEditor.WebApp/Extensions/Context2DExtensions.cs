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
        float lineWidth, string strokeStyle, double[] lineDash, string? fillStyle = null)
    {
        await ctx.SaveAsync();

        var bottomLeft = new Vector2(origin.X, origin.Y + height);
        var bottomRight = new Vector2(origin.X + width, origin.Y + height);
        var topRight = new Vector2(origin.X + width, origin.Y);

        await ctx.DrawLine(origin, bottomLeft, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(bottomLeft, bottomRight, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(bottomRight, topRight, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(topRight, origin, lineWidth, strokeStyle, lineDash);

        if (fillStyle != null)
        {
            await ctx.FillStyleAsync(fillStyle);
            await ctx.FillRectAsync(origin.X, origin.Y, width, height);
        }

        await ctx.RestoreAsync();
    }

    public static async Task DrawRect(this Context2D ctx, Vector2 pos1, Vector2 pos2, Vector2 pos3, Vector2 pos4,
        float lineWidth, string strokeStyle, double[] lineDash)
    {
        await ctx.SaveAsync();

        await ctx.DrawLine(pos1, pos2, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(pos2, pos3, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(pos3, pos4, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(pos4, pos1, lineWidth, strokeStyle, lineDash);

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

        await ctx.DrawLine(left, top, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(top, right, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(right, bottom, lineWidth, strokeStyle, lineDash);
        await ctx.DrawLine(bottom, left, lineWidth, strokeStyle, lineDash);

        List<Vector2> points = [ left, top, right, bottom ];

        if (fillStyle != null)
        {
            await ctx.FillStyleAsync(fillStyle);
            await ctx.FillPolygonAsync(points, fillStyle);
        }

        await ctx.RestoreAsync();
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

        // Stroke
        await ctx.LineWidthAsync(lineWidth);
        await ctx.StrokeStyleAsync(strokeStyle);
        await ctx.StrokeAsync();

        await ctx.RestoreAsync();
    }

    public static async Task FillPolygonAsync(this Context2D ctx, IEnumerable<Vector2> points, string fillStyle, FillRule fillRule = FillRule.NonZero)
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
