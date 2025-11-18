using MinecraftLayoutEditor.XML;
using System.Numerics;

namespace MinecraftLayoutEditor.Logic.History;

public class AddCircleRegionAction : AddRegionAction
{
    private readonly Vector2 _pos1;
    private readonly int _radius;

    public AddCircleRegionAction(MapElement mapElement, Vector2 pos1, int radius) : base(mapElement)
    {
        _pos1 = pos1;
        _radius = radius;
    }

    public override void Execute()
    {

        Region = new CircleRegion
        {
            Id = GenerateId("circle"),
            Center = _pos1,
            Radius = _radius
        };

        base.Execute();
    }

    private string GenerateId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
