using MinecraftLayoutEditor.Logic.History;
using MinecraftLayoutEditor.XML;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Interaction;

/// <summary>
/// One undo entry for one drag, holding the total offset rather than the many small
/// steps the pointer produced. A drag that emitted an action per mouse move would fill
/// the history with a hundred entries for a single gesture.
/// </summary>
public class MoveRegionAction : IHistoryAction
{
    private readonly RegionsElement _regions;
    private readonly Region _region;
    private readonly Vector2 _offset;

    public MoveRegionAction(RegionsElement regions, Region region, Vector2 offset)
    {
        _regions = regions;
        _region = region;
        _offset = offset;
    }

    public void Execute() => Apply(_offset);

    public void Undo() => Apply(-_offset);

    private void Apply(Vector2 offset)
    {
        _region.Translate(offset);
        _regions.MarkChanged();
    }
}
