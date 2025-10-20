using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Logic.History;

public class RemoveNodeAction : IHistoryAction
{
    private readonly Graph _graph;
    private readonly Node _nodeToRemove;

    public RemoveNodeAction(Graph graph, Node nodeToRemove)
    {
        _graph = graph;
        _nodeToRemove = nodeToRemove;
    }
    
    public void Execute()
    {
        _graph.DeleteNode(_nodeToRemove);
    }

    public void Undo()
    {
        _graph.AddNode(_nodeToRemove);
        var mirrorRef = _nodeToRemove.MirrorRef;

        if (mirrorRef != null)
        {
            _graph.AddNode(mirrorRef);
            FixEdges(mirrorRef);
        }
    }

    private void FixEdges(Node node)
    {
        foreach (var edge in node.Edges)
        {
            if (edge.Node1 != node && !_graph.Nodes.Contains(edge.Node1))
                edge.Node1.Edges.Add(edge);
            else if (edge.Node2 != node && !_graph.Nodes.Contains(edge.Node2))
                edge.Node2.Edges.Add(edge);
        }
    }
}
