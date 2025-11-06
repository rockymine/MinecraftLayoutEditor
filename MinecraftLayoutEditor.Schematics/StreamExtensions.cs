using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Schematics;

public static class StreamExtensions
{
    public static async Task ReadExactlyAsyncSelf(this Stream stream, byte[] buffer, int offset, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = await stream.ReadAsync(buffer, offset + total, count - total);
            if (read == 0) throw new EndOfStreamException();
            total += read;
        }
    }
}
