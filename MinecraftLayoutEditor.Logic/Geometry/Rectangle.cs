using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MinecraftLayoutEditor.Logic.Geometry;

public static class Rectangle
{
    public static List<Vector2> DiscretePointsInsideRect(Vector2 a, Vector2 b, double width)
    {
        var corners = FindRectCorners(a, b, width);
        var rect = AxisAlignedBoundingBox(corners);

        var inside = new List<Vector2>();

        for (float x = float.Floor(rect.MinX) - 1; x <= float.Ceiling(rect.MaxX) + 1; x++)
        {
            for (float y = float.Floor(rect.MinY) - 1; y <= float.Ceiling(rect.MaxY) + 1; y++)
            {
                if (InsideRect(a, b, new Vector2(x + 0.5f, y + 0.5f), width)) {
                    inside.Add(new Vector2(x, y));
                }
            }
        }

        return inside;
    }
    
    public static bool InsideRect(Vector2 a, Vector2 b, Vector2 point, double width)
    {
        var corners = FindRectCorners(a, b, width);
        var rect = AxisAlignedBoundingBox(corners);

        if (!rect.Contains(point))
            return false;

        var (distance, t) = PLineDistance(point, a, b);
        
        return distance <= width / 2 && t >= 0 && t <= 1;
    }

    // TODO: move to class Line
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
    
    public static List<Vector2> FindRectCorners(Vector2 pointA, Vector2 pointB, double width)
    {
        var halfWidth = (float)width / 2;

        // Normalize vector AB: normAB has length 1
        var normAB = Vector2.Normalize(pointB - pointA);

        // Find normal of normAB: normal is perpendicular to normAB
        var normal = new Vector2(-normAB.Y, normAB.X);

        // Find missing points by moving in each direction
        var pointC = pointA + normal * halfWidth;
        var pointD = pointA - normal * halfWidth;
        var pointE = pointB + normal * halfWidth;
        var pointF = pointB - normal * halfWidth;

        return [pointC, pointD, pointE, pointF];
    }
    
    public static Rect AxisAlignedBoundingBox(List<Vector2> points)
    {
        var rect = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

        foreach(var p in points)
        {
            rect.MinX = Math.Min(p.X, rect.MinX);
            rect.MinY = Math.Min(p.Y, rect.MinY);
            rect.MaxX = Math.Max(p.X, rect.MaxX);
            rect.MaxY = Math.Max(p.Y, rect.MaxY);
        }

        return rect;
    }
}
