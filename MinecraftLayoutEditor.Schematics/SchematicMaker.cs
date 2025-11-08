using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using System.Numerics;

namespace MinecraftLayoutEditor.Schematics;

public class SchematicMaker
{
    public static Schematic FromLayout(Layout layout, int scale = 1)
    {
        var schematic = new Schematic(layout.Name, (short)(layout.Width * scale), (short)layout.Thickness, 
            (short)(layout.Height * scale));

        AddEdgesToSchematic(schematic, layout, scale, schematic.Height, layout.LaneWidth);

        return schematic;
    }

    private static void AddEdgesToSchematic(Schematic schematic, Layout layout, int scale, int height, int laneWidth)
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
            // Use original world coordinates for rectangle calculation
            var blocksInside = Rectangle.DiscretePointsInsideRect(edge.Node1.Position, edge.Node2.Position, laneWidth);

            foreach (var block in blocksInside)
            {
                var (x, z) = GetSchematicPosition(block, layout.Width, layout.Height, scale);

                if (x < 0 || x >= schematic.Width || z < 0 || z >= schematic.Length)
                    continue;

                // TODO: Stack layers inside schematic
                for (int i = 0; i < height; i++)
                {
                    int blockId = 1;
                    if (i < 2)
                        blockId = 7;

                    schematic.SetBlock(x, i, z, (byte)blockId);
                }                
            }
        }

        // TODO: Find and fill missing corner polygons
    }

    private static (int x, int z) GetSchematicPosition(Vector2 position, int width, int height, int scale)
    {
        var scaledX = position.X * scale;
        var scaledZ = position.Y * scale;
        
        var x = (int)Math.Floor(scaledX + (width * scale) / 2);
        var z = (int)Math.Floor(scaledZ + (height * scale) / 2);
        return (x, z);
    }
}
