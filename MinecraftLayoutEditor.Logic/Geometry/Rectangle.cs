using System.Numerics;

namespace MinecraftLayoutEditor.Logic.Geometry;

/// <summary>
/// The four corners of an oriented rectangle. A struct rather than a list, because
/// callers ask for these per edge per frame and per cell when rasterising a lane.
/// </summary>
public readonly record struct RectCorners(
    Vector2 NearLeft, Vector2 NearRight, Vector2 FarLeft, Vector2 FarRight)
{
    public Rect BoundingBox()
    {
        var minX = MathF.Min(MathF.Min(NearLeft.X, NearRight.X), MathF.Min(FarLeft.X, FarRight.X));
        var minY = MathF.Min(MathF.Min(NearLeft.Y, NearRight.Y), MathF.Min(FarLeft.Y, FarRight.Y));
        var maxX = MathF.Max(MathF.Max(NearLeft.X, NearRight.X), MathF.Max(FarLeft.X, FarRight.X));
        var maxY = MathF.Max(MathF.Max(NearLeft.Y, NearRight.Y), MathF.Max(FarLeft.Y, FarRight.Y));

        return new Rect(minX, minY, maxX, maxY);
    }
}

public static class Rectangle
{
    /// <summary>
    /// The cells whose centre falls inside the lane between <paramref name="a"/> and
    /// <paramref name="b"/>.
    ///
    /// The lane's corners and bounding box do not depend on the cell being tested, so
    /// they are computed once before the sweep rather than once per cell.
    /// </summary>
    public static List<Vector2> DiscretePointsInsideRect(Vector2 a, Vector2 b, double width)
    {
        var bounds = FindRectCorners(a, b, width).BoundingBox();
        var halfWidthSquared = (float)(width * width / 4);

        var inside = new List<Vector2>();

        for (float x = float.Floor(bounds.MinX) - 1; x <= float.Ceiling(bounds.MaxX) + 1; x++)
        {
            for (float y = float.Floor(bounds.MinY) - 1; y <= float.Ceiling(bounds.MaxY) + 1; y++)
            {
                var center = new Vector2(x + 0.5f, y + 0.5f);

                if (!bounds.Contains(center))
                    continue;

                if (IsWithinLane(a, b, center, halfWidthSquared))
                    inside.Add(new Vector2(x, y));
            }
        }

        return inside;
    }

    public static bool InsideRect(Vector2 a, Vector2 b, Vector2 point, double width)
    {
        var bounds = FindRectCorners(a, b, width).BoundingBox();

        if (!bounds.Contains(point))
            return false;

        return IsWithinLane(a, b, point, (float)(width * width / 4));
    }

    /// <summary>
    /// Whether the point is within half the lane width of the segment, measured
    /// perpendicular to it and only along its length. Compares squared distances, so
    /// the sweep does not pay for a square root per cell.
    /// </summary>
    private static bool IsWithinLane(Vector2 a, Vector2 b, Vector2 point, float halfWidthSquared)
    {
        var (distanceSquared, alongSegment) = Line.PLineDistanceSquared(point, a, b);
        return distanceSquared <= halfWidthSquared && alongSegment >= 0 && alongSegment <= 1;
    }

    public static RectCorners FindRectCorners(Vector2 pointA, Vector2 pointB, double width)
    {
        var halfWidth = (float)width / 2;

        // Normalize vector AB: normAB has length 1
        var normAB = Vector2.Normalize(pointB - pointA);

        // Find normal of normAB: normal is perpendicular to normAB
        var normal = new Vector2(-normAB.Y, normAB.X);

        return new RectCorners(
            pointA + normal * halfWidth,
            pointA - normal * halfWidth,
            pointB + normal * halfWidth,
            pointB - normal * halfWidth);
    }
}
