using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

[XmlRoot("regions")]
public class RegionsElement
{
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

    public abstract bool Contains(Vector2 position);
}

public class RectangleRegion : Region
{
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

    public override bool Contains(Vector2 position)
    {
        return position.X >= Min.X && position.X <= Max.X &&
           position.Y >= Min.Y && position.Y <= Max.Y;
    }
}

public class CylinderRegion : Region
{
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

    public override bool Contains(Vector2 position)
    {
        return Vector2.Distance(new Vector2(Base.X, Base.Z), position) <= Radius;
    }
}

public class SphereRegion : Region
{
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

    public override bool Contains(Vector2 position)
    {
        return Vector2.Distance(new Vector2(Origin.X, Origin.Z), position) <= Radius;
    }
}

public class CircleRegion : Region
{
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

    public override bool Contains(Vector2 position)
    {
        return Vector2.Distance(Center, position) <= Radius;
    }
}

public class BlockRegion : Region
{
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

    public override bool Contains(Vector2 position)
    {
        return Vector2.Distance(new Vector2(Block.X, Block.Z), position) <= 0.5f;
    }
}

public class PointRegion : Region
{
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

    public override bool Contains(Vector2 position)
    {
        return Vector2.Distance(new Vector2(Point.X, Point.Z), position) <= 0.5f;
    }
}

public class UnionRegion : Region
{
    [XmlElement("rectangle", typeof(RectangleRegion))]
    [XmlElement("cylinder", typeof(CylinderRegion))]
    [XmlElement("sphere", typeof(SphereRegion))]
    [XmlElement("circle", typeof(CircleRegion))]
    [XmlElement("block", typeof(BlockRegion))]
    [XmlElement("point", typeof(PointRegion))]
    [XmlElement("union", typeof(UnionRegion))]
    [XmlElement("negative", typeof(NegativeRegion))]
    public List<Region> Children { get; set; } = [];

    // TODO
    public override bool Contains(Vector2 position)
    {
        return false;
    }
}

public class NegativeRegion : Region
{
    [XmlElement("rectangle", typeof(RectangleRegion))]
    [XmlElement("cylinder", typeof(CylinderRegion))]
    [XmlElement("sphere", typeof(SphereRegion))]
    [XmlElement("circle", typeof(CircleRegion))]
    [XmlElement("block", typeof(BlockRegion))]
    [XmlElement("point", typeof(PointRegion))]
    [XmlElement("union", typeof(UnionRegion))]
    [XmlElement("negative", typeof(NegativeRegion))]
    public List<Region> Children { get; set; } = [];

    // TODO
    public override bool Contains(Vector2 position)
    {
        return false;
    }
}
