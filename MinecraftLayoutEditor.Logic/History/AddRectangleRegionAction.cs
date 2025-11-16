using MinecraftLayoutEditor.XML;
using System.Numerics;

namespace MinecraftLayoutEditor.Logic.History;

public class AddRectangleRegionAction : AddRegionAction
{
    private readonly Vector2 _pos1;
    private readonly Vector2 _pos2;

    public AddRectangleRegionAction(MapElement mapElement, Vector2 pos1, Vector2 pos2) : base(mapElement)
    {
        _pos1 = pos1;
        _pos2 = pos2;
    }

    public override void Execute()
    {
        var min = new Vector2(Math.Min(_pos1.X, _pos2.X), Math.Min(_pos1.Y, _pos2.Y));
        var max = new Vector2(Math.Max(_pos1.X, _pos2.X), Math.Max(_pos1.Y, _pos2.Y));

        Region = new RectangleRegion
        {
            Id = GenerateId("rect"),
            Min = min,
            Max = max
        };

        base.Execute();
    }

    private string GenerateId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
