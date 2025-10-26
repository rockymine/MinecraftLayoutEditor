namespace MinecraftLayoutEditor.WebApp.Rendering;

public record RenderStyle
{
    public string FillStyle { get; init; } = "black";
    public string StrokeStyle { get; init; } = "black";
    public float Radius { get; init; } = 6f;
    public string Shape { get; init; } = "circle";
    public float LineWidth { get; init; } = 2f;
    public double[] LineDash { get; init; } = [];
}
