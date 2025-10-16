using System.Numerics;

namespace MinecraftLayoutEditor.Logic.Geometry
{
    public struct Rect
    {
        public float MinX;
        public float MinY;
        public float MaxX;
        public float MaxY;

        public Rect(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;
        }
    }
}
