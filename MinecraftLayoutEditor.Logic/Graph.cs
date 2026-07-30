using System.Numerics;

namespace MinecraftLayoutEditor.Logic;

public class Graph
{
    private readonly List<Node> _nodes = [];
    private readonly HashSet<Edge> _edges = [];
    private bool _edgesStale = true;

    public IReadOnlyList<Node> Nodes => _nodes;

    /// <summary>
    /// The distinct edges of the graph. Rebuilt only after the graph changes: renderers
    /// read this every frame, so walking every node's edge list per read would put an
    /// allocation and a full graph traversal on the render path.
    /// </summary>
    public IReadOnlyCollection<Edge> Edges
    {
        get
        {
            if (_edgesStale)
            {
                RebuildEdges();
                _edgesStale = false;
            }

            return _edges;
        }
    }

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
    /// Marks the cached edge set for rebuild. Call after mutating a node's edge list
    /// directly rather than through this class.
    /// </summary>
    public void InvalidateEdges()
    {
        _edgesStale = true;
    }

    public void Clear()
    {
        _nodes.Clear();
        _edgesStale = true;
    }

    public void AddNode(Node node)
    {
        _nodes.Add(node);
        _edgesStale = true;
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
        _edgesStale = true;
    }

    public void DeleteEdge(Edge edge)
    {
        edge.Node1.Edges.Remove(edge);
        edge.Node2.Edges.Remove(edge);
        _edgesStale = true;
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
        _edgesStale = true;

        return edge;
    }
}
