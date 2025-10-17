using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using System.Numerics;

namespace MinecraftLayoutEditor.Schematics;

public class SchematicMaker
{
    public static Schematic FromLayout(Layout layout, int height = 1, int scale = 1)
    {
        var schematic = new Schematic(layout.Name, (short)(layout.Width * scale), (short)height, 
            (short)(layout.Height * scale));

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
            // Use original world coordinates for rectangle calculation
            var blocksInside = Rectangle.DiscretePointsInsideRect(edge.Node1.Position, edge.Node2.Position, 4);

            foreach (var block in blocksInside)
            {
                // Transform each resulting block position to schematic coordinates
                var (x, z) = GetSchematicPosition(block, layout.Width, layout.Height, scale);
                schematic.SetBlock(x, 0, z, 2);
            }
        }

        // TODO: Find and fill missing corner polygons
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
}
