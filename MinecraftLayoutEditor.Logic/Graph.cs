using System.Numerics;

namespace MinecraftLayoutEditor.Logic;

public class Graph
{
    private const float CellSize = 1f;

    private readonly List<Node> _nodes = [];
    private readonly HashSet<Edge> _edges = [];
    private readonly Dictionary<(int, int), List<Node>> _nodesByCell = [];
    private int _revision;
    private int _edgesBuiltAtRevision = -1;
    private int _cellsBuiltAtRevision = -1;

    public IReadOnlyList<Node> Nodes => _nodes;

    /// <summary>
    /// Incremented on every change to the graph, including node positions. Cached
    /// geometry compares against it to know whether what it holds is still current.
    /// </summary>
    public int Revision => _revision;

    /// <summary>
    /// The distinct edges of the graph. Rebuilt only after the graph changes: renderers
    /// read this every frame, so walking every node's edge list per read would put an
    /// allocation and a full graph traversal on the render path.
    /// </summary>
    public IReadOnlyCollection<Edge> Edges
    {
        get
        {
            if (_edgesBuiltAtRevision != _revision)
            {
                RebuildEdges();
                _edgesBuiltAtRevision = _revision;
            }

            return _edges;
        }
    }

    /// <summary>
    /// The nearest node within <paramref name="radius"/> of <paramref name="position"/>,
    /// or null when there is none.
    ///
    /// Hover detection asks this on every pointer move, so it cannot afford to measure
    /// every node: the nodes are bucketed into a grid of cells and only the cells the
    /// search radius touches are examined, which makes the cost independent of how large
    /// the layout is.
    /// </summary>
    public Node? FindNodeWithin(Vector2 position, float radius)
    {
        if (_nodes.Count == 0)
            return null;

        EnsureCellIndex();

        var minCellX = (int)MathF.Floor((position.X - radius) / CellSize);
        var maxCellX = (int)MathF.Floor((position.X + radius) / CellSize);
        var minCellY = (int)MathF.Floor((position.Y - radius) / CellSize);
        var maxCellY = (int)MathF.Floor((position.Y + radius) / CellSize);

        Node? closestNode = null;
        var closestDistanceSquared = radius * radius;

        for (int cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (int cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                if (!_nodesByCell.TryGetValue((cellX, cellY), out var nodesInCell))
                    continue;

                foreach (var node in nodesInCell)
                {
                    var distanceSquared = Vector2.DistanceSquared(position, node.Position);
                    if (distanceSquared <= closestDistanceSquared)
                    {
                        closestDistanceSquared = distanceSquared;
                        closestNode = node;
                    }
                }
            }
        }

        return closestNode;
    }

    private void EnsureCellIndex()
    {
        if (_cellsBuiltAtRevision == _revision)
            return;

        _nodesByCell.Clear();

        foreach (var node in _nodes)
        {
            var cellKey = (
                (int)MathF.Floor(node.Position.X / CellSize),
                (int)MathF.Floor(node.Position.Y / CellSize));

            if (!_nodesByCell.TryGetValue(cellKey, out var nodesInCell))
            {
                nodesInCell = [];
                _nodesByCell.Add(cellKey, nodesInCell);
            }

            nodesInCell.Add(node);
        }

        _cellsBuiltAtRevision = _revision;
    }

    /// <summary>
    /// The nearest node to <paramref name="pos"/> at any distance. Used by the click
    /// handlers, which run once per click and need no bound on the search.
    /// </summary>
    public Node? GetClosestNode(Vector2 pos)
    {
        if (_nodes.Count == 0)
            return null;

        Node? closestNode = null;
        var closestDistance = double.MaxValue;

        foreach (var n in _nodes)
        {
            var distance = Vector2.Distance(pos, n.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = n;
            } 
        }

        return closestNode;
    }

    private void RebuildEdges()
    {
        _edges.Clear();
        foreach (var node in _nodes)
        {
            foreach (var edge in node.Edges)
            {
                _edges.Add(edge);
            }
        }
    }

    /// <summary>
    /// Records that the graph changed. Call after mutating a node's edge list or a node
    /// position directly rather than through this class.
    /// </summary>
    public void MarkChanged()
    {
        _revision++;
    }

    public void Clear()
    {
        _nodes.Clear();
        _revision++;
    }

    public void AddNode(Node node)
    {
        _nodes.Add(node);
        _revision++;
    }

    public void DeleteNode(Node node, bool isMirror = false)
    {
        if (node.MirrorRef != null && !isMirror)
        {
            var mirrorRef = node.MirrorRef;
            DeleteNode(mirrorRef, true);
        }            
        
        foreach (var e in node.Edges)
        {
            if (e.Node1 == node)
            {
                e.Node2.Edges.Remove(e);
            }
            else if (e.Node2 == node)
            {
                e.Node1.Edges.Remove(e);
            }
        }

        _nodes.Remove(node);
        _revision++;
    }

    public void DeleteEdge(Edge edge)
    {
        edge.Node1.Edges.Remove(edge);
        edge.Node2.Edges.Remove(edge);
        _revision++;
    }

    public Edge? AddOrRemoveEdge(Node node1, Node node2, bool isMirror = false)
    {
        if (node1 == node2)
            throw new InvalidOperationException();

        if (node1.MirrorRef != null && node2.MirrorRef != null && !isMirror
            && node1.MirrorRef != node2)
        {
            AddOrRemoveEdge(node1.MirrorRef, node2.MirrorRef, true);
        }

        var edge1 = node1.EdgeTo(node2);
        var edge2 = node2.EdgeTo(node1);

        var anyRemoved = false;
        
        if (edge1 != null && edge2 != null)
        {
            DeleteEdge(edge1);

            if (edge1 != edge2) 
                DeleteEdge(edge2);

            anyRemoved = true;
        } 
        else if (edge1 != null)
        {
            DeleteEdge(edge1);
            anyRemoved = true;
        } 
        else if (edge2 != null)
        {
            DeleteEdge(edge2);
            anyRemoved = true;
        }

        if (anyRemoved)
            return null;

        var edge = new Edge(node1, node2);
        node1.Edges.Add(edge);
        node2.Edges.Add(edge);
        _revision++;

        return edge;
    }
}
