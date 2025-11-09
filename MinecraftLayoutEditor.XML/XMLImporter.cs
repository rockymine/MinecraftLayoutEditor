using System.Xml.Serialization;

namespace MinecraftLayoutEditor.XML;

public static class XMLImporter
{
    public static async Task<MapElement> LoadFromStream(Stream stream)
    {
        // Read the entire stream into memory first
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        // Now deserialize from the memory stream (which supports sync reads)
        var serializer = new XmlSerializer(typeof(MapElement));
        var map = (MapElement)serializer.Deserialize(memoryStream)!;
        return map;
    }
}
