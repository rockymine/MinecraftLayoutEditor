using MinecraftLayoutEditor.Logic.Geometry;
using System.Numerics;

namespace MinecraftLayoutEditor.Logic;

public class Edge
{
    public Node Node1 { get; set; }
    public Node Node2 { get; set; }
    public EdgeType Type { get; set; }
    public double Distance => Vector2.Distance(Node1.Position, Node2.Position);

    private List<Vector2> _blocks = [];
    private Vector2 _blocksNode1;
    private Vector2 _blocksNode2;
    private double _blocksLaneWidth = double.NaN;

    public Edge(Node node1, Node node2)
    {
        Node1 = node1;
        Node2 = node2;
    }

    /// <summary>
    /// The cells this edge's lane covers at the given width.
    ///
    /// Derived on demand and remembered, keyed on the only two things it depends on:
    /// where the endpoints are and how wide the lane is. Computing it once and keeping
    /// it meant it went stale whenever either changed - a node nudged with the arrow
    /// keys, or the lane width edited - and left the caller responsible for noticing.
    /// </summary>
    public IReadOnlyList<Vector2> BlocksFor(double laneWidth)
    {
        if (laneWidth.Equals(_blocksLaneWidth)
            && Node1.Position == _blocksNode1
            && Node2.Position == _blocksNode2)
            return _blocks;

        _blocks = Rectangle.DiscretePointsInsideRect(Node1.Position, Node2.Position, laneWidth);
        _blocksNode1 = Node1.Position;
        _blocksNode2 = Node2.Position;
        _blocksLaneWidth = laneWidth;

        return _blocks;
    }

    public enum EdgeType
    {
        Walkable,
        Bridgeable
    }
}
