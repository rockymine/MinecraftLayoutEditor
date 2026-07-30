using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

/// <summary>
/// The cells covered by every edge's lane. This is the same kind of data as the
/// imported block layer - a large set of cells that changes only when the layout does
/// - so it is drawn through the same cached, tiled geometry rather than a second
/// implementation of it.
/// </summary>
public class EdgeBlocksRenderer : IRenderable, IDisposable
{
    private readonly BlockGeometry _geometry = new();
    private readonly List<Vector2> _blocks = [];
    private int _blocksBuiltAtRevision = -1;
    private int _blocksBuiltAtLaneWidth = -1;

    public void Render(RenderContext context)
    {
        if (!context.Options.ShowBlocksEnabled)
            return;

        var graph = context.Map.Graph;
        var laneWidth = context.Map.LaneWidth;

        // Keyed on the lane width as well as the graph: the width is a map setting the
        // graph revision knows nothing about, and it changes which cells a lane covers.
        if (_blocksBuiltAtRevision != graph.Revision || _blocksBuiltAtLaneWidth != laneWidth)
        {
            _blocks.Clear();
            foreach (var edge in graph.Edges)
                _blocks.AddRange(edge.BlocksFor(laneWidth));

            _blocksBuiltAtRevision = graph.Revision;
            _blocksBuiltAtLaneWidth = laneWidth;
        }

        // One number that moves whenever either input to the cell set does, so the
        // tiled geometry notices a lane-width change as well as a graph change.
        var cellRevision = HashCode.Combine(graph.Revision, laneWidth);

        // Cells are filled, not outlined. An outline stroked at one screen pixel is
        // wider than the cell itself once the map is zoomed out, so each cell painted
        // over its neighbours and the layer read as a hatch rather than as ground.
        var blockPaint = context.Cache.GetPaint(context.Options.CellFillStyle,
            SKPaintStyle.Fill, 1f, context.Viewport.Scale);

        _geometry.Draw(
            context.Surface!.Canvas,
            blockPaint,
            _blocks,
            cellRevision,
            context.Viewport.VisibleWorldRect(),
            context.LimitX,
            context.LimitY);
    }

    public void Dispose()
    {
        _geometry.Dispose();
        GC.SuppressFinalize(this);
    }
}
