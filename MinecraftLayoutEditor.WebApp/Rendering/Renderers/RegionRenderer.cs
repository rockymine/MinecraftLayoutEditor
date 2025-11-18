using MinecraftLayoutEditor.XML;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class RegionRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        if (context.MapElement == null)
            return;

        foreach (var region in context.MapElement.Regions.Items)
        {
            RenderRegion(context, region);
        }
    }

    private void RenderRegion(RenderContext context, Region region)
    {
        switch (region)
        {
            case RectangleRegion rectangleRegion:
                RenderRectangleRegion(context, rectangleRegion);
                break;
            case CircleRegion circleRegion:
                RenderCircleRegion(context, circleRegion);
                break;
            case CylinderRegion cylinderRegion:
                RenderCylinderRegion(context, cylinderRegion);
                break;
            case BlockRegion blockRegion:
                RenderBlockRegion(context, blockRegion);
                break;
            case PointRegion pointRegion:
                RenderPointRegion(context, pointRegion);
                break;
            case UnionRegion unionRegion:                
                RenderUnionRegion(context, unionRegion);
                break;
            case NegativeRegion negativeRegion:               
                RenderNegativeRegion(context, negativeRegion);
                break;
        }
    }

    private void RenderUnionRegion(RenderContext context, UnionRegion region)
    {
        foreach (var childRegion in region.Children)
        {
            RenderRegion(context, childRegion);
        }
    }

    private void RenderNegativeRegion(RenderContext context, NegativeRegion region)
    {
        foreach (var childRegion in region.Children)
        {
            RenderRegion(context, childRegion);
        }
    }

    private void RenderRectangleRegion(RenderContext context, RectangleRegion region)
    {
        var paint = context.Cache.GetPaint(SKColors.Purple, SKPaintStyle.Stroke, 1f, context.Viewport.Scale);
        var hoverPaint = context.Cache.GetPaint(SKColors.Yellow.WithAlpha(100), SKPaintStyle.StrokeAndFill, 1f, context.Viewport.Scale);

        var width = region.Max.X - region.Min.X;
        var height = region.Max.Y - region.Min.Y;

        var rect = SKRect.Create(
            region.Min.X,
            region.Min.Y,
            width,
            height
        );

        var finalPaint = (context.HoveredRegion == region) ? hoverPaint : paint;
        context.Surface.Canvas.DrawRect(rect, finalPaint);
    }

    private void RenderCircleRegion(RenderContext context, CircleRegion region)
    {
        var paint = context.Cache.GetPaint(SKColors.Blue, SKPaintStyle.Stroke, 1f, context.Viewport.Scale);
        var hoverPaint = context.Cache.GetPaint(SKColors.Yellow.WithAlpha(100), SKPaintStyle.StrokeAndFill, 1f, context.Viewport.Scale);

        var finalPaint = (context.HoveredRegion == region) ? hoverPaint : paint;
        context.Surface.Canvas.DrawCircle(region.Center.X, region.Center.Y, region.Radius, finalPaint);
    }

    private void RenderCylinderRegion(RenderContext context, CylinderRegion region)
    {
        var paint = context.Cache.GetPaint(SKColors.Blue, SKPaintStyle.Stroke, 1f, context.Viewport.Scale);
        context.Surface.Canvas.DrawCircle(region.Base.X, region.Base.Z, region.Radius, paint);
    }

    private void RenderPointRegion(RenderContext context, PointRegion region)
    {
        var paint = context.Cache.GetPaint(SKColors.Blue, SKPaintStyle.Fill, 0.5f, context.Viewport.Scale);
        context.Surface.Canvas.DrawCircle(region.Point.X, region.Point.Z, 0.5f, paint);
    }

    private void RenderBlockRegion(RenderContext context, BlockRegion region)
    {
        var paint = context.Cache.GetPaint(SKColors.Blue, SKPaintStyle.Fill, 1f, context.Viewport.Scale);
        var blockRect = SKRect.Create(region.Block.X, region.Block.Z, 1f, 1f);
        context.Surface.Canvas.DrawRect(blockRect, paint);
    }
}
