using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Logic.History;

public class RemoveNodeAction : HistoryAction
{
    public Node Node { get; set; }

    public RemoveNodeAction(Node node)
    {
        Node = node;
    }
}
