using System.Numerics;

namespace MinecraftLayoutEditor.Logic;

public class SymmetryAxis
{
    public bool IsHorizontal { get; set; }
    public float Offset { get; set; }
    public float RotationDeg { get; set; }

    public Vector2 GetStartPointWorld(Layout layout)
    {
        if (IsHorizontal)
        {
            return new Vector2(-layout.Width / 2f, 0);
        }
        else
        {
            return new Vector2(0, -layout.Height / 2f);
        }
    }

    public Vector2 GetEndPointWorld(Layout layout)
    {
        if (IsHorizontal)
        {
            return new Vector2(layout.Width / 2f, 0);
        }
        else
        {
            return new Vector2(0, layout.Height / 2f);
        }
    }
}
