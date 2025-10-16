using System.Numerics;

namespace MinecraftLayoutEditor.Logic
{
    public static class LayoutFactory
    {
        public static Layout Empty(int width, int height)
        {
            var layout = new Layout
            {
                Width = width,
                Height = height,
                MirrorEnabled = true,
                Symmetry = new SymmetryAxis()
                {
                    IsHorizontal = true,
                    RotationDeg = 0
                }
            };

            return layout;
        }
        
        public static Layout OneTeamDemo()
        {
            var layout = new Layout
            {
                Name = "One-Team Demo",
                Width = 300,
                Height = 160,
                Symmetry = new SymmetryAxis()
            };

            // --- Team (Blue as example) ---
            var blue = new Team { Color = MinecraftColor.BLUE };
            layout.Teams.Add(blue);

            // --- Nodes ---
            // 1 spawn
            var spawn = new Node(new Vector2(50, 80)) { Type = Node.NodeType.Spawn, Team = blue };

            // 5 undefined nodes (use Corridor as neutral/undefined)
            var nA = new Node(new Vector2(90, 80)) { Type = Node.NodeType.Undefined, Team = blue };
            var nB = new Node(new Vector2(130, 80)) { Type = Node.NodeType.Undefined, Team = blue };
            var nTop = new Node(new Vector2(170, 50)) { Type = Node.NodeType.Undefined };
            var nMid = new Node(new Vector2(170, 80)) { Type = Node.NodeType.Undefined };
            var nBot = new Node(new Vector2(170, 110)) { Type = Node.NodeType.Undefined };

            // 2 wools
            var wool1 = new Node(new Vector2(240, 40)) { Type = Node.NodeType.Wool, Team = blue };
            var wool2 = new Node(new Vector2(240, 120)) { Type = Node.NodeType.Wool, Team = blue };

            // Add to graph
            layout.Graph.AddNode(spawn);
            layout.Graph.AddNode(nA);
            layout.Graph.AddNode(nB);
            layout.Graph.AddNode(nTop);
            layout.Graph.AddNode(nMid);
            layout.Graph.AddNode(nBot);
            layout.Graph.AddNode(wool1);
            layout.Graph.AddNode(wool2);

            // --- Goals (for the team) ---
            blue.Goals.Add(new Goal(wool1)
            {
                Owner = blue,
                Color = MinecraftColor.BLUE,
                Type = Goal.GoalType.Wool
            });
            blue.Goals.Add(new Goal(wool2)
            {
                Owner = blue,
                Color = MinecraftColor.BLUE,
                Type = Goal.GoalType.Wool
            });

            // --- Edges (bidirectional via helper) ---
            Connect(spawn, nA, Edge.EdgeType.Walkable);
            Connect(nA, nB, Edge.EdgeType.Walkable);

            // hub splits into three corridors
            Connect(nB, nTop, Edge.EdgeType.Walkable);
            Connect(nB, nMid, Edge.EdgeType.Walkable);
            Connect(nB, nBot, Edge.EdgeType.Walkable);

            // corridors to wools (make these bridgeable to vary gameplay)
            Connect(nTop, wool1, Edge.EdgeType.Bridgeable);
            Connect(nBot, wool2, Edge.EdgeType.Bridgeable);

            // small cross-links to avoid chokepoints
            Connect(nTop, nMid, Edge.EdgeType.Walkable);
            Connect(nMid, nBot, Edge.EdgeType.Walkable);

            return layout;

            // --- local helpers ---
            static void Connect(Node a, Node b, Edge.EdgeType type)
            {
                var e = new Edge(a, b) { Type = type };
                a.Edges.Add(e);
                b.Edges.Add(e);
            }
        }
    }
}
