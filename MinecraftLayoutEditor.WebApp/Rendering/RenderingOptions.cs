using MinecraftLayoutEditor.Logic;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering
{
    public class RenderingOptions
    {
        public bool ShowBlocksEnabled { get; set; }
        public bool ShowBoundingBoxEnabled { get; set; }

        public float GridBorderLineWidth { get; init; } = 1f;
        public int GridSpacing { get; init; } = 1;

        public SKColor HoveredNodeStroke { get; init; } = SKColors.Purple;
        public SKColor SelectedNodeStroke { get; init; } = SKColors.Cyan;

        public SKColor CellFillStyle { get; init; } = SKColors.Gray;
        public SKColor BoundingBoxLineStroke { get; init; } = SKColors.Purple;

        public SKColor RegionOutlineStroke { get; init; } = SKColors.Purple;
        public SKColor RegionRadialStroke { get; init; } = SKColors.Blue;
        public SKColor RegionMarkerFill { get; init; } = SKColors.Blue;
        public SKColor HoveredRegionStroke { get; init; } = SKColors.Orange;
        public SKColor SelectedRegionStroke { get; init; } = SKColors.Cyan;

        /// <summary>
        /// Styles are keyed on the enum the caller already holds. A lookup by lowercased
        /// type name allocates two strings per node per frame, which the render path
        /// cannot afford once a layout has more than a handful of nodes.
        /// </summary>
        private readonly Dictionary<Node.NodeType, RenderStyle> _nodeStyles;
        private readonly Dictionary<Edge.EdgeType, RenderStyle> _edgeStyles;
        private readonly RenderStyle _fallbackStyle = new();

        public RenderStyle MirrorPointStyle { get; }
        public RenderStyle MirrorLineStyle { get; }
        public RenderStyle GridLineStyle { get; }

        public RenderingOptions()
        {
            // Node styles
            var defaultNodeStyle = new RenderStyle
            {
                FillStyle = SKColors.LightGray,
                StrokeStyle = SKColors.DarkGray,
                Radius = 0.4f,
                Shape = NodeShape.Circle,
                LineWidth = 2
            };

            var woolNodeStyle = new RenderStyle
            {
                FillStyle = SKColors.Green,
                StrokeStyle = SKColors.DarkGray,
                Radius = 0.4f,
                Shape = NodeShape.Square,
                LineWidth = 2f
            };

            var spawnNodeStyle = new RenderStyle
            {
                FillStyle = SKColors.Blue,
                StrokeStyle = SKColors.DarkGray,
                Radius = 0.4f,
                Shape = NodeShape.Diamond,
                LineWidth = 2f
            };

            // Edge styles
            var walkableEdgeStyle = new RenderStyle
            {
                StrokeStyle = SKColors.DarkGray,
                LineDash = [],
                LineWidth = 2f
            };

            var bridgeableEdgeStyle = new RenderStyle
            {
                StrokeStyle = SKColors.DarkGray,
                LineDash = [5],
                LineWidth = 2f
            };

            // Other styles
            MirrorPointStyle = new RenderStyle
            {
                Radius = 0.4f,
                FillStyle = SKColors.Red,
                StrokeStyle = SKColors.Red,
                LineWidth = 2f
            };

            MirrorLineStyle = new RenderStyle
            {
                LineWidth = 2f,
                StrokeStyle = SKColors.Red,
                LineDash = [5]
            };

            GridLineStyle = new RenderStyle
            {
                LineWidth = 1f,
                StrokeStyle = SKColors.Black,
            };

            _nodeStyles = new Dictionary<Node.NodeType, RenderStyle>
            {
                { Node.NodeType.Undefined, defaultNodeStyle },
                { Node.NodeType.Wool, woolNodeStyle },
                { Node.NodeType.Spawn, spawnNodeStyle }
            };

            _edgeStyles = new Dictionary<Edge.EdgeType, RenderStyle>
            {
                { Edge.EdgeType.Walkable, walkableEdgeStyle },
                { Edge.EdgeType.Bridgeable, bridgeableEdgeStyle }
            };
        }

        public RenderStyle GetNodeStyle(Node.NodeType nodeType) =>
            _nodeStyles.TryGetValue(nodeType, out var style) ? style : _fallbackStyle;

        public RenderStyle GetEdgeStyle(Edge.EdgeType edgeType) =>
            _edgeStyles.TryGetValue(edgeType, out var style) ? style : _fallbackStyle;
    }
}
