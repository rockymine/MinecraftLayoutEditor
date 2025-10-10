using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MinecraftLayoutEditor.Logic;

public class Edge
{
    public Node Node1 { get; set; }
    public Node Node2 { get; set; }
    public EdgeType Type { get; set; }
    public double Distance => Vector2.Distance(Node1.Position, Node2.Position);

    public Edge(Node node1, Node node2)
    {
        Node1 = node1;
        Node2 = node2;
    }

    public enum EdgeType
    {
        Walkable,
        Bridgeable
    }
}
