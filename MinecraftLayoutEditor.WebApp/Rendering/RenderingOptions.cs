using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Rendering
{
    public class RenderingOptions
    {
        public bool ShowBlocksEnabled { get; set; }

        public float GridLineWidth { get; init; } = 0.5f;
        public float GridBorderLineWidth { get; init; } = 1f;
        public string GridLineStroke { get; init; } = "black";
        public int GridSpacing { get; init; } = 1;

        public float MirrorLineWidth { get; init; } = 2f;
        public string MirrorLineStroke { get; init; } = "red";
        public double[] MirrorLineDash { get; init; } = [5];

        public float MirrorPointRadius { get; init; } = 6f;
        public string MirrorPointFill { get; init; } = "red";
        public string MirrorPointLineStroke { get; init; } = "red";
        public float MirrorPointLineWidth { get; init; } = 2f;

        public string HoveredNodeStroke { get; init; } = "purple";
        public string SelectedNodeStroke { get; init; } = "cyan";

        public string CellFillStyle { get; init; } = "gray";
        public string BoundingBoxLineStroke { get; init; } = "purple";

        public Dictionary<Node.NodeType, NodeRenderStyle> NodeStyles { get; set; }
        public Dictionary<Edge.EdgeType, EdgeRenderStyle> EdgeStyles { get; set; }

        public RenderingOptions() 
        {
            var defaultNodeStyle = new NodeRenderStyle
            {
                FillStyle = "lightgray",
                StrokeStyle = "#666",
                Radius = 6f,
                Shape = "circle",
                LineWidth = 2
            };

            var woolNodeStyle = new NodeRenderStyle
            {
                FillStyle = "green",
                StrokeStyle = "#666",
                Radius = 6f,
                Shape = "square",
                LineWidth = 2f
            };

            var spawnNodeStyle = new NodeRenderStyle
            {
                FillStyle = "blue",
                StrokeStyle = "#666",
                Radius = 6f,
                Shape = "diamond",
                LineWidth = 2f
            };

            NodeStyles = new Dictionary<Node.NodeType, NodeRenderStyle>
            {
                { Node.NodeType.Undefined, defaultNodeStyle },
                { Node.NodeType.Wool, woolNodeStyle },
                { Node.NodeType.Spawn, spawnNodeStyle },
            };

            var walkableEdgeStyle = new EdgeRenderStyle
            {
                StrokeStyle = "#666",
                LineDash = [],
                LineWidth = 2f
            };

            var bridgeableEdgeStyle = new EdgeRenderStyle
            {
                StrokeStyle = "#666",
                LineDash = [5],
                LineWidth = 2f
            };

            EdgeStyles = new Dictionary<Edge.EdgeType, EdgeRenderStyle>
            {
                { Edge.EdgeType.Walkable, walkableEdgeStyle },
                { Edge.EdgeType.Bridgeable, bridgeableEdgeStyle }
            };
        }

        public NodeRenderStyle GetStyle(Node.NodeType type) =>
            NodeStyles.TryGetValue(type, out var style) ? style : new NodeRenderStyle();

        public EdgeRenderStyle GetStyle(Edge.EdgeType type) =>
            EdgeStyles.TryGetValue(type, out var style) ? style : new EdgeRenderStyle();
    }
}
