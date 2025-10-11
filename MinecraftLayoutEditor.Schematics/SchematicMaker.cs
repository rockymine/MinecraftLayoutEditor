using MinecraftLayoutEditor.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Schematics;

public class SchematicMaker
{
    public static Schematic FromLayout(Layout layout)
    {
        var schematic = new Schematic(layout.Name, (short)layout.Width, (short)layout.Height);

        // Add blocks for node position
        foreach (var n in layout.Graph.Nodes)
        {
            var xPos = n.Position.X + layout.Width / 2;
            var yPos = n.Position.Y + layout.Height / 2;
            schematic.SetBlock((int)Math.Floor(xPos), 0, (int)Math.Floor(yPos), 1);
        }

        return schematic;
    }
}
