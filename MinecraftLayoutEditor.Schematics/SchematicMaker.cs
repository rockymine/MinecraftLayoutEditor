using MinecraftLayoutEditor.Logic;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MinecraftLayoutEditor.Schematics;

public class SchematicMaker
{
    public static Schematic FromLayout(Layout layout, int height = 1, int scale = 1)
    {
        var schematic = new Schematic(layout.Name, (short)(layout.Width * scale), (short)height, (short)(layout.Height * scale));

        AddEdgesToSchematic(schematic, layout, scale, schematic.Height);
        AddNodesToSchematic(schematic, layout, scale, schematic.Height);

        return schematic;
    }

    private static void AddEdgesToSchematic(Schematic schematic, Layout layout, int scale, int height)
    {
        // Collect unique edges to avoid drawing each edge twice
        var uniqueEdges = new HashSet<Edge>();

        foreach (var node in layout.Graph.Nodes)
        {
            foreach (var edge in node.Edges)
            {
                uniqueEdges.Add(edge);
            }
        }

        // Draw each unique edge
        foreach (var edge in uniqueEdges)
        {
            var (x, z) = GetSchematicPosition(edge.Node1.Position, layout.Width, layout.Height, scale);
            var endPos = GetSchematicPosition(edge.Node2.Position, layout.Width, layout.Height, scale);

            DrawLine(schematic, x, z, endPos.x, endPos.z, blockId: 41, height);
        }
    }

    private static void AddNodesToSchematic(Schematic schematic, Layout layout, int scale, int height)
    {
        foreach (var node in layout.Graph.Nodes)
        {
            var (x, z) = GetSchematicPosition(node.Position, layout.Width, layout.Height, scale);
            schematic.SetBlock(x, height - 1, z, 7);
        }
    }

    private static (int x, int z) GetSchematicPosition(Vector2 position, int width, int height, int scale)
    {
        var scaledX = position.X * scale;
        var scaledZ = position.Y * scale;
        
        var x = (int)Math.Floor(scaledX + (width * scale) / 2);
        var z = (int)Math.Floor(scaledZ + (height * scale) / 2);
        return (x, z);
    }

    private static void DrawLine(Schematic schematic, int x0, int z0, int x1, int z1, short blockId, int height)
    {
        int dx = Math.Abs(x1 - x0);
        int dz = Math.Abs(z1 - z0);
        int sx = x0 < x1 ? 1 : -1;
        int sz = z0 < z1 ? 1 : -1;
        int err = dx - dz;

        while (true)
        {
            // Set block at current position (check bounds)
            if (x0 >= 0 && x0 < schematic.Width && z0 >= 0 && z0 < schematic.Length)
            {
                for (int i = 0; i < height; i++)
                {
                    schematic.SetBlock(x0, i, z0, 1);
                }
            }

            if (x0 == x1 && z0 == z1) break;

            int e2 = 2 * err;
            if (e2 > -dz)
            {
                err -= dz;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                z0 += sz;
            }
        }
    }
}
