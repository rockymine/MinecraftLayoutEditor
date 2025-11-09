using System.Numerics;

namespace MinecraftLayoutEditor.Logic
{
    public static class MapFactory
    {
        public static Map Empty(int width, int height, int laneWidth, int thickness)
        {
            var map = new Map
            {
                Width = width,
                Height = height,
                MirrorEnabled = true,
                LaneWidth = laneWidth,
                Thickness = thickness,
                Symmetry = new SymmetryAxis()
                {
                    IsHorizontal = false,
                    RotationDeg = 0
                }
            };

            return map;
        }

        public static Map Performance(int numEdges = 20, int edgeLength = 200, int laneWidth = 10)
        {
            var map = Empty(edgeLength + 10, numEdges * 20 + 10, laneWidth, 10);  // Adjust dimensions to fit

            for (int i = 0; i < numEdges; i++)
            {
                // Horizontal long edges, spaced vertically
                var y = (i - numEdges / 2f) * 20 + 0.5f;  // Centered, spaced by 20 units
                var n1 = new Node(new Vector2(-edgeLength / 2f + 0.5f, y)) { Type = Node.NodeType.Undefined };
                var n2 = new Node(new Vector2(edgeLength / 2f - 0.5f, y)) { Type = Node.NodeType.Undefined };

                map.Graph.AddNode(n1);
                map.Graph.AddNode(n2);

                var e = new Edge(n1, n2) { Type = Edge.EdgeType.Walkable };
                n1.Edges.Add(e);
                n2.Edges.Add(e);
            }

            // Add some cross-connections for complexity (vertical short edges)
            var nodes = map.Graph.Nodes.ToList();  // Assuming even numEdges for pairing
            for (int i = 0; i < numEdges - 1; i += 2)
            {
                // Connect left nodes vertically
                Connect(nodes[i], nodes[i + 1], Edge.EdgeType.Bridgeable);
                // Connect right nodes vertically
                Connect(nodes[i + numEdges], nodes[i + numEdges + 1], Edge.EdgeType.Bridgeable);
            }

            map.CalculateEdgeBlocks();  // Precompute for perf testing

            return map;

            static void Connect(Node a, Node b, Edge.EdgeType type)
            {
                var e = new Edge(a, b) { Type = type };
                a.Edges.Add(e);
                b.Edges.Add(e);
            }
        }
    }
}
