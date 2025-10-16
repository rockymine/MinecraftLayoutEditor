namespace MinecraftLayoutEditor.Logic;

public class Goal
{
    public Team? Owner { get; set; }
    public MinecraftColor? Color { get; set; }
    public GoalType Type { get; set; }
    public Node Node { get; set; }

    public Goal(Node node)
    {
        Node = node;
    }

    public enum GoalType
    {
        Unspecified,
        Wool,
        Flag,
        Monument,
        Core,
        CapturePoint
    }
}
