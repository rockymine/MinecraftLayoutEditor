using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Logic.History
{
    public class AddOrRemoveEdgeAction : HistoryAction
    {
        public Node Node1 { get; set; }
        public Node Node2 { get; set; }

        public AddOrRemoveEdgeAction(Node node1, Node node2)
        {
            Node1 = node1;
            Node2 = node2;
        }
    }
}
