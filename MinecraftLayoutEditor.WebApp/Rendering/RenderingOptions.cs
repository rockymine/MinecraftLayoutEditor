using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Rendering
{
    public class RenderingOptions
    {
        public bool ShowBlocksEnabled { get; set; }

        public float GridBorderLineWidth { get; init; } = 1f;
        public int GridSpacing { get; init; } = 1;

        public string HoveredNodeStroke { get; init; } = "purple";
        public string SelectedNodeStroke { get; init; } = "cyan";

        public string CellFillStyle { get; init; } = "gray";
        public string BoundingBoxLineStroke { get; init; } = "purple";

        public Dictionary<string, RenderStyle> RenderStyles { get; set; }

        public RenderingOptions() 
        {
            // Node styles
            var defaultNodeStyle = new RenderStyle
            {
                FillStyle = "lightgray",
                StrokeStyle = "#666",
                Radius = 0.4f,
                Shape = "circle",
                LineWidth = 2
            };

            var woolNodeStyle = new RenderStyle
            {
                FillStyle = "green",
                StrokeStyle = "#666",
                Radius = 6f,
                Shape = "square",
                LineWidth = 2f
            };

            var spawnNodeStyle = new RenderStyle
            {
                FillStyle = "blue",
                StrokeStyle = "#666",
                Radius = 6f,
                Shape = "diamond",
                LineWidth = 2f
            };


            // Edge styles
            var walkableEdgeStyle = new RenderStyle
            {
                StrokeStyle = "#666",
                LineDash = [],
                LineWidth = 2f
            };

            var bridgeableEdgeStyle = new RenderStyle
            {
                StrokeStyle = "#666",
                LineDash = [5],
                LineWidth = 2f
            };

            // Other styles
            var mirrorPointStyle = new RenderStyle
            {
                Radius = 6f,
                FillStyle = "red",
                StrokeStyle = "red",
                LineWidth = 2f
            };

            var mirrorLineStyle = new RenderStyle
            {
                LineWidth = 2f,
                StrokeStyle = "red",
                LineDash = [5]
            };

            var gridLineStyle = new RenderStyle
            {
                LineWidth = 0.5f,
                StrokeStyle = "black",
            };

            RenderStyles = new Dictionary<string, RenderStyle>
            {
                { "undefined", defaultNodeStyle },
                { "wool", woolNodeStyle },
                { "spawn", spawnNodeStyle },
                { "walkable", walkableEdgeStyle },
                { "bridgeable", bridgeableEdgeStyle },
                { "mirrorPointStyle", mirrorPointStyle },
                { "mirrorLineStyle", mirrorLineStyle },
                { "gridLineStyle", gridLineStyle }
            };
        }

        public RenderStyle GetStyle(string type) =>
            RenderStyles.TryGetValue(type, out var style) ? style : new RenderStyle();
    }
}
