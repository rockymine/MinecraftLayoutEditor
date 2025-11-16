using MinecraftLayoutEditor.XML;

namespace MinecraftLayoutEditor.Logic.History;

public abstract class AddRegionAction : IHistoryAction
{
    protected readonly MapElement MapElement;
    protected Region? Region;

    protected AddRegionAction(MapElement mapElement)
    {
        MapElement = mapElement;
    }

    public virtual void Execute()
    {
        if (Region == null)
            return;

        MapElement.Regions.Items.Add(Region);
    }

    public virtual void Undo()
    {
        if (Region == null)
            return;

        MapElement.Regions.Items.Remove(Region);
    }
}
