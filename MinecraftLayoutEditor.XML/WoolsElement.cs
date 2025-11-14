using System.Globalization;
using System.Numerics;
using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

public class WoolsElement
{
    [XmlAttribute("craftable")]
    public bool Craftable { get; set; }

    [XmlElement("wool")]
    public List<WoolElement> Wools { get; set; } = [];
}

public class WoolElement
{
    [XmlAttribute("team")]
    public string Team { get; set; } = "";

    [XmlAttribute("color")]
    public string Color { get; set; } = "";

    [XmlAttribute("monument")]
    public string Monument { get; set; } = "";

    [XmlAttribute("location")]
    public string LocationString
    {
        get => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Location.X, Location.Y, Location.Z);
        set
        {
            var parts = value.Split(',');
            Location = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }

    [XmlIgnore]
    public Vector3 Location { get; set; }

    [XmlIgnore]
    public Region? MonumentRef { get; set; }
}
