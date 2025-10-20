using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MinecraftLayoutEditor.Logic.Geometry;

public static class Rotation
{
    public static Vector2 MirrorPosition(Vector2 pos, SymmetryAxis axis)
    {
        if (axis.RotationDeg != 0)
        {
            return RotateAboutOrigin(pos, Vector2.Zero, axis.RotationDeg * float.Pi / 180);
        }

        if (axis.IsHorizontal)
        {
            var isAboveAxis = pos.Y < axis.Offset;
            var distToAxis = Math.Abs(pos.Y - axis.Offset);

            return isAboveAxis ? new Vector2(pos.X, axis.Offset + distToAxis)
                : new Vector2(pos.X, axis.Offset - distToAxis);
        }
        else
        {
            var isLeftOfAxis = pos.X < axis.Offset;
            var distToAxis = Math.Abs(pos.X - axis.Offset);

            return isLeftOfAxis ? new Vector2(axis.Offset + distToAxis, pos.Y)
                : new Vector2(axis.Offset - distToAxis, pos.Y);
        }
    }

    public static Vector2 RotateAboutOrigin(Vector2 point, Vector2 origin, float rotation)
    {
        return Vector2.Transform(point - origin, Matrix3x2.CreateRotation(rotation)) + origin;
    }
}
