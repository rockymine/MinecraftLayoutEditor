using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

public class AuthorsElement
{
    [XmlElement("author")]
    public List<AuthorElement> Authors { get; set; } = [];
}

public class AuthorElement
{
    [XmlAttribute("uuid")]
    public string Uuid { get; set; } = "";
}
