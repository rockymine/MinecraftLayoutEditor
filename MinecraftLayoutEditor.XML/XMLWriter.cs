using System.Xml;
using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

public static class XMLWriter
{
    /// <summary>
    /// Writes a MapElement to an XML file.
    /// </summary>
    public static void WriteToFile(string path, MapElement map)
    {
        using var stream = File.Create(path);
        WriteToStream(stream, map);
    }

    /// <summary>
    /// Returns the XML string representation of a MapElement.
    /// </summary>
    public static string WriteToString(MapElement map)
    {
        var settings = GetDefaultSettings();
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");

        var serializer = new XmlSerializer(typeof(MapElement));
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, settings);

        serializer.Serialize(writer, map, ns);
        return sw.ToString();
    }

    /// <summary>
    /// Writes a MapElement to an existing stream.
    /// </summary>
    public static void WriteToStream(Stream stream, MapElement map)
    {
        var settings = GetDefaultSettings();
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");

        var serializer = new XmlSerializer(typeof(MapElement));
        using var writer = XmlWriter.Create(stream, settings);
        serializer.Serialize(writer, map, ns);
    }

    private static XmlWriterSettings GetDefaultSettings() => new()
    {
        Indent = true,
        OmitXmlDeclaration = false,
        NamespaceHandling = NamespaceHandling.OmitDuplicates
    };
}
