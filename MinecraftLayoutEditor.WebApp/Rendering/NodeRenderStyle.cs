namespace MinecraftLayoutEditor.WebApp.Rendering;

public class NodeRenderStyle
{
    public string FillStyle { get; init; } = "lightgray";
    public string StrokeStyle { get; init; } = "#666";
    public float Radius { get; init; } = 6f;
    public string Shape { get; init; } = "circle";
    public float LineWidth { get; set; } = 2f;
}
