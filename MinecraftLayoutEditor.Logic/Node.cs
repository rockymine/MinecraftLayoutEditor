using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MinecraftLayoutEditor.Logic;

public class Node
{
    public Vector2 Position { get; set; }
    public NodeType Type { get; set; }
    public Node? MirrorRef { get; set; }
    public List<Edge> Edges { get; set; } = [];
    public Team? Team { get; set; }

    public Node(Vector2 position)
    {
        Position = position;
    }

    public Edge? EdgeTo(Node other)
    {
        foreach (var e in Edges)
        {
            if (e.Node1 == other || e.Node2 == other)
                return e;
        }

        return null;
    }

    public bool AxisAligned(Node other)
    {
        return Position.X == other.Position.X ||
           Position.Y == other.Position.Y;
    }

    public enum NodeType
    {
        Undefined,
        Wool,
        WoolEntry,
        Spawn,
        SpawnEntry,
        Frontline,
        Hub,
        Corridor
    }
}
