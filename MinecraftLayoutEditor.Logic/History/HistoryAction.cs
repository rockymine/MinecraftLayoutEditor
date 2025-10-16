namespace MinecraftLayoutEditor.Logic.History;

public class HistoryAction
{
    public static AddNodeAction AddNode(Node node) => new(node);
    public static AddEdgeAction AddEdge(Node node1, Node node2) => new(node1, node2);
    public static RemoveNodeAction RemoveNode(Node node) => new(node);
    public static AddOrRemoveEdgeAction AddOrRemoveEdge(Node node1, Node node2) => new(node1, node2);
}
