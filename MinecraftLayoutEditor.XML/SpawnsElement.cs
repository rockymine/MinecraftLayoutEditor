using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

public class SpawnsElement
{
    [XmlElement("spawn")]
    public List<SpawnElement> Spawns = [];

    [XmlElement("default")]
    public DefaultSpawnElement Default { get; set; } = new();
}

public class SpawnElement
{
    [XmlAttribute("team")]
    public string Team { get; set; } = "";

    [XmlAttribute("kit")]
    public string Kit { get; set; } = "";

    [XmlAttribute("yaw")]
    public float Yaw { get; set; }

    [XmlAttribute("region")]
    public string Region { get; set; } = "";

    [XmlIgnore]
    public Region? RegionRef { get; set; }
}

public class DefaultSpawnElement
{
    [XmlAttribute("yaw")]
    public float Yaw { get; set; }

    [XmlAttribute("region")]
    public string Region { get; set; } = "";

    [XmlIgnore]
    public Region? RegionRef { get; set; }
}
