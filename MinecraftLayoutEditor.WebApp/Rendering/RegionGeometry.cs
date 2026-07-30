using MinecraftLayoutEditor.XML;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

/// <summary>
/// Cached draw batches for the map's regions, grouped by how they are drawn rather than
/// by what they are: outlined boxes, outlined circles, and filled markers.
///
/// Regions can be dragged, so unlike the imported block layer this batch genuinely
/// changes during use. It is keyed on <see cref="RegionsElement.Revision"/>, which a
/// move bumps - the same arrangement as the graph, and the reason a moved region does
/// not leave its old outline behind.
///
/// The hovered and selected regions are drawn again on top of the batch that already
/// contains them, so pointing at a region does not invalidate anything.
/// </summary>
public class RegionGeometry : IDisposable
{
    private SKPath? _boxOutlines;
    private SKPath? _circleOutlines;
    private SKPath? _markerFills;
    private int _builtAtRevision = -1;

    public void Draw(RenderContext context, RegionsElement regions)
    {
        EnsureBuilt(regions);

        var options = context.Options;
        var scale = context.Viewport.Scale;
        var canvas = context.Surface!.Canvas;

        canvas.DrawPath(_boxOutlines,
            context.Cache.GetPaint(options.RegionOutlineStroke, SKPaintStyle.Stroke, 1f, scale));
        canvas.DrawPath(_circleOutlines,
            context.Cache.GetPaint(options.RegionRadialStroke, SKPaintStyle.Stroke, 1f, scale));
        canvas.DrawPath(_markerFills,
            context.Cache.GetPaint(options.RegionMarkerFill, SKPaintStyle.Fill, 1f, scale));

        DrawHighlight(context, context.HoveredRegion, options.HoveredRegionStroke);
        DrawHighlight(context, context.SelectedRegion, options.SelectedRegionStroke);
    }

    private static void DrawHighlight(RenderContext context, Region? region, SKColor strokeColor)
    {
        if (region == null)
            return;

        using var outlines = new SKPath();
        using var circles = new SKPath();
        using var markers = new SKPath();
        AddRegion(region, outlines, circles, markers);

        // Two screen pixels wide, so a highlight reads as a highlight at any zoom.
        var paint = context.Cache.GetPaint(strokeColor, SKPaintStyle.Stroke, 2f,
            context.Viewport.Scale);

        context.Surface!.Canvas.DrawPath(outlines, paint);
        context.Surface!.Canvas.DrawPath(circles, paint);
        context.Surface!.Canvas.DrawPath(markers, paint);
    }

    private void EnsureBuilt(RegionsElement regions)
    {
        if (regions.Revision == _builtAtRevision && _boxOutlines != null)
            return;

        DisposePaths();

        _boxOutlines = new SKPath();
        _circleOutlines = new SKPath();
        _markerFills = new SKPath();

        foreach (var region in regions.Items)
            AddRegion(region, _boxOutlines, _circleOutlines, _markerFills);

        _builtAtRevision = regions.Revision;
    }

    /// <summary>
    /// Adds a region's plan-view geometry to whichever batch draws its kind. Compound
    /// regions contribute their children, which is what the renderer always did and what
    /// makes their children pickable.
    /// </summary>
    private static void AddRegion(Region region, SKPath boxOutlines, SKPath circleOutlines,
        SKPath markerFills)
    {
        switch (region)
        {
            case RectangleRegion rectangle:
                boxOutlines.AddRect(new SKRect(
                    MathF.Min(rectangle.Min.X, rectangle.Max.X),
                    MathF.Min(rectangle.Min.Y, rectangle.Max.Y),
                    MathF.Max(rectangle.Min.X, rectangle.Max.X),
                    MathF.Max(rectangle.Min.Y, rectangle.Max.Y)));
                break;

            case CircleRegion circle:
                circleOutlines.AddCircle(circle.Center.X, circle.Center.Y, circle.Radius);
                break;

            case CylinderRegion cylinder:
                circleOutlines.AddCircle(cylinder.Base.X, cylinder.Base.Z, cylinder.Radius);
                break;

            case SphereRegion sphere:
                circleOutlines.AddCircle(sphere.Origin.X, sphere.Origin.Z, sphere.Radius);
                break;

            case BlockRegion block:
                markerFills.AddRect(SKRect.Create(
                    MathF.Floor(block.Block.X), MathF.Floor(block.Block.Z), 1f, 1f));
                break;

            case PointRegion point:
                markerFills.AddCircle(point.Point.X, point.Point.Z, 0.5f);
                break;

            case UnionRegion union:
                foreach (var child in union.Children)
                    AddRegion(child, boxOutlines, circleOutlines, markerFills);
                break;

            case NegativeRegion negative:
                foreach (var child in negative.Children)
                    AddRegion(child, boxOutlines, circleOutlines, markerFills);
                break;
        }
    }

    private void DisposePaths()
    {
        _boxOutlines?.Dispose();
        _circleOutlines?.Dispose();
        _markerFills?.Dispose();

        _boxOutlines = null;
        _circleOutlines = null;
        _markerFills = null;
    }

    public void Dispose()
    {
        DisposePaths();
        GC.SuppressFinalize(this);
    }
}
