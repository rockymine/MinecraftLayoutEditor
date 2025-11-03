using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public record RenderStyle
{
    public SKColor FillStyle { get; init; } = SKColors.Black;
    public SKColor StrokeStyle { get; init; } = SKColors.Black;
    public float Radius { get; init; } = 6f;
    public string Shape { get; init; } = "circle";
    public float LineWidth { get; init; } = 2f;
    public double[] LineDash { get; init; } = [];
}
