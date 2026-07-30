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

        /// <summary>
        /// Replaces the graph of <paramref name="map"/> with a fully connected grid of
        /// nodes, and resizes the map to hold it. Hand-drawing a layout large enough to
        /// see the cost of the node and edge renderers takes hundreds of clicks, so this
        /// produces one directly.
        /// </summary>
        public static void FillWithGrid(Map map, int columns, int rows, int spacing = 12)
        {
            map.Graph.Clear();
            map.Width = (columns + 1) * spacing;
            map.Height = (rows + 1) * spacing;

            var grid = new Node[columns, rows];

            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var position = new Vector2(
                        (column - (columns - 1) / 2f) * spacing + 0.5f,
                        (row - (rows - 1) / 2f) * spacing + 0.5f);

                    // Spread across the node types that have a style, so every shape the
                    // node renderer can draw is represented.
                    var nodeType = ((column + row) % 3) switch
                    {
                        0 => Node.NodeType.Undefined,
                        1 => Node.NodeType.Wool,
                        _ => Node.NodeType.Spawn
                    };

                    var node = new Node(position) { Type = nodeType };
                    grid[column, row] = node;
                    map.Graph.AddNode(node);
                }
            }

            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    if (column + 1 < columns)
                        Connect(grid[column, row], grid[column + 1, row], Edge.EdgeType.Walkable);

                    if (row + 1 < rows)
                        Connect(grid[column, row], grid[column, row + 1], Edge.EdgeType.Bridgeable);
                }
            }

            map.Graph.MarkChanged();
            map.CalculateEdgeBlocks();

            static void Connect(Node first, Node second, Edge.EdgeType type)
            {
                var edge = new Edge(first, second) { Type = type };
                first.Edges.Add(edge);
                second.Edges.Add(edge);
            }
        }
    }
}
