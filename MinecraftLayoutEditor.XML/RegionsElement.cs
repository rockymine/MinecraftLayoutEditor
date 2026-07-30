using System.Globalization;
using System.Numerics;
using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

[XmlRoot("regions")]
public class RegionsElement
{
    /// <summary>
    /// Bumped whenever a region moves, so cached region geometry can tell that what it
    /// holds is out of date.
    /// </summary>
    [XmlIgnore]
    public int Revision { get; private set; }

    public void MarkChanged() => Revision++;

    /// <summary>
    /// The region under a point, or null.
    ///
    /// A canvas has no elements to attach a click to, so the shapes have to answer the
    /// question themselves. Iteration runs backwards through the list because that is
    /// the reverse of paint order: the region drawn last is the one on top, and the one
    /// on top is the one a click should land on.
    ///
    /// <paramref name="tolerance"/> is in world units and should be a screen distance
    /// divided by the current zoom, the same way stroke widths are - a fixed world
    /// tolerance makes small regions unclickable exactly when they are hardest to hit.
    /// </summary>
    public Region? Pick(Vector2 planePoint, float tolerance = 0f)
    {
        for (int index = Items.Count - 1; index >= 0; index--)
        {
            if (Items[index].Contains(planePoint, tolerance))
                return Items[index];
        }

        return null;
    }

    [XmlElement("rectangle", typeof(RectangleRegion))]
    [XmlElement("cylinder", typeof(CylinderRegion))]
    [XmlElement("sphere", typeof(SphereRegion))]
    [XmlElement("circle", typeof(CircleRegion))]
    [XmlElement("block", typeof(BlockRegion))]
    [XmlElement("point", typeof(PointRegion))]
    [XmlElement("union", typeof(UnionRegion))]
    [XmlElement("negative", typeof(NegativeRegion))]
    public List<Region> Items { get; set; } = [];
}

[XmlInclude(typeof(UnionRegion))]
[XmlInclude(typeof(NegativeRegion))]
[XmlInclude(typeof(RectangleRegion))]
[XmlInclude(typeof(CylinderRegion))]
[XmlInclude(typeof(SphereRegion))]
[XmlInclude(typeof(CircleRegion))]
[XmlInclude(typeof(BlockRegion))]
[XmlInclude(typeof(PointRegion))]
public abstract class Region
{
    [XmlAttribute("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// Whether the region covers a point on the plan, grown by <paramref name="tolerance"/>.
    /// Regions are spatial predicates to begin with, so this is what they already mean.
    /// </summary>
    public abstract bool Contains(Vector2 planePoint, float tolerance = 0f);

    /// <summary>
    /// Moves the region across the plan. The X and Z of the underlying coordinates
    /// change and any height stays put, because the canvas is a top-down view.
    /// </summary>
    public abstract void Translate(Vector2 planeOffset);

    protected static bool WithinRadius(Vector2 center, float radius, Vector2 planePoint,
        float tolerance)
    {
        var reach = radius + tolerance;
        return Vector2.DistanceSquared(center, planePoint) <= reach * reach;
    }
}

public class RectangleRegion : Region
{
    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        planePoint.X >= MathF.Min(Min.X, Max.X) - tolerance
        && planePoint.X <= MathF.Max(Min.X, Max.X) + tolerance
        && planePoint.Y >= MathF.Min(Min.Y, Max.Y) - tolerance
        && planePoint.Y <= MathF.Max(Min.Y, Max.Y) + tolerance;

    public override void Translate(Vector2 planeOffset)
    {
        Min += planeOffset;
        Max += planeOffset;
    }

    [XmlIgnore]
    public Vector2 Min { get; set; }

    [XmlAttribute("min")]
    public string MinString
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1}", Min.X, Min.Y);
        set
        {
            var parts = value.Split(',');
            Min = new Vector2(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture));
        }
    }

    [XmlIgnore]
    public Vector2 Max { get; set; }

    [XmlAttribute("max")]
    public string MaxString
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1}", Max.X, Max.Y);
        set
        {
            var parts = value.Split(',');
            Max = new Vector2(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture));
        }
    }
}

public class CylinderRegion : Region
{
    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        WithinRadius(new Vector2(Base.X, Base.Z), Radius, planePoint, tolerance);

    public override void Translate(Vector2 planeOffset) =>
        Base = new Vector3(Base.X + planeOffset.X, Base.Y, Base.Z + planeOffset.Y);

    [XmlIgnore]
    public Vector3 Base { get; set; }

    [XmlAttribute("base")]
    public string BaseText
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Base.X, Base.Y, Base.Z);
        set
        {
            var parts = value.Split(",");
            Base = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }

    [XmlAttribute("radius")]
    public float Radius { get; set; }

    [XmlAttribute("height")]
    public float Height { get; set; }
}

public class SphereRegion : Region
{
    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        WithinRadius(new Vector2(Origin.X, Origin.Z), Radius, planePoint, tolerance);

    public override void Translate(Vector2 planeOffset) =>
        Origin = new Vector3(Origin.X + planeOffset.X, Origin.Y, Origin.Z + planeOffset.Y);

    [XmlIgnore]
    public Vector3 Origin { get; set; }

    [XmlAttribute("origin")]
    public string OriginText
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Origin.X, Origin.Y, Origin.Z);
        set
        {
            var parts = value.Split(",");
            Origin = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }

    [XmlAttribute("radius")]
    public float Radius { get; set; }
}

public class CircleRegion : Region
{
    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        WithinRadius(Center, Radius, planePoint, tolerance);

    public override void Translate(Vector2 planeOffset) => Center += planeOffset;

    [XmlIgnore]
    public Vector2 Center { get; set; }

    [XmlAttribute("center")]
    public string CenterText
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1}", Center.X, Center.Y);
        set
        {
            var parts = value.Split(',');
            Center = new Vector2(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture));
        }
    }

    [XmlAttribute("radius")]
    public float Radius { get; set; }
}

public class BlockRegion : Region
{
    public override bool Contains(Vector2 planePoint, float tolerance = 0f)
    {
        var cellX = MathF.Floor(Block.X);
        var cellZ = MathF.Floor(Block.Z);

        return planePoint.X >= cellX - tolerance && planePoint.X <= cellX + 1 + tolerance
            && planePoint.Y >= cellZ - tolerance && planePoint.Y <= cellZ + 1 + tolerance;
    }

    public override void Translate(Vector2 planeOffset) =>
        Block = new Vector3(Block.X + planeOffset.X, Block.Y, Block.Z + planeOffset.Y);

    [XmlIgnore]
    public Vector3 Block { get; set; }

    [XmlText]
    public string BlockText
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Block.X, Block.Y, Block.Z);
        set
        {
            var parts = value.Split(",");
            Block = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }
}

public class PointRegion : Region
{
    private const float DrawnRadius = 0.5f;

    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        WithinRadius(new Vector2(Point.X, Point.Z), DrawnRadius, planePoint, tolerance);

    public override void Translate(Vector2 planeOffset) =>
        Point = new Vector3(Point.X + planeOffset.X, Point.Y, Point.Z + planeOffset.Y);

    [XmlIgnore]
    public Vector3 Point { get; set; }

    [XmlText]
    public string PointText
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Point.X, Point.Y, Point.Z);
        set
        {
            var parts = value.Split(",");
            Point = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }
}

public class UnionRegion : Region
{
    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        Children.Any(child => child.Contains(planePoint, tolerance));

    public override void Translate(Vector2 planeOffset)
    {
        foreach (var child in Children)
            child.Translate(planeOffset);
    }

    [XmlElement("rectangle", typeof(RectangleRegion))]
    [XmlElement("cylinder", typeof(CylinderRegion))]
    [XmlElement("sphere", typeof(SphereRegion))]
    [XmlElement("circle", typeof(CircleRegion))]
    [XmlElement("block", typeof(BlockRegion))]
    [XmlElement("point", typeof(PointRegion))]
    [XmlElement("union", typeof(UnionRegion))]
    [XmlElement("negative", typeof(NegativeRegion))]
    public List<Region> Children { get; set; } = [];
}

public class NegativeRegion : Region
{
    /// <summary>
    /// Covers what its children cover, which is the opposite of what a negative region
    /// means to the game. Picking follows what is on screen - the children are what got
    /// painted, so the children are what a click can land on.
    /// </summary>
    public override bool Contains(Vector2 planePoint, float tolerance = 0f) =>
        Children.Any(child => child.Contains(planePoint, tolerance));

    public override void Translate(Vector2 planeOffset)
    {
        foreach (var child in Children)
            child.Translate(planeOffset);
    }

    [XmlElement("rectangle", typeof(RectangleRegion))]
    [XmlElement("cylinder", typeof(CylinderRegion))]
    [XmlElement("sphere", typeof(SphereRegion))]
    [XmlElement("circle", typeof(CircleRegion))]
    [XmlElement("block", typeof(BlockRegion))]
    [XmlElement("point", typeof(PointRegion))]
    [XmlElement("union", typeof(UnionRegion))]
    [XmlElement("negative", typeof(NegativeRegion))]
    public List<Region> Children { get; set; } = [];
}
