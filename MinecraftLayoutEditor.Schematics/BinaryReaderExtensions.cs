using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Schematics;

public static class BinaryReaderExtensions
{
    public static int ReadInt32BigEndian(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}