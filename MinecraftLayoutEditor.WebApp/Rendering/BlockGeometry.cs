using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

/// <summary>
/// Cached, spatially partitioned cell geometry for a block set.
///
/// An imported world contributes tens of thousands of cells that do not move, so the
/// path describing them is built once and kept. It is split into square tiles of
/// <see cref="TileSize"/> world units so a frame can draw only the tiles that
/// intersect the viewport instead of submitting the whole set every time.
///
/// Within a tile, cells that sit side by side on the same row are merged into one
/// rectangle. Filled cells that touch cover exactly the same pixels as the single
/// rectangle spanning them, so this changes nothing about the result while cutting
/// the number of rectangles a solid area needs by a large factor.
/// </summary>
public class BlockGeometry : IDisposable
{
    private const float TileSize = 32f;

    private readonly List<Tile> _tiles = [];
    private int _sourceRevision = -1;
    private int _sourceCount = -1;
    private int _limitX = -1;
    private int _limitY = -1;

    /// <summary>Tiles drawn during the most recent <see cref="Draw"/> call.</summary>
    public int DrawnTiles { get; private set; }

    /// <summary>Rectangles across all tiles after merging, for diagnostics.</summary>
    public int MergedRectangles { get; private set; }

    /// <summary>
    /// Rebuilds the cached paths when the block set or the map bounds have changed,
    /// then draws the tiles overlapping <paramref name="visibleWorldRect"/>.
    /// </summary>
    public void Draw(SKCanvas canvas, SKPaint paint, IReadOnlyList<Vector2> blocks,
        int revision, SKRect visibleWorldRect, int limitX, int limitY)
    {
        EnsureBuilt(blocks, revision, limitX, limitY);

        DrawnTiles = 0;
        foreach (var tile in _tiles)
        {
            if (!tile.Bounds.IntersectsWith(visibleWorldRect))
                continue;

            canvas.DrawPath(tile.Path, paint);
            DrawnTiles++;
        }
    }

    private void EnsureBuilt(IReadOnlyList<Vector2> blocks, int revision, int limitX, int limitY)
    {
        if (revision == _sourceRevision && blocks.Count == _sourceCount
            && limitX == _limitX && limitY == _limitY)
            return;

        DisposeTiles();
        MergedRectangles = 0;

        var cellsByTile = new Dictionary<(int, int), List<(int X, int Y)>>();

        foreach (var block in blocks)
        {
            // The map bounds clamp the block layer, matching what the map can hold.
            if (Math.Abs(block.X + 0.5f) > limitX || Math.Abs(block.Y + 0.5f) > limitY)
                continue;

            var cellX = (int)MathF.Floor(block.X);
            var cellY = (int)MathF.Floor(block.Y);

            var tileKey = (
                (int)MathF.Floor(cellX / TileSize),
                (int)MathF.Floor(cellY / TileSize));

            if (!cellsByTile.TryGetValue(tileKey, out var cells))
            {
                cells = [];
                cellsByTile.Add(tileKey, cells);
            }

            cells.Add((cellX, cellY));
        }

        foreach (var ((tileX, tileY), cells) in cellsByTile)
        {
            var path = new SKPath { FillType = SKPathFillType.Winding };
            MergedRectangles += AddMergedRows(path, cells);

            var bounds = SKRect.Create(tileX * TileSize, tileY * TileSize, TileSize, TileSize);
            _tiles.Add(new Tile(bounds, path));
        }

        _sourceRevision = revision;
        _sourceCount = blocks.Count;
        _limitX = limitX;
        _limitY = limitY;
    }

    /// <summary>
    /// Adds one rectangle per horizontal run of touching cells, and returns how many
    /// were added. Sorting by row then column puts the cells of a run next to each
    /// other, so a single pass finds them. Repeated cells - two edges can cover the
    /// same one - fall inside the run they repeat and add nothing.
    /// </summary>
    private static int AddMergedRows(SKPath path, List<(int X, int Y)> cells)
    {
        cells.Sort(static (first, second) =>
            first.Y != second.Y ? first.Y.CompareTo(second.Y) : first.X.CompareTo(second.X));

        var rectangles = 0;
        var index = 0;

        while (index < cells.Count)
        {
            var (runStartX, runY) = cells[index];
            var runEndX = runStartX;
            index++;

            while (index < cells.Count
                && cells[index].Y == runY
                && cells[index].X <= runEndX + 1)
            {
                runEndX = Math.Max(runEndX, cells[index].X);
                index++;
            }

            path.AddRect(SKRect.Create(runStartX, runY, runEndX - runStartX + 1, 1f));
            rectangles++;
        }

        return rectangles;
    }

    private void DisposeTiles()
    {
        foreach (var tile in _tiles)
            tile.Path.Dispose();

        _tiles.Clear();
    }

    public void Dispose()
    {
        DisposeTiles();
        GC.SuppressFinalize(this);
    }

    private sealed record Tile(SKRect Bounds, SKPath Path);
}
