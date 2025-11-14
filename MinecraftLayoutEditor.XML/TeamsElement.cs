using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

public class TeamsElement
{
    [XmlElement("team")]
    public List<TeamsElement> Teams { get; set; } = [];
}

public class TeamElement
{
    [XmlAttribute("id")]
    public string Id { get; set; } = "";

    [XmlAttribute("color")]
    public string Color { get; set; } = "";

    [XmlAttribute("dye-color")]
    public string DyeColor { get; set; } = "";

    [XmlAttribute("max")]
    public int Max { get; set; }

    [XmlText]
    public string Name { get; set; } = "";
}
