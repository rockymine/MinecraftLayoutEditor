using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public enum NodeShape
{
    Circle,
    Square,
    Diamond
}

public record RenderStyle
{
    public SKColor FillStyle { get; init; } = SKColors.Black;
    public SKColor StrokeStyle { get; init; } = SKColors.Black;
    public float Radius { get; init; } = 6f;
    public NodeShape Shape { get; init; } = NodeShape.Circle;
    public float LineWidth { get; init; } = 2f;
    public double[] LineDash { get; init; } = [];
}
