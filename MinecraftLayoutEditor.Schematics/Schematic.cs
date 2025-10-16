using System.IO.Compression;

namespace MinecraftLayoutEditor.Schematics;

public class Schematic
{
    public string Name { get; }
    public short Width { get; }
    public short Length { get; }
    public short Height { get; }
    public byte[,,] Blocks { get; }
    public string Materials { get; } = "Alpha";

    public Schematic(string name, short width, short height, short length)
    {
        Name = name;
        Width = width;
        Height = height;
        Length = length;
        Blocks = new byte[Width, Height, Length];
    }

    public void SetBlock(int x, int y, int z, byte blockID)
    {
        if (x < 0 || y < 0 || z < 0 || x >= Blocks.GetLength(0) || y >= Blocks.GetLength(1) || z >= Blocks.GetLength(2))
            throw new IndexOutOfRangeException($"Block outside of schematic: {x}, {y}, {z}");

        Blocks[x, y, z] = blockID;
    }

    public byte[] Save()
    {
        var dataStream = new MemoryStream();
        var schematicWriter = new SchematicWriter(dataStream);

        WriteSchematic(schematicWriter);

        var outStream = new MemoryStream();
        var compressionStream = new GZipStream(outStream, CompressionMode.Compress);
        var data = dataStream.ToArray();

        compressionStream.Write(data, 0, data.Length);
        compressionStream.Flush();
        compressionStream.Close();

        return outStream.ToArray();
    }

    public void WriteSchematic(SchematicWriter schematicWriter)
    {
        // Start root compound tag
        schematicWriter.WriteTagCompoundStart("Schematic");

        // Write dimensions
        schematicWriter.WriteTagShort("Width", Width);
        schematicWriter.WriteTagShort("Height", Height);
        schematicWriter.WriteTagShort("Length", Length);

        // Write materials
        schematicWriter.WriteTagString("Materials", Materials);

        // Prepare block data arrays
        int totalBlocks = Width * Height * Length;
        byte[] blockIds = new byte[totalBlocks];

        // Convert 3D array to 1D arrays
        // Index formula: (Y × Length + Z) × Width + X
        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Length; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = (y * Length + z) * Width + x;
                    byte block = Blocks[x, y, z];

                    blockIds[index] = block;
                }
            }
        }

        // Write block arrays
        schematicWriter.WriteTagByteArray("Blocks", blockIds);
        schematicWriter.WriteTagByteArray("Data", new byte[totalBlocks]);

        // End root compound
        schematicWriter.WriteTagEnd();
    }
}
