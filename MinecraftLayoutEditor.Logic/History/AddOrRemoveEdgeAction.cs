using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Logic.History
{
    public class AddOrRemoveEdgeAction : IHistoryAction
    {
        private readonly Graph _graph;
        private readonly Node _node1;
        private readonly Node _node2;

        public AddOrRemoveEdgeAction(Graph graph, Node node1, Node node2)
        {
            _graph = graph;
            _node1 = node1;
            _node2 = node2;
        }

        public void Execute()
        {
            _graph.AddOrRemoveEdge(_node1, _node2);
        }

        public void Undo()
        {
            _graph.AddOrRemoveEdge(_node1, _node2);
        }
    }
}
