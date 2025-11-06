using SharpNBT;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Schematics;

public class BitReader
{
    private readonly long[] _data;
    private int _bitIndex = 0;

    public BitReader(LongArrayTag tag) => _data = tag.ToArray();

    public int Read(int bitCount)
    {
        int result = 0;
        for (int i = 0; i < bitCount; i++)
        {
            int longIndex = _bitIndex / 64;
            int bitOffset = _bitIndex % 64;
            if (longIndex >= _data.Length) return 0;

            result |= ((int)((_data[longIndex] >> bitOffset) & 1)) << i;
            _bitIndex++;
        }
        return result;
    }
}
