using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using System.Numerics;

namespace MinecraftLayoutEditor.Schematics;

public class SchematicMaker
{
    public static Schematic FromMap(Map map)
    {
        var schematic = new Schematic(map.Name, (short)(map.Width), (short)map.Thickness, 
            (short)(map.Height));

        AddEdgesToSchematic(schematic, map);

        return schematic;
    }

    private static void AddEdgesToSchematic(Schematic schematic, Map map)
    {
        // Collect unique edges to avoid drawing each edge twice
        var uniqueEdges = new HashSet<Edge>();

        foreach (var node in map.Graph.Nodes)
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
            var blocksInside = Rectangle.DiscretePointsInsideRect(edge.Node1.Position, edge.Node2.Position, map.LaneWidth);

            foreach (var block in blocksInside)
            {
                var (x, z) = GetSchematicPosition(block, map.Width, map.Height);

                if (x < 0 || x >= schematic.Width || z < 0 || z >= schematic.Length)
                    continue;

                // TODO: Stack layers inside schematic
                for (int i = 0; i < map.Thickness; i++)
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

    private static (int x, int z) GetSchematicPosition(Vector2 position, int width, int height)
    {  
        var x = (int)Math.Floor(position.X + width / 2);
        var z = (int)Math.Floor(position.Y + height / 2);
        return (x, z);
    }
}
