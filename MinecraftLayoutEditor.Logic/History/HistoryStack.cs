using System.Xml.Linq;

namespace MinecraftLayoutEditor.Logic.History;

public class HistoryStack
{
    private Stack<HistoryAction> Actions = [];
    private Graph Graph;

    public HistoryStack(Graph graph)
    {
        Graph = graph;
    }

    public void Add(HistoryAction action)
    {
        Actions.Push(action);
    }

    public void Undo()
    {
        if (Actions.Count == 0)
            return;

        var action = Actions.Pop();

        if (action is AddNodeAction addNodeAction)
        {
             Graph.DeleteNode(addNodeAction.Node);
        }
        else if (action is AddEdgeAction addEdgeAction)
        {
            Graph.AddOrRemoveEdge(addEdgeAction.Node1, addEdgeAction.Node2);
        }
        else if (action is AddOrRemoveEdgeAction removeEdgeAction)
        {
            Graph.AddOrRemoveEdge(removeEdgeAction.Node1, removeEdgeAction.Node2);
        }
        else if (action is RemoveNodeAction removeNodeAction)
        {
            var node = removeNodeAction.Node;
            var edges = node.Edges;

            Graph.AddNode(removeNodeAction.Node);

            // Fix node edges. In node.edges.othernode add edges. (inverted deletenode)
            foreach (var edge in edges)
            {
                if (edge.Node1 != node)
                {
                    edge.Node1.Edges.Add(edge);
                }
                else if (edge.Node2 != node)
                {
                    edge.Node2.Edges.Add(edge);
                }
            }
        }
    }

    //TODO: redo
}
