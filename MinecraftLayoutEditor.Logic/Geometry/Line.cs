using System.Numerics;

namespace MinecraftLayoutEditor.Logic.Geometry;

public static class Line
{
    // https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect/1968345#1968345
    public static (double distance, double t) PLineDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        var (distanceSquared, t) = PLineDistanceSquared(point, a, b);
        return (Math.Sqrt(distanceSquared), t);
    }

    /// <summary>
    /// The squared distance from the point to the segment, and how far along the segment
    /// the closest point lies. Callers comparing against a threshold should square the
    /// threshold and use this, so a sweep over many points pays no square roots.
    /// </summary>
    public static (float distanceSquared, float t) PLineDistanceSquared(Vector2 point, Vector2 a, Vector2 b)
    {
        var offsetX = point.X - a.X;
        var offsetY = point.Y - a.Y;
        var segmentX = b.X - a.X;
        var segmentY = b.Y - a.Y;

        var dotProduct = offsetX * segmentX + offsetY * segmentY;
        var segmentLengthSquared = segmentX * segmentX + segmentY * segmentY;
        var t = -1f;

        if (segmentLengthSquared != 0) //in case of 0 length line
            t = dotProduct / segmentLengthSquared;

        Vector2 pointIntersection;

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
            pointIntersection = new Vector2(a.X + t * segmentX, a.Y + t * segmentY);
        }

        var deltaX = point.X - pointIntersection.X;
        var deltaY = point.Y - pointIntersection.Y;
        return (deltaX * deltaX + deltaY * deltaY, t);
    }
}
