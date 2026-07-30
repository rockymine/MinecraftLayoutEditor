using MinecraftLayoutEditor.XML;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Interaction;

/// <summary>
/// Dragging a region across the plan.
///
/// The whole gesture is expressed in world coordinates, which is what makes it simple:
/// the pointer is converted once on the way in, and from then on the offset means blocks
/// rather than pixels, so it is unaffected by zoom and pan and can be snapped to whole
/// blocks by rounding.
///
/// The region is moved as the pointer moves, so what is on screen is the result. The
/// offset applied so far is tracked separately, so that on release the live movement can
/// be handed to the history stack as a single action rather than replayed as many.
/// </summary>
public class RegionDrag
{
    private Region? _region;
    private Vector2 _startWorldPosition;
    private Vector2 _appliedOffset;

    public bool InProgress => _region != null;

    /// <summary>The region being dragged, for as long as one is.</summary>
    public Region? Region => _region;

    public void Begin(Region region, Vector2 worldPosition)
    {
        _region = region;
        _startWorldPosition = worldPosition;
        _appliedOffset = Vector2.Zero;
    }

    /// <summary>
    /// Moves the region to follow the pointer, snapped to whole blocks. Returns whether
    /// anything actually moved, so the caller only repaints when it did.
    /// </summary>
    public bool MoveTo(Vector2 worldPosition, RegionsElement regions)
    {
        if (_region == null)
            return false;

        // Snapping the offset rather than the position keeps a region's own fractional
        // coordinates intact - only the distance it travels is whole blocks.
        var desiredOffset = Round(worldPosition - _startWorldPosition);
        var step = desiredOffset - _appliedOffset;

        if (step == Vector2.Zero)
            return false;

        _region.Translate(step);
        _appliedOffset = desiredOffset;
        regions.MarkChanged();

        return true;
    }

    /// <summary>
    /// Ends the gesture and returns the action that represents it, or null if the region
    /// never actually moved. The live movement is rewound first, so that executing the
    /// returned action reapplies it exactly once and undo has something to reverse.
    /// </summary>
    public MoveRegionAction? Commit(RegionsElement regions)
    {
        if (_region == null)
            return null;

        var region = _region;
        var offset = _appliedOffset;
        _region = null;
        _appliedOffset = Vector2.Zero;

        if (offset == Vector2.Zero)
            return null;

        region.Translate(-offset);
        return new MoveRegionAction(regions, region, offset);
    }

    public void Cancel()
    {
        _region = null;
        _appliedOffset = Vector2.Zero;
    }

    private static Vector2 Round(Vector2 value) =>
        new(MathF.Round(value.X), MathF.Round(value.Y));
}
