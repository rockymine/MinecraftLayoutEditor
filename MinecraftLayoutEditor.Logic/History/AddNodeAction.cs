namespace MinecraftLayoutEditor.Logic.History;

public class AddNodeAction : HistoryAction
{
    public Node Node { get; set; }

    public AddNodeAction(Node node)
    {
        Node = node;
    }
}
