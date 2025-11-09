using System.Numerics;

namespace MinecraftLayoutEditor.Logic;

public class SymmetryAxis
{
    public bool IsHorizontal { get; set; }
    public float Offset { get; set; }
    public float RotationDeg { get; set; }

    public Vector2 GetStartPointWorld(Map map)
    {
        if (IsHorizontal)
        {
            return new Vector2(-map.Width / 2f, 0);
        }
        else
        {
            return new Vector2(0, -map.Height / 2f);
        }
    }

    public Vector2 GetEndPointWorld(Map map)
    {
        if (IsHorizontal)
        {
            return new Vector2(map.Width / 2f, 0);
        }
        else
        {
            return new Vector2(0, map.Height / 2f);
        }
    }
}
