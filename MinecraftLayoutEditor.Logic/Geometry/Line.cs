using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MinecraftLayoutEditor.Logic.Geometry;

public static class Line
{
    // https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect/1968345#1968345
    public static (double distance, double t) PLineDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        var A = point.X - a.X;
        var B = point.Y - a.Y;
        var C = b.X - a.X;
        var D = b.Y - a.Y;

        var dotProduct = A * C + B * D;
        var len_sq = C * C + D * D;
        var t = -1f;

        if (len_sq != 0) //in case of 0 length line
            t = dotProduct / len_sq;

        var pointIntersection = new Vector2();

        if (t < 0)
        {
            pointIntersection = a;
        }
        else if (t > 1)
        {
            pointIntersection = b;
        }
        else
        {
            pointIntersection = new Vector2(a.X + t * C, a.Y + t * D);
        }

        var dx = point.X - pointIntersection.X;
        var dy = point.Y - pointIntersection.Y;
        return (Math.Sqrt(dx * dx + dy * dy), t);
    }
}
