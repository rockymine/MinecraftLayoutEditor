using System.Globalization;
using System.Numerics;
using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

[XmlRoot("map")]
public class MapElement
{
    [XmlAttribute("proto")]
    public string Proto { get; set; } = "";

    [XmlElement("name")]
    public string Name { get; set; } = "";

    [XmlElement("version")]
    public string Version { get; set; } = "";

    [XmlElement("objective")]
    public string Objective { get; set; } = "";

    [XmlElement("gamemode")]
    public string Gamemode { get; set; } = "";

    [XmlElement("authors")]
    public AuthorsElement Authors { get; set; } = new();

    [XmlElement("teams")]
    public TeamsElement Teams { get; set; } = new();

    [XmlElement("spawns")]
    public SpawnsElement Spawns { get; set; } = new();

    [XmlElement("wools")]
    public WoolsElement Wools { get; set; } = new();

    [XmlElement("regions")]
    public RegionsElement Regions { get; set; } = new();

    [XmlElement("maxbuildheight")]
    public int MaxBuildHeight { get; set; }
}
