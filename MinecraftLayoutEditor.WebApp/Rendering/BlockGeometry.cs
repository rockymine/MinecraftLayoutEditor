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
/// The paths hold one rect per cell, which is what the cell appearance depends on -
/// merging adjacent cells would change how the block layer looks.
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

        var pathsByTile = new Dictionary<(int, int), SKPath>();

        foreach (var block in blocks)
        {
            // The map bounds clamp the block layer, matching what the map can hold.
            if (Math.Abs(block.X + 0.5f) > limitX || Math.Abs(block.Y + 0.5f) > limitY)
                continue;

            var tileKey = (
                (int)MathF.Floor(block.X / TileSize),
                (int)MathF.Floor(block.Y / TileSize));

            if (!pathsByTile.TryGetValue(tileKey, out var path))
            {
                path = new SKPath { FillType = SKPathFillType.Winding };
                pathsByTile.Add(tileKey, path);
            }

            path.AddRect(SKRect.Create(block.X, block.Y, 1f, 1f));
        }

        foreach (var ((tileX, tileY), path) in pathsByTile)
        {
            var bounds = SKRect.Create(tileX * TileSize, tileY * TileSize, TileSize, TileSize);
            _tiles.Add(new Tile(bounds, path));
        }

        _sourceRevision = revision;
        _sourceCount = blocks.Count;
        _limitX = limitX;
        _limitY = limitY;
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
