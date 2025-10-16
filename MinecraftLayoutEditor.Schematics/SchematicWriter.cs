using System.Text;

namespace MinecraftLayoutEditor.Schematics;

public class SchematicWriter : IDisposable
{
    private BinaryWriter writer;

    public SchematicWriter(Stream stream)
    {
        writer = new BinaryWriter(stream);
    }

    private void WriteString(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteShort((short)bytes.Length);
        writer.Write(bytes);
    }

    private void WriteByte(byte value) => writer.Write(value);

    private void WriteShort(short value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    private void WriteInt(int value)
    {
        writer.Write((byte)(value >> 24));
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    private void WriteLong(long value)
    {
        WriteInt((int)(value >> 32));
        WriteInt((int)value);
    }

    private void WriteFloat(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private void WriteDouble(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    public void WriteNamedTag(string name, byte tagType, Action writePayload)
    {
        WriteByte(tagType);
        if (tagType != 0) // TAG_End has no name
        {
            WriteString(name);
            writePayload();
        }
    }

    public void WriteTagEnd()
    {
        WriteByte(0);
        writer.Flush();
    }

    public void WriteTagByte(string name, byte value)
    {
        WriteNamedTag(name, 1, () => WriteByte(value));
    }

    public void WriteTagShort(string name, short value)
    {
        WriteNamedTag(name, 2, () => WriteShort(value));
    }

    public void WriteTagInt(string name, int value)
    {
        WriteNamedTag(name, 3, () => WriteInt(value));
    }

    public void WriteTagLong(string name, long value)
    {
        WriteNamedTag(name, 4, () => WriteLong(value));
    }

    public void WriteTagFloat(string name, float value)
    {
        WriteNamedTag(name, 5, () => WriteFloat(value));
    }

    public void WriteTagDouble(string name, double value)
    {
        WriteNamedTag(name, 6, () => WriteDouble(value));
    }

    public void WriteTagByteArray(string name, byte[] value)
    {
        WriteNamedTag(name, 7, () =>
        {
            WriteInt(value.Length);
            writer.Write(value);
        });
    }

    public void WriteTagString(string name, string value)
    {
        WriteNamedTag(name, 8, () => WriteString(value));
    }

    public void WriteTagList(string name, byte elementType, int count, Action writeElements)
    {
        WriteNamedTag(name, 9, () =>
        {
            WriteByte(elementType);
            WriteInt(count);
            writeElements();
        });
    }

    public void WriteTagCompoundStart(string name)
    {
        WriteNamedTag(name, 10, () => { });
    }

    public void Dispose()
    {
        writer?.Dispose();
    }
}
