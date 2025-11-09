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

        public Dictionary<string, RenderStyle> RenderStyles { get; set; }

        public RenderingOptions() 
        {
            // Node styles
            var defaultNodeStyle = new RenderStyle
            {
                FillStyle = SKColors.LightGray,
                StrokeStyle = SKColors.DarkGray,
                Radius = 0.4f,
                Shape = "circle",
                LineWidth = 2
            };

            var woolNodeStyle = new RenderStyle
            {
                FillStyle = SKColors.Green,
                StrokeStyle = SKColors.DarkGray,
                Radius = 0.4f,
                Shape = "square",
                LineWidth = 2f
            };

            var spawnNodeStyle = new RenderStyle
            {
                FillStyle = SKColors.Blue,
                StrokeStyle = SKColors.DarkGray,
                Radius = 0.4f,
                Shape = "diamond",
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
            var mirrorPointStyle = new RenderStyle
            {
                Radius = 0.4f,
                FillStyle = SKColors.Red,
                StrokeStyle = SKColors.Red,
                LineWidth = 2f
            };

            var mirrorLineStyle = new RenderStyle
            {
                LineWidth = 2f,
                StrokeStyle = SKColors.Red,
                LineDash = [5]
            };

            var gridLineStyle = new RenderStyle
            {
                LineWidth = 1f,
                StrokeStyle = SKColors.Black,
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

    public enum RenderTrigger
    {
        Initial,
        MouseMove,
        MouseClick,
        Zoom,
        Pan,
        KeyboardMove,
        SettingsChanged,
        NodeAdded,
        NodeRemoved,
        NodeDeselected,
        NodeMoved,
        NodeHover,
        EdgeAdded,
        EdgeRemoved,
        Undo,
        Redo,
        MapCleared,
        ViewReset,
        ViewFit,
        WorldImport
    }
}
