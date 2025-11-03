using Excubo.Blazor.Canvas.Contexts;
using Excubo.Blazor.Canvas;
using System.Numerics;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Extensions;

public static class SKCanvasExtensions
{
    //public void DrawDiamond(this SKCanvas canvas, Vector2 origin, float width, float height,
    //    float lineWidth, string strokeStyle, double[] lineDash, string? fillStyle = null)
    //{
    //    var left = new Vector2(origin.X - width, origin.Y);
    //    var top = new Vector2(origin.X, origin.Y - height);
    //    var right = new Vector2(origin.X + width, origin.Y);
    //    var bottom = new Vector2(origin.X, origin.Y + height);



    //    await ctx.BeginPathAsync();
    //    await ctx.MoveToAsync(left.X, left.Y);
    //    await ctx.LineToAsync(top.X, top.Y);
    //    await ctx.LineToAsync(right.X, right.Y);
    //    await ctx.LineToAsync(bottom.X, bottom.Y);
    //    await ctx.ClosePathAsync();

    //    await ctx.SetLineDashAsync(lineDash);
    //    await ctx.LineWidthAsync(lineWidth);
    //    await ctx.StrokeStyleAsync(strokeStyle);

    //    if (fillStyle != null)
    //    {
    //        await ctx.FillStyleAsync(fillStyle);
    //        await ctx.FillAsync(FillRule.NonZero);
    //    }

    //    await ctx.StrokeAsync();
    //    await ctx.RestoreAsync();
    //}
}
